using FluentAssertions;
using Kot.MongoDB.Migrations.Exceptions;
using Kot.MongoDB.Migrations.Locators;
using Kot.MongoDB.Migrations.Tests.Extensions;
using Kot.MongoDB.Migrations.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private const string MigrationsLockCollectionName = "MigrationHistory.lock";
        private const string DocCollectionName = "DocCollection";

        private MongoDbRunner _runner;
        private IMongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<MigrationHistory> _histCollection;
        private IMongoCollection<MigrationLock> _lockCollection;
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
            _lockCollection = _db.GetCollection<MigrationLock>(MigrationsLockCollectionName);
            _docCollection = _db.GetCollection<TestDoc>(DocCollectionName);
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Dispose();
        }

        [TestCase(false, TestName = "NoMigrations_WithoutLogger")]
        [TestCase(true, TestName = "NoMigrations_WithLogger")]
        public async Task NoMigrations(bool withLogger)
        {
            // Arrange
            var loggerWrapper = new LoggerWrapper<Migrator>(withLogger);
            var migrator = SetupMigrator(Enumerable.Empty<IMongoMigration>(), TransactionScope.None, withLogger);
            var expectedResult = new MigrationResult
            {
                Type = MigrationResultType.UpToDate,
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

        [TestCase(TransactionScope.None, false, TestName = "ApplyUp_TransactionScopeNone_WithoutLogger")]
        [TestCase(TransactionScope.SingleMigration, false, TestName = "ApplyUp_TransactionScopeSingleMigration_WithoutLogger")]
        [TestCase(TransactionScope.AllMigrations, false, TestName = "ApplyUp_TransactionScopeAllMigrations_WithoutLogger")]
        [TestCase(TransactionScope.None, true, TestName = "ApplyUp_TransactionScopeNone_WithLogger")]
        [TestCase(TransactionScope.SingleMigration, true, TestName = "ApplyUp_TransactionScopeSingleMigration_WithLogger")]
        [TestCase(TransactionScope.AllMigrations, true, TestName = "ApplyUp_TransactionScopeAllMigrations_WithLogger")]
        public async Task ApplyUp(TransactionScope transactionScope, bool withLogger)
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
                Type = MigrationResultType.Upgraded,
                AppliedMigrations = migrations.ToList(),
                InitialVersion = null,
                FinalVersion = migrations[2].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };
            var migrator = SetupMigrator(migrations, transactionScope, withLogger);

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

        [TestCase(TransactionScope.None, false, TestName = "ApplyDown_TransactionScopeNone_WithoutLogger")]
        [TestCase(TransactionScope.SingleMigration, false, TestName = "ApplyDown_TransactionScopeSingleMigration_WithoutLogger")]
        [TestCase(TransactionScope.AllMigrations, false, TestName = "ApplyDown_TransactionScopeAllMigrations_WithoutLogger")]
        [TestCase(TransactionScope.None, true, TestName = "ApplyDown_TransactionScopeNone_WithLogger")]
        [TestCase(TransactionScope.SingleMigration, true, TestName = "ApplyDown_TransactionScopeSingleMigration_WithLogger")]
        [TestCase(TransactionScope.AllMigrations, true, TestName = "ApplyDown_TransactionScopeAllMigrations_WithLogger")]
        public async Task ApplyDown(TransactionScope transactionScope, bool withLogger)
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
                Type = MigrationResultType.Downgraded,
                AppliedMigrations = migrations.Reverse().Take(2).ToList(),
                InitialVersion = migrations[2].Version,
                FinalVersion = migrations[0].Version,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };
            var migrator = SetupMigrator(migrations, transactionScope, withLogger);

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

        [TestCase(false, TestName = "TargetVersionEqualsCurrent_WithoutLogger")]
        [TestCase(true, TestName = "TargetVersionEqualsCurrent_WithLogger")]
        public async Task TargetVersionEqualsCurrent(bool withLogger)
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
            var migrator = SetupMigrator(migrations, TransactionScope.None, withLogger);

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

        [TestCase(false, TestName = "FirstMigrationAlreadyApplied_WithoutLogger")]
        [TestCase(true, TestName = "FirstMigrationAlreadyApplied_WithLogger")]
        public async Task FirstMigrationAlreadyApplied(bool withLogger)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
            };

            var expectedResult = new MigrationResult
            {
                Type = MigrationResultType.Upgraded,
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

            var migrator = SetupMigrator(migrations, TransactionScope.None, withLogger);

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

        [TestCase(false, TestName = "RollbackLastMigration_WithoutLogger")]
        [TestCase(true, TestName = "RollbackLastMigration_WithLogger")]
        public async Task RollbackLastMigration(bool withLogger)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
            };

            var expectedResult = new MigrationResult
            {
                Type = MigrationResultType.Downgraded,
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

            var migrator = SetupMigrator(migrations, TransactionScope.None, withLogger);

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

        [TestCase(false, TestName = "RollbackLastMigration_WithoutLogger")]
        [TestCase(true, TestName = "RollbackLastMigration_WithLogger")]
        public async Task MigrationException_NoTransaction(bool withLogger)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_MigrationExc("0.0.1")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.None, withLogger);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<Exception>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().BeEmpty();
            actualTestDocs.Should().BeEmpty();
        }

        [TestCase(false, TestName = "MigrationException_SingleMigrationTransaction_WithoutLogger")]
        [TestCase(true, TestName = "MigrationException_SingleMigrationTransaction_WithLogger")]
        public async Task MigrationException_SingleMigrationTransaction(bool withLogger)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_MigrationExc("0.0.2")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.SingleMigration, withLogger);

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

        [TestCase(false, TestName = "MigrationException_AllMigrationsTransaction_Up_WithoutLogger")]
        [TestCase(true, TestName = "MigrationException_AllMigrationsTransaction_Up_WithLogger")]
        public async Task MigrationException_AllMigrationsTransaction_Up(bool withLogger)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_MigrationExc("0.0.2")
            };
            var migrator = SetupMigrator(migrations, TransactionScope.AllMigrations, withLogger);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<Exception>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().BeEmpty();
            actualTestDocs.Should().BeEmpty();
        }

        [TestCase(false, TestName = "MigrationException_AllMigrationsTransaction_Down_WithoutLogger")]
        [TestCase(true, TestName = "MigrationException_AllMigrationsTransaction_Down_WithLogger")]
        public async Task MigrationException_AllMigrationsTransaction_Down(bool withLogger)
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

            var migrator = SetupMigrator(migrations, TransactionScope.AllMigrations, withLogger);

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

        [TestCase(false, TestName = "IndexExists_WithoutLogger")]
        [TestCase(true, TestName = "IndexExists_WithLogger")]
        public async Task IndexExists(bool withLogger)
        {
            // Arrange
            var migrator = SetupMigrator(Enumerable.Empty<IMongoMigration>(), TransactionScope.None, withLogger);

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

        [TestCase(false, TestName = "OtherMigrationInProgress_Cancel_WithoutLogger")]
        [TestCase(true, TestName = "OtherMigrationInProgress_Cancel_WithLogger")]
        public async Task OtherMigrationInProgress_Cancel(bool withLogger)
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
                Type = MigrationResultType.Cancelled,
                AppliedMigrations = new List<IMongoMigration>(),
                InitialVersion = null,
                FinalVersion = null,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };
            var migrator = SetupMigrator(migrations, TransactionScope.None, withLogger, ParallelRunsBehavior.Cancel);
            var lockDoc = new MigrationLock { AcquiredAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            await _lockCollection.InsertOneAsync(lockDoc);

            // Act
            MigrationResult actualResult = await migrator.MigrateAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();
            MigrationLock actualLockDoc = await _lockCollection.Find(FilterDefinition<MigrationLock>.Empty).FirstOrDefaultAsync();

            actualHistoryDocs.Should().BeEmpty();
            actualTestDocs.Should().BeEmpty();
            actualLockDoc.AcquiredAt.Should().Be(lockDoc.AcquiredAt);
            VerifyMigrationResult(actualResult, expectedResult);
        }

        [TestCase(false, TestName = "OtherMigrationInProgress_Throw_WithoutLogger")]
        [TestCase(true, TestName = "OtherMigrationInProgress_Throw_WithLogger")]
        public async Task OtherMigrationInProgress_Throw(bool withLogger)
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1"),
                new MigratorTest_Migration("0.0.2"),
                new MigratorTest_Migration("0.0.3"),
            };
            var migrator = SetupMigrator(migrations, TransactionScope.None, withLogger, ParallelRunsBehavior.Throw);
            var lockDoc = new MigrationLock { AcquiredAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            await _lockCollection.InsertOneAsync(lockDoc);

            // Act
            Func<Task> migrateFunc = async () => await migrator.MigrateAsync();

            // Assert
            await migrateFunc.Should().ThrowAsync<MigrationInProgressException>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();
            MigrationLock actualLockDoc = await _lockCollection.Find(FilterDefinition<MigrationLock>.Empty).FirstOrDefaultAsync();

            actualHistoryDocs.Should().BeEmpty();
            actualTestDocs.Should().BeEmpty();
            actualLockDoc.AcquiredAt.Should().Be(lockDoc.AcquiredAt);
        }

        [TestCase(false, TestName = "ParallelMigrations_FirstApplied_SecondCancelled_WithoutLogger")]
        [TestCase(true, TestName = "ParallelMigrations_FirstApplied_SecondCancelled_WithLogger")]
        public async Task ParallelMigrations_FirstApplied_SecondCancelled(bool withLogger)
        {
            // Arrange
            var completionSource = new TaskCompletionSource();
            var migrationA = new MigratorTest_MigrationManualCompletion(completionSource.Task);
            var migrationsA = new IMongoMigration[]
            {
                migrationA
            };
            var migrationsB = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1")
            };

            var expectedResultA = new MigrationResult
            {
                Type = MigrationResultType.Upgraded,
                AppliedMigrations = migrationsA.ToList(),
                InitialVersion = null,
                FinalVersion = "0.0.1",
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now.AddSeconds(10)
            };
            var expectedResultB = new MigrationResult
            {
                Type = MigrationResultType.Cancelled,
                AppliedMigrations = new List<IMongoMigration>(),
                InitialVersion = null,
                FinalVersion = null,
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now
            };
            var migratorA = SetupMigrator(migrationsA, TransactionScope.None, withLogger, ParallelRunsBehavior.Cancel);
            var migratorB = SetupMigrator(migrationsB, TransactionScope.None, withLogger, ParallelRunsBehavior.Cancel);

            // Act
            Task<MigrationResult> actualResultTaskA = migratorA.MigrateAsync();
            await migrationA.StartedTask;
            MigrationResult actualResultB = await migratorB.MigrateAsync();
            completionSource.SetResult();
            MigrationResult actualResultA = await actualResultTaskA;

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();
            MigrationLock actualLockDoc = await _lockCollection.Find(FilterDefinition<MigrationLock>.Empty).FirstOrDefaultAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.Contain(x => x.Name == migrationA.Name && x.Version == migrationA.Version);

            actualTestDocs.Should().HaveCount(1)
                .And.Contain(x => x.Version == migrationA.Version);

            actualLockDoc.Should().BeNull();

            VerifyMigrationResult(actualResultA, expectedResultA);
            VerifyMigrationResult(actualResultB, expectedResultB);
        }

        [TestCase(false, TestName = "ParallelMigrations_FirstApplied_SecondThrows_WithoutLogger")]
        [TestCase(true, TestName = "ParallelMigrations_FirstApplied_SecondThrows_WithLogger")]
        public async Task ParallelMigrations_FirstApplied_SecondThrows(bool withLogger)
        {
            // Arrange
            var completionSource = new TaskCompletionSource();
            var migrationA = new MigratorTest_MigrationManualCompletion(completionSource.Task);
            var migrationsA = new IMongoMigration[]
            {
                migrationA
            };
            var migrationsB = new IMongoMigration[]
            {
                new MigratorTest_Migration("0.0.1")
            };

            var expectedResultA = new MigrationResult
            {
                Type = MigrationResultType.Upgraded,
                AppliedMigrations = migrationsA.ToList(),
                InitialVersion = null,
                FinalVersion = "0.0.1",
                StartTime = DateTime.Now,
                FinishTime = DateTime.Now.AddSeconds(10)
            };
            var migratorA = SetupMigrator(migrationsA, TransactionScope.None, withLogger, ParallelRunsBehavior.Throw);
            var migratorB = SetupMigrator(migrationsB, TransactionScope.None, withLogger, ParallelRunsBehavior.Throw);

            // Act
            Task<MigrationResult> actualResultTaskA = migratorA.MigrateAsync();
            await migrationA.StartedTask;
            Task<MigrationResult> actualResultTaskB = migratorB.MigrateAsync();
            Task.WaitAny(actualResultTaskB);
            completionSource.SetResult();
            MigrationResult actualResultA = await actualResultTaskA;

            // Assert
            await ((Func<Task>)(() => actualResultTaskB)).Should().ThrowAsync<MigrationInProgressException>();

            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();
            MigrationLock actualLockDoc = await _lockCollection.Find(FilterDefinition<MigrationLock>.Empty).FirstOrDefaultAsync();

            actualHistoryDocs.Should().HaveCount(1)
                .And.Contain(x => x.Name == migrationA.Name && x.Version == migrationA.Version);

            actualTestDocs.Should().HaveCount(1)
                .And.Contain(x => x.Version == migrationA.Version);

            actualLockDoc.Should().BeNull();

            VerifyMigrationResult(actualResultA, expectedResultA);
        }

        private Migrator SetupMigrator(IEnumerable<IMongoMigration> migrations, TransactionScope transactionScope, bool withLogger,
            ParallelRunsBehavior parallelRunsBehavior = ParallelRunsBehavior.Cancel)
        {
            var locatorMock = new Mock<IMigrationsLocator>();
            locatorMock.Setup(x => x.Locate()).Returns(migrations);

            var options = new MigrationOptions(DatabaseName)
            {
                MigrationsCollectionName = MigrationsCollectionName,
                TransactionScope = transactionScope,
                ParallelRunsBehavior = parallelRunsBehavior
            };
            var logger = withLogger ? new NullLogger<Migrator>() : null;
            var migrator = new Migrator(locatorMock.Object, _client, options, logger);

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

        class MigratorTest_MigrationManualCompletion : MongoMigration
        {
            private readonly Task _task;
            private readonly TaskCompletionSource startedTaskSource = new TaskCompletionSource();

            public Task StartedTask => startedTaskSource.Task;

            public MigratorTest_MigrationManualCompletion(Task task) : base("0.0.1", "0.0.1")
            {
                _task = task;
            }

            public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                startedTaskSource.SetResult();
                IMongoCollection<TestDoc> collection = db.GetCollection<TestDoc>(DocCollectionName);
                var doc = new TestDoc { Version = Version.ToString() };
                await collection.InsertOneAsync(doc, null, cancellationToken);
                await _task;
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }

        [BsonIgnoreExtraElements]
        class TestDoc
        {
            public string Version { get; set; }
        }
    }
}