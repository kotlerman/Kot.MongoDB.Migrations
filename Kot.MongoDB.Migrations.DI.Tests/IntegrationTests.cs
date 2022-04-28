using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mongo2Go;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.DI.Tests
{
    [TestFixture]
    public class IntegrationTests
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
        public async Task Migrate_WithExternalClient()
        {
            await Migrate((services, options) =>
            {
                services.AddSingleton(_client);
                services.AddMongoMigrations(options);
            });
        }

        [Test]
        public async Task Migrate_WithSpecificClient()
        {
            await Migrate((services, options) =>
            {
                services.AddMongoMigrations(_client, options);
            });
        }

        [Test]
        public async Task Migrate_WithConnectionString()
        {
            await Migrate((services, options) =>
            {
                services.AddMongoMigrations(_runner.ConnectionString, options);
            });
        }

        private async Task Migrate(Action<IServiceCollection, MigrationOptions> configureMigrations)
        {
            // Arrange
            var testValue = "TestValue";
            var testService = new TestService { TestValue = testValue };
            var options = new MigrationOptions(DatabaseName) { MigrationsCollectionName = MigrationsCollectionName };

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    configureMigrations(services, options);
                    services.AddSingleton<ITestService>(testService);
                    services.AddHostedService<HostedService>();
                })
                .Build();

            // Act
            await host.RunAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Should().HaveCount(1).And.ContainSingle(x => x.Version == MigrationVersion);
            actualTestDocs.Should().HaveCount(1).And.ContainSingle(x => x.Value == testValue);
        }

        class HostedService : IHostedService
        {
            private readonly IMigrator _migrator;
            private readonly IHost _host;

            public HostedService(IMigrator migrator, IHost host)
            {
                _migrator = migrator;
                _host = host;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                await _migrator.MigrateAsync(cancellationToken: cancellationToken);
                await _host.StopAsync(cancellationToken);
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        class TestMigration : MongoMigration
        {
            private readonly string _testValue;

            public TestMigration(ITestService testService) : base(MigrationVersion, MigrationVersion)
            {
                _testValue = testService.TestValue;
            }

            public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            {
                var collection = db.GetCollection<TestDoc>(DocCollectionName);
                await collection.InsertOneAsync(new TestDoc { Value = _testValue }, null, cancellationToken);
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }

        [BsonIgnoreExtraElements]
        class TestDoc
        {
            public string Value { get; set; }
        }

        interface ITestService
        {
            string TestValue { get; set; }
        }

        class TestService : ITestService
        {
            public string TestValue { get; set; }
        }
    }
}
