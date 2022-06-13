using Kot.MongoDB.Migrations.Locators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    public class Migrator : IMigrator
    {
        private readonly IMigrationsLocator _migrationsLocator;
        private readonly IMongoClient _mongoClient;
        private readonly MigrationOptions _options;
        private readonly IMongoDatabase _db;
        private readonly IMongoCollection<MigrationHistory> _historyCollection;

        public Migrator(IMigrationsLocator locator, IMongoClient mongoClient, MigrationOptions options)
        {
            _migrationsLocator = locator;
            _mongoClient = mongoClient;
            _options = options;
            _db = _mongoClient.GetDatabase(_options.DatabaseName);
            _historyCollection = _db.GetCollection<MigrationHistory>(_options.MigrationsCollectionName);
        }

        public async Task MigrateAsync(DatabaseVersion targetVersion = default, CancellationToken cancellationToken = default)
        {
            DatabaseVersion currentVersion = await GetCurrentDatabaseVersion(_historyCollection, cancellationToken);
            IEnumerable<IMongoMigration> migrations = _migrationsLocator.Locate();

            if (targetVersion != default && currentVersion == targetVersion)
            {
                return;
            }

            bool isUpgrade = targetVersion >= currentVersion;

            IEnumerable<IMongoMigration> applicableMigrations = isUpgrade
                ? migrations.TakeWhile(x => x.Version <= targetVersion || targetVersion == default)
                : migrations.Reverse().TakeWhile(x => x.Version > targetVersion);

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
                .Descending($"{nameof(MigrationHistory.Version)}.{nameof(DatabaseVersion.Major)}")
                .Descending($"{nameof(MigrationHistory.Version)}.{nameof(DatabaseVersion.Minor)}")
                .Descending($"{nameof(MigrationHistory.Version)}.{nameof(DatabaseVersion.Patch)}");

            MigrationHistory lastMigration = await historyCollection
                .Find(FilterDefinition<MigrationHistory>.Empty)
                .Sort(sort)
                .FirstOrDefaultAsync(cancellationToken);

            return lastMigration?.Version ?? default;
        }
    }
}
