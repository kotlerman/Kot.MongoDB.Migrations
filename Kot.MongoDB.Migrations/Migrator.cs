using Kot.MongoDB.Migrations.Locators;
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
        private readonly IMongoDatabase _db;
        private readonly IMongoCollection<MigrationHistory> _historyCollection;

        /// <summary>
        /// Initializes a new instance of <see cref="Migrator"/> that loads migrations using specified <paramref name="locator"/>
        /// and applies them to a database that <paramref name="mongoClient"/> is connected to, according to <paramref name="options"/>.
        /// </summary>
        /// <param name="locator">Migrations locator that loads migrations.</param>
        /// <param name="mongoClient">Mongo client connected to a database that migrations should be applied to.</param>
        /// <param name="options">Migration options that customize how migrations are applied.</param>
        /// <exception cref="ArgumentNullException"><paramref name="locator"/>, <paramref name="mongoClient"/> or
        /// <paramref name="options"/> is <see langword="null"/>.</exception>
        public Migrator(IMigrationsLocator locator, IMongoClient mongoClient, MigrationOptions options)
        {
            _migrationsLocator = locator ?? throw new ArgumentNullException(nameof(locator));
            _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _db = _mongoClient.GetDatabase(_options.DatabaseName);
            _historyCollection = _db.GetCollection<MigrationHistory>(_options.MigrationsCollectionName);
        }

        /// <inheritdoc/>
        public async Task MigrateAsync(DatabaseVersion? targetVersion = null, CancellationToken cancellationToken = default)
        {
            await CreateIndex();

            DatabaseVersion currVersion = await GetCurrentDatabaseVersion(_historyCollection, cancellationToken);
            IEnumerable<IMongoMigration> migrations = _migrationsLocator.Locate();

            if (currVersion == targetVersion)
            {
                return;
            }

            bool isUpgrade = targetVersion > currVersion || targetVersion == null;

            IEnumerable<IMongoMigration> applicableMigrations = isUpgrade
                ? migrations.SkipWhile(x => x.Version <= currVersion).TakeWhile(x => x.Version <= targetVersion || targetVersion == null)
                : migrations.Reverse().TakeWhile(x => x.Version > targetVersion.Value);

            switch (_options.TransactionScope)
            {
                case TransactionScope.AllMigrations:
                    await ApplyAllMigrationsInOneTransaction(applicableMigrations, isUpgrade, cancellationToken);
                    break;
                case TransactionScope.SingleMigration:
                    await ApplyAllMigrationsInSeparateTransactions(applicableMigrations, isUpgrade, cancellationToken);
                    break;
                case TransactionScope.None:
                    await ApplyAllMigrationsWithoutTransaction(applicableMigrations, isUpgrade, cancellationToken);
                    break;
            }
        }

        private async Task CreateIndex()
        {
            IndexKeysDefinition<MigrationHistory> indexDefinition = Builders<MigrationHistory>.IndexKeys
                .Ascending(MajorFieldName)
                .Ascending(MinorFieldName)
                .Ascending(PatchFieldName);

            CreateIndexModel<MigrationHistory> indexModel = new CreateIndexModel<MigrationHistory>(
                indexDefinition, new CreateIndexOptions { Unique = true });

            await _historyCollection.Indexes.CreateOneAsync(indexModel);
        }

        private async Task ApplyAllMigrationsInOneTransaction(IEnumerable<IMongoMigration> migrations, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            using (IClientSessionHandle session = await _mongoClient.StartSessionAsync(_options.ClientSessionOptions, cancellationToken))
            {
                session.StartTransaction();

                try
                {
                    foreach (IMongoMigration migration in migrations)
                    {
                        await ApplyMigration(session, migration, isUpgrade, cancellationToken);
                    }

                    await session.CommitTransactionAsync(cancellationToken);
                }
                catch
                {
                    await session.AbortTransactionAsync(cancellationToken);
                    throw;
                }
            }
        }

        private async Task ApplyAllMigrationsInSeparateTransactions(IEnumerable<IMongoMigration> migrations, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            foreach (IMongoMigration migration in migrations)
            {
                using (IClientSessionHandle session = await _mongoClient.StartSessionAsync(_options.ClientSessionOptions, cancellationToken))
                {
                    session.StartTransaction();

                    try
                    {
                        await ApplyMigration(session, migration, isUpgrade, cancellationToken);
                        await session.CommitTransactionAsync(cancellationToken);
                    }
                    catch
                    {
                        await session.AbortTransactionAsync(cancellationToken);
                        throw;
                    }
                }
            }
        }

        private async Task ApplyAllMigrationsWithoutTransaction(IEnumerable<IMongoMigration> migrations, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            foreach (IMongoMigration migration in migrations)
            {
                await ApplyMigration(null, migration, isUpgrade, cancellationToken);
            }
        }

        private async Task ApplyMigration(IClientSessionHandle session, IMongoMigration migration, bool isUpgrade,
            CancellationToken cancellationToken)
        {
            if (isUpgrade)
            {
                await ApplyMigrationUp(session, migration, cancellationToken);
            }
            else
            {
                await ApplyMigrationDown(session, migration, cancellationToken);
            }
        }

        private async Task ApplyMigrationUp(IClientSessionHandle session, IMongoMigration migration, CancellationToken cancellationToken)
        {
            await migration.UpAsync(_db, session, cancellationToken);

            var historyEntry = new MigrationHistory
            {
                Version = migration.Version,
                Name = migration.Name,
                AppliedAt = DateTime.Now
            };

            if (session == null)
            {
                await _historyCollection.InsertOneAsync(historyEntry, null, cancellationToken);
            }
            else
            {
                await _historyCollection.InsertOneAsync(session, historyEntry, null, cancellationToken);
            }
        }

        private async Task ApplyMigrationDown(IClientSessionHandle session, IMongoMigration migration, CancellationToken cancellationToken)
        {
            await migration.DownAsync(_db, session, cancellationToken);

            if (session == null)
            {
                await _historyCollection.DeleteOneAsync(x => x.Version == migration.Version, cancellationToken);
            }
            else
            {
                await _historyCollection.DeleteOneAsync(session, x => x.Version == migration.Version, null, cancellationToken);
            }
        }

        private static async Task<DatabaseVersion> GetCurrentDatabaseVersion(IMongoCollection<MigrationHistory> historyCollection,
            CancellationToken cancellationToken)
        {
            var sort = Builders<MigrationHistory>.Sort
                .Descending(MajorFieldName)
                .Descending(MinorFieldName)
                .Descending(PatchFieldName);

            MigrationHistory lastMigration = await historyCollection
                .Find(FilterDefinition<MigrationHistory>.Empty)
                .Sort(sort)
                .FirstOrDefaultAsync(cancellationToken);

            return lastMigration?.Version ?? default;
        }
    }
}
