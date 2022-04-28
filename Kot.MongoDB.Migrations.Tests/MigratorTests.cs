using FluentAssertions;
using Kot.MongoDB.Migrations.Tests.Extensions;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class MigratorTests
    {
        private const string DatabaseName = "IntegrationTest";
        private const string MigrationsCollectionName = "MigrationHistory";
        private const string DocCollectionName = "DocCollection";

        private MongoDbRunner _runner;
        private IMongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<MigrationHistory> _histCollection;
        private IMongoCollection<TestDoc> _docCollection;

        [SetUp]
        public void Setup()
        {
            ILogger logger = LoggerFactory
                .Create(config => config.SetMinimumLevel(LogLevel.Error).AddConsole())
                .CreateLogger("Mongo2Go");

            _runner = MongoDbRunner.Start(singleNodeReplSet: true, logger: logger);
            _client = new MongoClient(_runner.ConnectionString);
            _db = _client.GetDatabase(DatabaseName);
            _histCollection = _db.GetCollection<MigrationHistory>(MigrationsCollectionName);
            _docCollection = _db.GetCollection<TestDoc>(DocCollectionName);
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Dispose();
        }

        [Test]
        public async Task NoMigrations()
        {
            // Arrange
            var migrator = SetupMigrator(Enumerable.Empty<IMongoMigration>(), TransactionScope.None);

            // Act
            await migrator.MigrateAsync();

            // Assert
            var historyRecordsCount = await _histCollection.EstimatedDocumentCountAsync();
            var hasCollections = await _db.ListCollections().AnyAsync();

            historyRecordsCount.Should().Be(0);
            hasCollections.Should().Be(false);
        }

        [TestCase(TransactionScope.None, TestName = "ApplyUp_TransactionScopeNone")]
        [TestCase(TransactionScope.SingleMigration, TestName = "ApplyUp_TransactionScopeSingleMigration")]
        [TestCase(TransactionScope.AllMigrations, TestName = "ApplyUp_TransactionScopeAllMigrations")]
        public async Task ApplyUp(TransactionScope transactionScope)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
                new MigratorTest_Migration("0.0.3"),
            };
            var migrator = SetupMigrator(migrations, transactionScope);

            var expectedHistoryDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            var expectedTestDocs = migrations
                .Select(x => new TestDoc { Version = x.Version.ToString() })
                .ToList();

            // Act
            await migrator.MigrateAsync(migrations[2].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedHistoryDocs,
                    opt => opt.Excluding(x => x.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should()
                .HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedTestDocs);
        }

        [TestCase(TransactionScope.None, TestName = "ApplyDown_TransactionScopeNone")]
        [TestCase(TransactionScope.SingleMigration, TestName = "ApplyDown_TransactionScopeSingleMigration")]
        [TestCase(TransactionScope.AllMigrations, TestName = "ApplyDown_TransactionScopeAllMigrations")]
        public async Task ApplyDown(TransactionScope transactionScope)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
                new MigratorTest_Migration("0.0.3"),
            };
            var migrator = SetupMigrator(migrations, transactionScope);

            var testDocs = migrations
                .Select(x => new TestDoc { Version = x.Version.ToString() })
                .ToList();

            await _docCollection.InsertManyAsync(testDocs);

            var historyDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            await _histCollection.InsertManyAsync(historyDocs);

            // Act
            await migrator.MigrateAsync(migrations[0].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(historyDocs[0],
                    opt => opt.Excluding(h => h.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(testDocs[0]);
        }

        [Test]
        public async Task TargetVersionEqualsCurrent()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.None);

            var historyDoc = new MigrationHistory
            {
                Name = migrations[0].Name,
                Version = migrations[0].Version,
                AppliedAt = DateTime.UtcNow,
            };

            await _histCollection.InsertOneAsync(historyDoc);

            // Act
            await migrator.MigrateAsync(migrations[0].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(historyDoc, opt => opt.Excluding(h => h.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should().BeEmpty();
        }

        [Test]
        public async Task MigrationException_NoTransaction()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_MigrationExc("0.0.1")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.None);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<Exception>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().BeEmpty();
            actualTestDocs.Should().BeEmpty();
        }

        [Test]
        public async Task MigrationException_SingleMigrationTransaction()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_MigrationExc("0.0.2")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.SingleMigration);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<Exception>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.Contain(x => x.Name == migrations[0].Name && x.Version == migrations[0].Version);

            actualTestDocs.Should().HaveCount(1)
                .And.Contain(x => x.Version == migrations[0].Version);
        }

        [Test]
        public async Task MigrationException_AllMigrationsTransaction_Up()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_MigrationExc("0.0.2")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.AllMigrations);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<Exception>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().BeEmpty();
            actualTestDocs.Should().BeEmpty();
        }

        [Test]
        public async Task MigrationException_AllMigrationsTransaction_Down()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_MigrationExc("0.0.1"),
                new MigratorTest_Migration("0.0.2")
            };

            var testDocs = migrations
                .Select(x => new TestDoc { Version = x.Version.ToString() })
                .ToList();

            await _docCollection.InsertManyAsync(testDocs);

            var historyDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            await _histCollection.InsertManyAsync(historyDocs);

            var migrator = SetupMigrator(migrations, TransactionScope.AllMigrations);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<Exception>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(migrations.Length)
                .And.BeEquivalentTo(historyDocs,
                    opt => opt.Excluding(x => x.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should()
                .HaveCount(testDocs.Count)
                .And.BeEquivalentTo(testDocs);
        }

        private Migrator SetupMigrator(IEnumerable<IMongoMigration> migrations, TransactionScope transactionScope)
        {
            var locatorMock = new Mock<IMigrationsLocator>();
            locatorMock.Setup(x => x.Locate()).Returns(migrations);

            var options = new MigrationOptions(DatabaseName)
            {
                MigrationsCollectionName = MigrationsCollectionName,
                TransactionScope = transactionScope
            };
            var migrator = new Migrator(locatorMock.Object, _client, options);

            return migrator;
        }

        class MigratorTest_Migration : MongoMigration
        {
            public MigratorTest_Migration() : this("0.0.0") { }

            public MigratorTest_Migration(string version) : base(version, version)
            {
            }

            public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                IMongoCollection<TestDoc> collection = db.GetCollection<TestDoc>(DocCollectionName);
                var doc = new TestDoc { Version = Version.ToString() };

                if (session == null)
                {
                    await collection.InsertOneAsync(doc, null, cancellationToken);
                }
                else
                {
                    await collection.InsertOneAsync(session, doc, null, cancellationToken);
                }
            }

            public override async Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                IMongoCollection<TestDoc> collection = db.GetCollection<TestDoc>(DocCollectionName);

                if (session == null)
                {
                    var res = await collection.DeleteOneAsync(x => x.Version == Version.ToString(), null, cancellationToken);
                }
                else
                {
                    var res = await collection.DeleteOneAsync(session, x => x.Version == Version.ToString(), null, cancellationToken);
                }
            }
        }

        class MigratorTest_MigrationExc : MongoMigration
        {
            public MigratorTest_MigrationExc() : this("0.0.1") { }

            public MigratorTest_MigrationExc(string version) : base(version, version)
            {
            }

            public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                throw new Exception();
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                throw new Exception();
            }
        }

        [BsonIgnoreExtraElements]
        class TestDoc
        {
            public string Version { get; set; }
        }
    }
}