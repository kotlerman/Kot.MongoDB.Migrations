using FluentAssertions;
using Kot.MongoDB.Migrations.Locators;
using Kot.MongoDB.Migrations.Tests.Extensions;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using MongoDB.Bson;
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
            var expectedResult = new MigrationResult
            {
                AppliedMigrations = new List<IMongoMigration>(),
                InitialVersion = null,
                FinalVersion = null,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };

            // Act
            MigrationResult actualResult = await migrator.MigrateAsync();

            // Assert
            var historyRecordsCount = await _histCollection.EstimatedDocumentCountAsync();
            var collectionNames = await _db.ListCollectionNames().ToListAsync();

            historyRecordsCount.Should().Be(0);
            collectionNames.Should().ContainSingle(x => x == MigrationsCollectionName);
            VerifyMigrationResult(actualResult, expectedResult);
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
            var expectedResult = new MigrationResult
            {
                AppliedMigrations = migrations.ToList(),
                InitialVersion = null,
                FinalVersion = migrations[2].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };
            var migrator = SetupMigrator(migrations, transactionScope);

            var expectedHistoryDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            var expectedTestDocs = migrations
                .Select(x => new TestDoc { Version = x.Version.ToString() })
                .ToList();

            // Act
            MigrationResult actualResult = await migrator.MigrateAsync(migrations[2].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedHistoryDocs,
                    opt => opt.Excluding(x => x.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should()
                .HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedTestDocs);

            VerifyMigrationResult(actualResult, expectedResult);
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
            var expectedResult = new MigrationResult
            {
                AppliedMigrations = migrations.Reverse().Take(2).ToList(),
                InitialVersion = migrations[2].Version,
                FinalVersion = migrations[0].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
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
            MigrationResult actualResult = await migrator.MigrateAsync(migrations[0].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(historyDocs[0],
                    opt => opt.Excluding(h => h.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(testDocs[0]);

            VerifyMigrationResult(actualResult, expectedResult);
        }

        [Test]
        public async Task TargetVersionEqualsCurrent()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1")
            };
            var expectedResult = new MigrationResult
            {
                AppliedMigrations = new List<IMongoMigration>(),
                InitialVersion = migrations[0].Version,
                FinalVersion = migrations[0].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
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
            MigrationResult actualResult = await migrator.MigrateAsync(migrations[0].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.ContainEquivalentOf(historyDoc, opt => opt.Excluding(h => h.Id).UsingNonStrictDateTimeComparison());

            actualTestDocs.Should().BeEmpty();

            VerifyMigrationResult(actualResult, expectedResult);
        }

        [Test]
        public async Task FirstMigrationAlreadyApplied()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
            };

            var expectedResult = new MigrationResult
            {
                AppliedMigrations = new List<IMongoMigration>() { migrations[1] },
                InitialVersion = migrations[0].Version,
                FinalVersion = migrations[1].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };

            var historyDoc = new MigrationHistory
            {
                Name = migrations[0].Name,
                Version = migrations[0].Version,
                AppliedAt = DateTime.Now
            };

            await _histCollection.InsertOneAsync(historyDoc);

            var migrator = SetupMigrator(migrations, TransactionScope.None);

            var expectedHistoryDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            // Act
            MigrationResult actualResult = await migrator.MigrateAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(migrations.Length)
                .And.BeEquivalentTo(expectedHistoryDocs,
                    opt => opt.Excluding(x => x.Id).UsingNonStrictDateTimeComparison());

            VerifyMigrationResult(actualResult, expectedResult);
        }

        [Test]
        public async Task RollbackLastMigration()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
            };

            var expectedResult = new MigrationResult
            {
                AppliedMigrations = new List<IMongoMigration>() { migrations[1] },
                InitialVersion = migrations[1].Version,
                FinalVersion = migrations[0].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };

            var historyDocs = migrations
                .Select(x => new MigrationHistory { Name = x.Name, Version = x.Version, AppliedAt = DateTime.Now })
                .ToList();

            await _histCollection.InsertManyAsync(historyDocs);

            var migrator = SetupMigrator(migrations, TransactionScope.None);

            var expectedHistoryDocs = new List<MigrationHistory>
            {
                new MigrationHistory
                {
                    Name = migrations[0].Name,
                    Version = migrations[0].Version,
                    AppliedAt = DateTime.Now
                }
            };

            // Act
            MigrationResult actualResult = await migrator.MigrateAsync(migrations[0].Version);

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(expectedHistoryDocs.Count)
                .And.BeEquivalentTo(expectedHistoryDocs,
                    opt => opt.Excluding(x => x.Id).UsingNonStrictDateTimeComparison());

            VerifyMigrationResult(actualResult, expectedResult);
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
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync("0.0.0");

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

        [Test]
        public async Task IndexExists()
        {
            // Arrange
            var migrator = SetupMigrator(Enumerable.Empty<IMongoMigration>(), TransactionScope.None);

            // Act
            await migrator.MigrateAsync();

            // Assert
            List<BsonDocument> indexes = await _histCollection.Indexes.List().ToListAsync();
            BsonDocument versionIndexKey = indexes.Single(x => x["name"] != "_id_")["key"].AsBsonDocument;

            const string majorKey = $"{nameof(MigrationHistory.Version)}.{nameof(MigrationHistory.Version.Major)}";
            const string minorKey = $"{nameof(MigrationHistory.Version)}.{nameof(MigrationHistory.Version.Minor)}";
            const string patchKey = $"{nameof(MigrationHistory.Version)}.{nameof(MigrationHistory.Version.Patch)}";

            versionIndexKey[majorKey].Should().Be(1);
            versionIndexKey[minorKey].Should().Be(1);
            versionIndexKey[patchKey].Should().Be(1);
        }

        [Test]
        public void NullLocator_ThrowsException()
        {
            // Act && Assert
            Assert.Throws<ArgumentNullException>(() => new Migrator(null, _client, new MigrationOptions(DatabaseName)));
        }

        [Test]
        public void NullClient_ThrowsException()
        {
            // Act && Assert
            Assert.Throws<ArgumentNullException>(
                () => new Migrator(new Mock<IMigrationsLocator>().Object, null, new MigrationOptions(DatabaseName)));
        }

        [Test]
        public void NullOptions_ThrowsException()
        {
            // Act && Assert
            Assert.Throws<ArgumentNullException>(() => new Migrator(new Mock<IMigrationsLocator>().Object, _client, null));
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

        private static void VerifyMigrationResult(MigrationResult actual, MigrationResult expected)
        {
            actual.Should().BeEquivalentTo(expected, x => x.UsingNonStrictDateTimeComparison());
            actual.FinishTime.Should().BeOnOrAfter(actual.StartTime);
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