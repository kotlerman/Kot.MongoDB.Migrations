using FluentAssertions;
using FluentAssertions.Extensions;
using Mongo2Go;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [SetUp]
        public void Setup()
        {
            _runner = MongoDbRunner.Start(singleNodeReplSet: true);
            _client = new MongoClient(_runner.ConnectionString);
            _db = _client.GetDatabase(DatabaseName);
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
            var locatorMock = new Mock<IMigrationsLocator>();
            locatorMock.Setup(x => x.Locate()).Returns(Array.Empty<IMongoMigration>());

            var options = new MigrationOptions
            {
                DatabaseName = DatabaseName,
                MigrationsCollectionName = MigrationsCollectionName,
                TransactionScope = TransactionScope.None
            };
            var migrator = new Migrator(locatorMock.Object, _client, options);

            // Act
            await migrator.MigrateAsync();

            // Assert
            var historyRecordsCount = await _db.GetCollection<MigrationHistory>(MigrationsCollectionName).EstimatedDocumentCountAsync();
            var hasCollections = await _db.ListCollections().AnyAsync();

            Assert.AreEqual(0, historyRecordsCount);
            Assert.IsFalse(hasCollections);
        }

        [TestCase(TransactionScope.None, TestName = "ApplyUp_TransactionScopeNone")]
        [TestCase(TransactionScope.SingleMigration, TestName = "ApplyUp_TransactionScopeSingleMigration")]
        [TestCase(TransactionScope.AllMigrations, TestName = "ApplyUp_TransactionScopeAllMigrations")]
        public async Task ApplyUp_NoTransaction(TransactionScope transactionScope)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
                new MigratorTest_Migration("0.0.3"),
            };

            var locatorMock = new Mock<IMigrationsLocator>();
            locatorMock.Setup(x => x.Locate()).Returns(migrations);

            var options = new MigrationOptions
            {
                DatabaseName = DatabaseName,
                MigrationsCollectionName = MigrationsCollectionName,
                TransactionScope = transactionScope
            };
            var migrator = new Migrator(locatorMock.Object, _client, options);

            var expectedHistoryDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            var expectedTestDocs = migrations
                .Select(x => new TestDoc { Version = x.Version.ToString() })
                .ToList();

            // Act
            await migrator.MigrateAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _db.GetCollection<MigrationHistory>(MigrationsCollectionName)
                .Find(FilterDefinition<MigrationHistory>.Empty)
                .ToListAsync();

            List<TestDoc> actualTestDocs = await _db.GetCollection<TestDoc>(DocCollectionName)
                .Find(FilterDefinition<TestDoc>.Empty)
                .ToListAsync();

            actualHistoryDocs.Should().HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedHistoryDocs,
                    opt => opt.Excluding(x => x.Id)
                        .Using<DateTime>(x => x.Subject.Should().BeCloseTo(DateTime.UtcNow, 5.Minutes()))
                        .When(x => x.Type == typeof(DateTime)));

            actualTestDocs.Should()
                .HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedTestDocs);
        }

        [TestCase(TransactionScope.None, TestName = "ApplyDown_TransactionScopeNone")]
        [TestCase(TransactionScope.SingleMigration, TestName = "ApplyDown_TransactionScopeSingleMigration")]
        [TestCase(TransactionScope.AllMigrations, TestName = "ApplyDown_TransactionScopeAllMigrations")]
        public async Task ApplyDown_NoTransaction(TransactionScope transactionScope)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
                new MigratorTest_Migration("0.0.3"),
            };

            var locatorMock = new Mock<IMigrationsLocator>();
            locatorMock.Setup(x => x.Locate()).Returns(migrations);

            var options = new MigrationOptions
            {
                DatabaseName = DatabaseName,
                MigrationsCollectionName = MigrationsCollectionName,
                TransactionScope = transactionScope
            };
            var migrator = new Migrator(locatorMock.Object, _client, options);

            var testDocs = migrations
                .Select(x => new TestDoc { Version = x.Version.ToString() })
                .ToList();

            IMongoCollection<TestDoc> docCollection = _db.GetCollection<TestDoc>(DocCollectionName);
            await docCollection.InsertManyAsync(testDocs);

            var historyDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            IMongoCollection<MigrationHistory> migrationsCollection = _db.GetCollection<MigrationHistory>(MigrationsCollectionName);
            await migrationsCollection.InsertManyAsync(historyDocs);

            // Act
            await migrator.MigrateAsync(migrations[0].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await migrationsCollection
                .Find(FilterDefinition<MigrationHistory>.Empty)
                .ToListAsync();

            List<TestDoc> actualTestDocs = await docCollection
                .Find(FilterDefinition<TestDoc>.Empty)
                .ToListAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(historyDocs[0],
                    opt => opt.Excluding(h => h.Id)
                        .Using<DateTime>(x => x.Subject.Should().BeCloseTo(DateTime.UtcNow, 1.Seconds()))
                        .When(x => x.Type == typeof(DateTime)));

            actualTestDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(testDocs[0]);
        }

        class MigratorTest_Migration : MongoMigration
        {

            public MigratorTest_Migration() : this("0.0.0") { }

            public MigratorTest_Migration(string version) : base(version, version)
            {
            }

            public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session)
            {
                IMongoCollection<TestDoc> collection = db.GetCollection<TestDoc>(DocCollectionName);
                var doc = new TestDoc { Version = Version.ToString() };
                await collection.InsertOneAsync(doc);
            }

            public override async Task DownAsync(IMongoDatabase db, IClientSessionHandle session)
            {
                IMongoCollection<TestDoc> collection = db.GetCollection<TestDoc>(DocCollectionName);
                var res = await collection.DeleteOneAsync(x => x.Version == Version.ToString());
            }
        }

        [BsonIgnoreExtraElements]
        class TestDoc
        {
            public string Version { get; set; }
        }
    }
}