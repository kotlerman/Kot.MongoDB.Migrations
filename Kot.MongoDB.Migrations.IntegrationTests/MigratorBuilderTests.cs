using FluentAssertions;
using Kot.MongoDB.Migrations.IntegrationTests.Migrations;
using Kot.MongoDB.Migrations.IntegrationTests.Migrations.Subfolder;
using Mongo2Go;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.IntegrationTests
{
    [TestFixture]
    public class MigratorBuilderTests
    {
        private const string DatabaseName = "IntegrationTest";
        private const string MigrationsCollectionName = "MigrationHistory";

        private static readonly MigrationOptions Options = new(DatabaseName)
        {
            MigrationsCollectionName = MigrationsCollectionName
        };

        private MongoDbRunner _runner;
        private IMongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<MigrationHistory> _histCollection;
        private IMongoCollection<TestDoc> _docCollection;

        [SetUp]
        public void SetUp()
        {
            _runner = MongoDbRunner.Start(singleNodeReplSet: true);
            _client = new MongoClient(_runner.ConnectionString);
            _db = _client.GetDatabase(DatabaseName);
            _histCollection = _db.GetCollection<MigrationHistory>(MigrationsCollectionName);
            _docCollection = _db.GetCollection<TestDoc>(TestDoc.CollectionName);
        }

        [TearDown]
        public void TearDown()
        {
            _runner.Dispose();
        }

        [Test]
        public async Task FromMongoClient_FromCurrentDomain()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromMongoClient(_client, Options)
                .LoadMigrationsFromCurrentDomain()
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromConnectionString_FromCurrentDomain()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromConnectionString(_runner.ConnectionString, Options)
                .LoadMigrationsFromCurrentDomain()
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromMongoClient_FromExecutingAssembly()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromMongoClient(_client, Options)
                .LoadMigrationsFromExecutingAssembly()
                .Build();

            var expectedVersions = Enumerable.Empty<string>();

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromConnectionString_FromExecutingAssembly()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromConnectionString(_runner.ConnectionString, Options)
                .LoadMigrationsFromExecutingAssembly()
                .Build();

            var expectedVersions = Enumerable.Empty<string>();

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromMongoClient_FromNamespace()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromMongoClient(_client, Options)
                .LoadMigrationsFromNamespace("Kot.MongoDB.Migrations.IntegrationTests.Migrations.Subfolder")
                .Build();

            var expectedVersions = new[] { "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromConnectionString_FromNamespace()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromConnectionString(_runner.ConnectionString, Options)
                .LoadMigrationsFromNamespace("Kot.MongoDB.Migrations.IntegrationTests.Migrations.Subfolder")
                .Build();

            var expectedVersions = new[] { "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromMongoClient_MigrationsCollection()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new SimpleMigration001(),
                new SimpleMigration003()
            };

            IMigrator migrator = MigratorBuilder.FromMongoClient(_client, Options)
                .LoadMigrations(migrations)
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromConnectionString_MigrationsCollection()
        {
            // Arrange
            var migrations = new IMongoMigration[]
            {
                new SimpleMigration001(),
                new SimpleMigration003()
            };

            IMigrator migrator = MigratorBuilder.FromConnectionString(_runner.ConnectionString, Options)
                .LoadMigrations(migrations)
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [TestCaseSource(nameof(FromConnectionStringTests))]
        public void FromConnectionString_ArgumentNullException(string connectionString, MigrationOptions options)
        {
            Assert.Throws<ArgumentNullException>(() => MigratorBuilder.FromConnectionString(connectionString, options));
        }

        [TestCaseSource(nameof(FromMongoClientTests))]
        public void FromMongoClient_ArgumentNullException(IMongoClient mongoClient, MigrationOptions options)
        {
            Assert.Throws<ArgumentNullException>(() => MigratorBuilder.FromMongoClient(mongoClient, options));
        }

        [Test]
        public void NoLocator_InvalidOperationException()
        {
            var builder = MigratorBuilder.FromMongoClient(new Mock<IMongoClient>().Object, Options);
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        private async Task TestMigration(IMigrator migrator, IEnumerable<string> expectedVersions)
        {
            // Act
            await migrator.MigrateAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Select(x => x.Version.ToString()).Should().BeEquivalentTo(expectedVersions);
            actualTestDocs.Select(x => x.Value.ToString()).Should().BeEquivalentTo(expectedVersions);
        }

        private static IEnumerable<TestCaseData> FromConnectionStringTests() => new[]
        {
            new TestCaseData(null, Options).SetName("FromConnectionString_NullString_ArgumentNullException"),
            new TestCaseData("", Options).SetName("FromConnectionString_EmptyString_ArgumentNullException"),
            new TestCaseData("ConnectionString", null).SetName("FromConnectionString_NullOptions_ArgumentNullException"),
        };

        private static IEnumerable<TestCaseData> FromMongoClientTests() => new[]
        {
            new TestCaseData(null, Options).SetName("FromMongoClient_NullClient_ArgumentNullException"),
            new TestCaseData(new Mock<IMongoClient>().Object, null).SetName("FromMongoClient_NullOptions_ArgumentNullException"),
        };
    }
}
