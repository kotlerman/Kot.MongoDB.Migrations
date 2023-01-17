using Kot.MongoDB.Migrations.Exceptions;
using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Mongo database migrator that loads migrations and applies them to a database.
    /// </summary>
    public class Migrator : IMigrator
    {
        private static readonly string MajorFieldName = $"{nameof(MigrationHistory.Version)}.{nameof(DatabaseVersion.Major)}";
        private static readonly string MinorFieldName = $"{nameof(MigrationHistory.Version)}.{nameof(DatabaseVersion.Minor)}";
        private static readonly string PatchFieldName = $"{nameof(MigrationHistory.Version)}.{nameof(DatabaseVersion.Patch)}";

        private readonly IMigrationsLocator _migrationsLocator;
        private readonly IMongoClient _mongoClient;
        private readonly MigrationOptions _options;
        private readonly ILogger _logger;
        private readonly IMongoDatabase _db;
        private readonly IMongoCollection<MigrationHistory> _historyCollection;
        private readonly IMongoCollection<MigrationLock> _lockCollection;

        /// <summary>
        /// Initializes a new instance of <see cref="Migrator"/> that loads migrations using specified <paramref name="locator"/>
        /// and applies them to a database that <paramref name="mongoClient"/> is connected to, according to <paramref name="options"/>.
        /// </summary>
        /// <param name="locator">Migrations locator that loads migrations.</param>
        /// <param name="mongoClient">Mongo client connected to a database that migrations should be applied to.</param>
        /// <param name="options">Migration options that customize how migrations are applied.</param>
        /// <param name="logger">Logger.</param>
        /// <exception cref="ArgumentNullException"><paramref name="locator"/>, <paramref name="mongoClient"/> or
        /// <paramref name="options"/> is <see langword="null"/>.</exception>
        public Migrator(IMigrationsLocator locator, IMongoClient mongoClient, MigrationOptions options, ILogger<Migrator> logger = null)
        {
            _migrationsLocator = locator ?? throw new ArgumentNullException(nameof(locator));
            _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _db = _mongoClient.GetDatabase(_options.DatabaseName);
            _historyCollection = _db.GetCollection<MigrationHistory>(_options.MigrationsCollectionName);
            _lockCollection = _db.GetCollection<MigrationLock>(_options.MigrationsLockCollectionName);
        }

        /// <inheritdoc/>
        public async Task<MigrationResult> MigrateAsync(DatabaseVersion? targetVersion = null, CancellationToken cancellationToken = default)
        {
            if (targetVersion.HasValue)
            {
                _logger?.LogInformation("Starting migration. Target version is {version}.", targetVersion);
            }
            else
            {
                _logger?.LogInformation("Starting migration.");
            }

            var startTime = DateTime.UtcNow;
            _logger?.LogDebug("Acquiring DB lock.");
            bool lockAcquired = await TryAcquireDbLock(cancellationToken).ConfigureAwait(false);

            if (!lockAcquired)
            {
                _logger?.LogDebug("Failed to acquire DB lock.");

                switch (_options.ParallelRunsBehavior)
                {
                    case ParallelRunsBehavior.Throw:
                        _logger?.LogError("Other migration in progress detected.");
                        throw new MigrationInProgressException();

                    case ParallelRunsBehavior.Cancel:
                    default:
                        _logger?.LogInformation("Other migration in progress detected. Cancelling current run.");
                        return new MigrationResult
                        {
                            Type = MigrationResultType.Cancelled,
                            StartTime = startTime,
                            FinishTime = DateTime.UtcNow
                        };
                }
            }

            try
            {
                return await MigrateInner(startTime, targetVersion, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "There was an error while applying the migrations.");
                throw;
            }
            finally
            {
                _logger?.LogDebug("Releasing DB lock.");
                await ReleaseDbLock(cancellationToken).ConfigureAwait(false);
                _logger?.LogDebug("DB lock released.");
                _logger?.LogInformation("Migration completed.");
            }
        }

        private async Task<MigrationResult> MigrateInner(DateTime startTime, DatabaseVersion? targetVersion, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Creating indexes for migrations history collection.");
            await CreateIndex().ConfigureAwait(false);

            _logger?.LogDebug("Getting current DB version.");
            DatabaseVersion? initialVersion = await GetCurrentDatabaseVersion(_historyCollection, cancellationToken).ConfigureAwait(false);
            DatabaseVersion currVersion = initialVersion ?? default;
            _logger?.LogInformation("Current DB version is {version}.", currVersion);

            _logger?.LogDebug("Locating migrations.");
            List<IMongoMigration> migrations = _migrationsLocator.Locate().ToList();
            _logger?.LogInformation("Found {count} migrations.", migrations.Count);

            if (currVersion == targetVersion)
            {
                _logger?.LogInformation("The DB is up-to-date.");

                return new MigrationResult
                {
                    Type = MigrationResultType.UpToDate,
                    StartTime = startTime,
                    FinishTime = DateTime.UtcNow,
                    InitialVersion = initialVersion,
                    FinalVersion = initialVersion
                };
            }

            bool isUpgrade = targetVersion > currVersion || targetVersion == null;
            List<IMongoMigration> applicableMigrations;

            if (isUpgrade)
            {
                applicableMigrations = migrations
                    .SkipWhile(x => x.Version <= currVersion)
                    .TakeWhile(x => x.Version <= targetVersion || targetVersion == null)
                    .ToList();
            }
            else
            {
                applicableMigrations = migrations
                    .Reverse<IMongoMigration>()
                    .SkipWhile(x => x.Version > currVersion)
                    .TakeWhile(x => x.Version > targetVersion.Value)
                    .ToList();
            }

            if (applicableMigrations.Count == 0)
            {
                _logger?.LogInformation("The DB is up-to-date.");

                return new MigrationResult
                {
                    Type = MigrationResultType.UpToDate,
                    StartTime = startTime,
                    FinishTime = DateTime.UtcNow,
                    InitialVersion = initialVersion,
                    FinalVersion = initialVersion,
                };
            }

            if (isUpgrade)
            {
                _logger?.LogInformation("Upgrading the DB with {count} applicable migrations.", applicableMigrations.Count);
            }
            else
            {
                _logger?.LogInformation("Downgrading the DB with {count} applicable migrations.", applicableMigrations.Count);
            }

            switch (_options.TransactionScope)
            {
                case TransactionScope.AllMigrations:
                    await ApplyAllMigrationsInOneTransaction(applicableMigrations, isUpgrade, cancellationToken).ConfigureAwait(false);
                    break;
                case TransactionScope.SingleMigration:
                    await ApplyAllMigrationsInSeparateTransactions(applicableMigrations, isUpgrade, cancellationToken).ConfigureAwait(false);
                    break;
                case TransactionScope.None:
                    await ApplyAllMigrationsWithoutTransaction(applicableMigrations, isUpgrade, cancellationToken).ConfigureAwait(false);
                    break;
            }

            _logger?.LogDebug("Verifying DB version after migration.");
            DatabaseVersion? finalVersion = await GetCurrentDatabaseVersion(_historyCollection, cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation("The DB was migrated to version {version}.", finalVersion);

            return new MigrationResult
            {
                Type = isUpgrade ? MigrationResultType.Upgraded : MigrationResultType.Downgraded,
                StartTime = startTime,
                FinishTime = DateTime.UtcNow,
                InitialVersion = initialVersion,
                FinalVersion = finalVersion,
                AppliedMigrations = applicableMigrations
            };
        }

        private async Task<bool> TryAcquireDbLock(CancellationToken cancellationToken)
        {
            UpdateResult result = await _lockCollection.UpdateOneAsync(
                Builders<MigrationLock>.Filter.Empty,
                Builders<MigrationLock>.Update.SetOnInsert(x => x.AcquiredAt, DateTime.UtcNow),
                new UpdateOptions { IsUpsert = true },
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return result.MatchedCount == 0;
        }

        private async Task ReleaseDbLock(CancellationToken cancellationToken)
        {
            await _lockCollection.DeleteOneAsync(Builders<MigrationLock>.Filter.Empty, cancellationToken).ConfigureAwait(false);
        }

        private async Task CreateIndex()
        {
            IndexKeysDefinition<MigrationHistory> indexDefinition = Builders<MigrationHistory>.IndexKeys
                .Ascending(MajorFieldName)
                .Ascending(MinorFieldName)
                .Ascending(PatchFieldName);

            CreateIndexModel<MigrationHistory> indexModel = new CreateIndexModel<MigrationHistory>(
                indexDefinition, new CreateIndexOptions { Unique = true });

            await _historyCollection.Indexes.CreateOneAsync(indexModel).ConfigureAwait(false);
        }

        private async Task ApplyAllMigrationsInOneTransaction(IEnumerable<IMongoMigration> migrations, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Applying all migrations in one transaction.");

            using (IClientSessionHandle session = await _mongoClient.StartSessionAsync(_options.ClientSessionOptions, cancellationToken)
                .ConfigureAwait(false))
            {
                _logger?.LogDebug("Starting a transaction.");
                session.StartTransaction();

                try
                {
                    foreach (IMongoMigration migration in migrations)
                    {
                        await ApplyMigration(session, migration, isUpgrade, cancellationToken).ConfigureAwait(false);
                    }

                    _logger?.LogDebug("Commiting the transaction.");
                    await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "There was an error while applying the migrations. Aborting the transaction.");
                    await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }
        }

        private async Task ApplyAllMigrationsInSeparateTransactions(IEnumerable<IMongoMigration> migrations, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Applying migrations in separate transactions.");

            foreach (IMongoMigration migration in migrations)
            {
                using (IClientSessionHandle session = await _mongoClient.StartSessionAsync(_options.ClientSessionOptions, cancellationToken)
                    .ConfigureAwait(false))
                {
                    _logger?.LogDebug("Starting a transaction for migration {name} ({version}).", migration.Name, migration.Version);
                    session.StartTransaction();

                    try
                    {
                        await ApplyMigration(session, migration, isUpgrade, cancellationToken).ConfigureAwait(false);
                        _logger?.LogDebug("Commiting the transaction.");
                        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "There was an error while applying the migration. Aborting the transaction.");
                        await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                        throw;
                    }
                }
            }
        }

        private async Task ApplyAllMigrationsWithoutTransaction(IEnumerable<IMongoMigration> migrations, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Applying all migrations without transactions.");

            foreach (IMongoMigration migration in migrations)
            {
                await ApplyMigration(null, migration, isUpgrade, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ApplyMigration(IClientSessionHandle session, IMongoMigration migration, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Applying migration {name} ({version}).", migration.Name, migration.Version);

            if (isUpgrade)
            {
                await ApplyMigrationUp(session, migration, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ApplyMigrationDown(session, migration, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ApplyMigrationUp(IClientSessionHandle session, IMongoMigration migration, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Applying the migration (UP).");
            await migration.UpAsync(_db, session, cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Migration applied.");

            var historyEntry = new MigrationHistory
            {
                Version = migration.Version,
                Name = migration.Name,
                AppliedAt = DateTime.Now
            };

            _logger?.LogDebug("Writing history entry.");

            if (session == null)
            {
                await _historyCollection.InsertOneAsync(historyEntry, null, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _historyCollection.InsertOneAsync(session, historyEntry, null, cancellationToken).ConfigureAwait(false);
            }

            _logger?.LogDebug("History entry saved.");
        }

        private async Task ApplyMigrationDown(IClientSessionHandle session, IMongoMigration migration, CancellationToken cancellationToken)
        {
            _logger?.LogDebug("Applying the migration (DOWN).");
            await migration.DownAsync(_db, session, cancellationToken);
            _logger?.LogDebug("Migration applied.");
            _logger?.LogDebug("Deleting history entry.");

            if (session == null)
            {
                await _historyCollection.DeleteOneAsync(x => x.Version == migration.Version, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _historyCollection.DeleteOneAsync(session, x => x.Version == migration.Version, null, cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger?.LogDebug("History entry deleted.");
        }

        private static async Task<DatabaseVersion?> GetCurrentDatabaseVersion(IMongoCollection<MigrationHistory> historyCollection,
            CancellationToken cancellationToken)
        {
            var sort = Builders<MigrationHistory>.Sort
                .Descending(MajorFieldName)
                .Descending(MinorFieldName)
                .Descending(PatchFieldName);

            MigrationHistory lastMigration = await historyCollection
                .Find(FilterDefinition<MigrationHistory>.Empty)
                .Sort(sort)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return lastMigration?.Version;
        }
    }
}
