﻿using FluentAssertions;
using Kot.MongoDB.Migrations.IntegrationTests.Migrations;
using Kot.MongoDB.Migrations.IntegrationTests.Migrations.Subfolder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NUnit.Framework;
using Serilog.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute;
using Testcontainers.MongoDb;

namespace Kot.MongoDB.Migrations.IntegrationTests
{
    [TestFixture]
    public class MigratorBuilderTests
    {
        private const string DatabaseName = "IntegrationTest";
        private const string MigrationsCollectionName = "MigrationHistory";

        private static readonly MigrationOptions Options = new MigrationOptions(DatabaseName)
        {
            MigrationsCollectionName = MigrationsCollectionName
        };

        private MongoDbContainer _container;
        private string _connectionString;
        private IMongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<MigrationHistory> _histCollection;
        private IMongoCollection<TestDoc> _docCollection;
        private Assembly _externalMigrationsAssembly;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _externalMigrationsAssembly = CompileAndLoadAssemblyWithMigration();

            var logger = LoggerFactory
                .Create(config => config.SetMinimumLevel(LogLevel.Error).AddConsole())
                .CreateLogger("MongoDbContainer");

            _container = new MongoDbBuilder()
                .WithImage("mongo:8.0")
                .WithReplicaSet()
                .WithLogger(logger)
                .Build();

            await _container.StartAsync();

            _connectionString = _container.GetConnectionString() + "?directConnection=true";
            _client = new MongoClient(_connectionString);
            _db = _client.GetDatabase(DatabaseName);
            _histCollection = _db.GetCollection<MigrationHistory>(MigrationsCollectionName);
            _docCollection = _db.GetCollection<TestDoc>(TestDoc.CollectionName);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _db.DropCollectionAsync(MigrationsCollectionName);
            await _db.DropCollectionAsync(TestDoc.CollectionName);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _container.StopAsync();
        }

        [Test]
        public async Task FromMongoClient_FromCurrentDomain()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromMongoClient(_client, Options)
                .LoadMigrationsFromCurrentDomain()
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromConnectionString_FromCurrentDomain()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromConnectionString(_connectionString, Options)
                .LoadMigrationsFromCurrentDomain()
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };

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
            IMigrator migrator = MigratorBuilder.FromConnectionString(_connectionString, Options)
                .LoadMigrationsFromExecutingAssembly()
                .Build();

            var expectedVersions = Enumerable.Empty<string>();

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromMongoClient_FromAssembly()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromMongoClient(_client, Options)
                .LoadMigrationsFromAssembly(_externalMigrationsAssembly)
                .Build();

            var expectedVersions = new[] { "0.0.4" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [Test]
        public async Task FromConnectionString_FromAssembly()
        {
            // Arrange
            IMigrator migrator = MigratorBuilder.FromConnectionString(_connectionString, Options)
                .LoadMigrationsFromAssembly(_externalMigrationsAssembly)
                .Build();

            var expectedVersions = new[] { "0.0.4" };

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
            IMigrator migrator = MigratorBuilder.FromConnectionString(_connectionString, Options)
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

            IMigrator migrator = MigratorBuilder.FromConnectionString(_connectionString, Options)
                .LoadMigrations(migrations)
                .Build();

            var expectedVersions = new[] { "0.0.1", "0.0.3" };

            // Act & Assert
            await TestMigration(migrator, expectedVersions);
        }

        [TestCase(false, TestName = "WithoutLogger_EmptyLogs")]
        [TestCase(true, TestName = "WithLogger_WritesLogs")]
        public async Task Logs(bool withLogger)
        {
            // Arrange
            var stringWriter = new StringWriter();

            MigratorBuilder migratorBuilder = MigratorBuilder.FromConnectionString(_connectionString, Options)
                .LoadMigrations(Enumerable.Empty<IMongoMigration>());

            if (withLogger)
            {
                var serilogLogger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.TextWriter(stringWriter, outputTemplate: "[{Level}] {Message}\n{Exception}")
                    .CreateLogger();

                migratorBuilder = migratorBuilder.WithLogger(new SerilogLoggerFactory(serilogLogger));
            }

            IMigrator migrator = migratorBuilder.Build();

            var expectedLog = withLogger
                ? "[Information] Starting migration.\n" +
                    "[Information] Current DB version is \"0.0.0\".\n" +
                    "[Information] Found 0 migrations.\n" +
                    "[Information] The DB is up-to-date.\n" +
                    "[Information] Migration completed.\n"
                : "";

            // Act
            await migrator.MigrateAsync();

            // Assert
            stringWriter.ToString().Should().Be(expectedLog);
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
            var builder = MigratorBuilder.FromMongoClient(Substitute.For<IMongoClient>(), Options);
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Test]
        public void WithLogger_ArgumentNullException()
        {
            var builder = MigratorBuilder.FromMongoClient(Substitute.For<IMongoClient>(), Options);
            Assert.Throws<ArgumentNullException>(() => builder.WithLogger(null));
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

        private static Assembly CompileAndLoadAssemblyWithMigration()
        {
            string code = @"
                using Kot.MongoDB.Migrations.IntegrationTests.Migrations;

                namespace Kot.MigrationsAssembly
                {
                    public class SimpleMigration004 : TestMigrationBase
                    {
                        public SimpleMigration004() : base(""0.0.4"")
                        {
                        }
                    }
                }
            ";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            MetadataReference[] references = new[]
            {
#if NET6_0
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location),
#endif
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoClient).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoMigration).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MigratorBuilderTests).GetTypeInfo().Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create("Kot.MigrationsAssembly")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree);

            using (var memoryStream = new MemoryStream())
            {
                compilation.Emit(memoryStream);
                return AppDomain.CurrentDomain.Load(memoryStream.ToArray());
            }
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
            new TestCaseData(Substitute.For<IMongoClient>(), null).SetName("FromMongoClient_NullOptions_ArgumentNullException"),
        };
    }
}
