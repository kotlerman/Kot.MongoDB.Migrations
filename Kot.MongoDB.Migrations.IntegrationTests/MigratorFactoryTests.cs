using FluentAssertions;
using Mongo2Go;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.IntegrationTests
{
    [TestFixture]
    public class MigratorFactoryTests
    {
        private const string DatabaseName = "IntegrationTest";
        private const string MigrationsCollectionName = "MigrationHistory";
        private const string DocCollectionName = "DocCollection";
        private const string MigrationVersion = "1.0.0";

        private MongoDbRunner _runner;
        private IMongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<MigrationHistory> _histCollection;
        private IMongoCollection<TestDoc> _docCollection;

        [SetUp]
        public void Setup()
        {
            _runner = MongoDbRunner.Start(singleNodeReplSet: true);
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
        public async Task MigrateSuccess()
        {
            // Arrange
            var options = new MigrationOptions(DatabaseName) { MigrationsCollectionName = MigrationsCollectionName };
            var migrator = MigratorFactory.Create(_client, options);

            // Act
            await migrator.MigrateAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1).And.ContainSingle(x => x.Version == MigrationVersion);
            actualTestDocs.Should().HaveCount(1).And.ContainSingle(x => x.Value == MigrationVersion);
        }

        class TestMigration : MongoMigration
        {
            public TestMigration() : base(MigrationVersion, MigrationVersion)
            {
            }

            public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                var collection = db.GetCollection<TestDoc>(DocCollectionName);
                await collection.InsertOneAsync(new TestDoc { Value = MigrationVersion }, null, cancellationToken);
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }

        [BsonIgnoreExtraElements]
        class TestDoc
        {
            public string Value { get; set; }
        }
    }
}
