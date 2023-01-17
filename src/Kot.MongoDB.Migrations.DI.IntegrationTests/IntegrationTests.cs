using FluentAssertions;
using Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations;
using Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations.Subfolder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mongo2Go;
using MongoDB.Driver;
using NUnit.Framework;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.DI.IntegrationTests
{
    [TestFixture]
    public class IntegrationTests
    {
        private const string DatabaseName = "IntegrationTest";
        private const string MigrationsCollectionName = "MigrationHistory";
        private const string SubfolderNamespace = "Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations.Subfolder";
        private const string ServiceTestValue = "TestValue";

        private StringWriter _logWriter;
        private Assembly _externalMigrationsAssembly;
        private MongoDbRunner _runner;
        private IMongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<MigrationHistory> _histCollection;
        private IMongoCollection<TestDoc> _docCollection;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _externalMigrationsAssembly = CompileAndLoadAssemblyWithMigration();
        }

        [SetUp]
        public void Setup()
        {
            _logWriter = new StringWriter();

            Microsoft.Extensions.Logging.ILogger logger = LoggerFactory
                .Create(config => config.SetMinimumLevel(LogLevel.Error).AddConsole())
                .CreateLogger("Mongo2Go");

            _runner = MongoDbRunner.Start(singleNodeReplSet: true, logger: logger);
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
        public async Task Migrate_WithExternalClient_Default()
        {
            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };
            await MigrateWithExternalClient(null, expectedVersions);
        }

        [Test]
        public async Task Migrate_WithExternalClient_FromCurrentDomain()
        {
            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };
            await MigrateWithExternalClient(config => config.LoadMigrationsFromCurrentDomain(), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithExternalClient_FromExecutingAssembly()
        {
            var expectedVersions = Enumerable.Empty<string>();
            await MigrateWithExternalClient(config => config.LoadMigrationsFromExecutingAssembly(), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithExternalClient_FromAssembly()
        {
            var expectedVersions = new[] { "0.0.4" };
            await MigrateWithExternalClient(config => config.LoadMigrationsFromAssembly(_externalMigrationsAssembly), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithExternalClient_FromNamespace()
        {
            var expectedVersions = new[] { "0.0.3" };
            await MigrateWithExternalClient(config => config.LoadMigrationsFromNamespace(SubfolderNamespace), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithExternalClient_MigrationsCollection()
        {
            var testService = new TestService() { TestValue = ServiceTestValue };
            var migrations = new IMongoMigration[]
            {
                new SimpleMigration001(testService),
                new SimpleMigration003(testService)
            };
            var expectedVersions = new[] { "0.0.1", "0.0.3" };
            await MigrateWithExternalClient(config => config.LoadMigrations(migrations), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithSpecificClient_Default()
        {
            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };
            await MigrateWithSpecificClient(null, expectedVersions);
        }

        [Test]
        public async Task Migrate_WithSpecificClient_FromCurrentDomain()
        {
            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };
            await MigrateWithSpecificClient(config => config.LoadMigrationsFromCurrentDomain(), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithSpecificClient_FromExecutingAssembly()
        {
            var expectedVersions = Enumerable.Empty<string>();
            await MigrateWithSpecificClient(config => config.LoadMigrationsFromExecutingAssembly(), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithSpecificClient_FromAssembly()
        {
            var expectedVersions = new[] { "0.0.4" };
            await MigrateWithSpecificClient(config => config.LoadMigrationsFromAssembly(_externalMigrationsAssembly), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithSpecificClient_FromNamespace()
        {
            var expectedVersions = new[] { "0.0.3" };
            await MigrateWithSpecificClient(config => config.LoadMigrationsFromNamespace(SubfolderNamespace), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithSpecificClient_MigrationsCollection()
        {
            var testService = new TestService() { TestValue = ServiceTestValue };
            var migrations = new IMongoMigration[]
            {
                new SimpleMigration001(testService),
                new SimpleMigration003(testService)
            };
            var expectedVersions = new[] { "0.0.1", "0.0.3" };
            await MigrateWithSpecificClient(config => config.LoadMigrations(migrations), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithConnectionString_Default()
        {
            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };
            await MigrateWithConnectionString(null, expectedVersions);
        }

        [Test]
        public async Task Migrate_WithConnectionString_FromCurrentDomain()
        {
            var expectedVersions = new[] { "0.0.1", "0.0.2", "0.0.3", "0.0.4" };
            await MigrateWithConnectionString(config => config.LoadMigrationsFromCurrentDomain(), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithConnectionString_FromExecutingAssembly()
        {
            var expectedVersions = Enumerable.Empty<string>();
            await MigrateWithConnectionString(config => config.LoadMigrationsFromExecutingAssembly(), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithConnectionString_FromAssembly()
        {
            var expectedVersions = new[] { "0.0.4" };
            await MigrateWithConnectionString(config => config.LoadMigrationsFromAssembly(_externalMigrationsAssembly), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithConnectionString_FromNamespace()
        {
            var expectedVersions = new[] { "0.0.3" };
            await MigrateWithConnectionString(config => config.LoadMigrationsFromNamespace(SubfolderNamespace), expectedVersions);
        }

        [Test]
        public async Task Migrate_WithConnectionString_MigrationsCollection()
        {
            var testService = new TestService() { TestValue = ServiceTestValue };
            var migrations = new IMongoMigration[]
            {
                new SimpleMigration001(testService),
                new SimpleMigration003(testService)
            };
            var expectedVersions = new[] { "0.0.1", "0.0.3" };
            await MigrateWithConnectionString(config => config.LoadMigrations(migrations), expectedVersions);
        }

        [TestCase(false, TestName = "Migrate_WithSpecificClient_WithoutLogs")]
        [TestCase(true, TestName = "Migrate_WithSpecificClient_WithLogs")]
        public async Task Migrate_WithSpecificClient_Logs(bool withLogs)
        {
            // Arrange
            var testService = new TestService() { TestValue = ServiceTestValue };

            // Act
            await MigrateWithSpecificClient(config => config.LoadMigrations(Array.Empty<IMongoMigration>()), Array.Empty<string>(), withLogs);

            // Assert
            var expectedLogs = withLogs ? LogStrings.EmptyMigrations : "";
            _logWriter.ToString().Should().Be(expectedLogs);
        }

        [TestCase(false, TestName = "Migrate_WithExternalClient_WithoutLogs")]
        [TestCase(true, TestName = "Migrate_WithExternalClient_WithLogs")]
        public async Task Migrate_WithExternalClient_Logs(bool withLogs)
        {
            // Arrange
            var testService = new TestService() { TestValue = ServiceTestValue };

            // Act
            await MigrateWithExternalClient(config => config.LoadMigrations(Array.Empty<IMongoMigration>()), Array.Empty<string>(), withLogs);

            // Assert
            var expectedLogs = withLogs ? LogStrings.EmptyMigrations : "";
            _logWriter.ToString().Should().Be(expectedLogs);
        }

        [TestCase(false, TestName = "Migrate_WithConnectionString_WithoutLogs")]
        [TestCase(true, TestName = "Migrate_WithConnectionString_WithLogs")]
        public async Task Migrate_WithConnectionString_Logs(bool withLogs)
        {
            // Arrange
            var testService = new TestService() { TestValue = ServiceTestValue };

            // Act
            await MigrateWithConnectionString(config => config.LoadMigrations(Array.Empty<IMongoMigration>()), Array.Empty<string>(), withLogs);

            // Assert
            var expectedLogs = withLogs ? LogStrings.EmptyMigrations : "";
            _logWriter.ToString().Should().Be(expectedLogs);
        }

        private async Task MigrateWithExternalClient(Action<DIMigrationsLocationConfigurator> configure,
            IEnumerable<string> expectedVersions, bool withLogs = true)
        {
            await TestMigration((services, options) =>
            {
                services.AddSingleton(_client);
                services.AddMongoMigrations(options, configure);
            },
            expectedVersions,
            withLogs);
        }

        private async Task MigrateWithSpecificClient(Action<DIMigrationsLocationConfigurator> configure,
            IEnumerable<string> expectedVersions, bool withLogs = true)
        {
            await TestMigration((services, options) =>
            {
                services.AddMongoMigrations(_client, options, configure);
            },
            expectedVersions,
            withLogs);
        }

        private async Task MigrateWithConnectionString(Action<DIMigrationsLocationConfigurator> configure,
            IEnumerable<string> expectedVersions, bool withLogs = true)
        {
            await TestMigration((services, options) =>
            {
                services.AddMongoMigrations(_runner.ConnectionString, options, configure);
            },
            expectedVersions,
            withLogs);
        }

        private async Task TestMigration(Action<IServiceCollection, MigrationOptions> configure, IEnumerable<string> expectedVersions, bool withLogger)
        {
            // Arrange
            var testService = new TestService { TestValue = ServiceTestValue };
            var options = new MigrationOptions(DatabaseName) { MigrationsCollectionName = MigrationsCollectionName };

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    configure(services, options);
                    services.AddSingleton<ITestService>(testService);
                    services.AddHostedService<HostedService>();

                    if (!withLogger)
                    {
                        services.AddSingleton<ILogger<Migrator>>(_ => null);
                    }
                })
                .UseSerilog((_, logging) =>
                {
                    logging.MinimumLevel.Debug();
                    logging.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning);
                    logging.MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Warning);
                    logging.WriteTo.TextWriter(_logWriter, outputTemplate: "[{Level}] {Message}\n{Exception}");
                })
                .Build();

            // Act
            await host.RunAsync();

            // Assert
            List<MigrationHistory> actualHistoryDocs = await _histCollection.Find(FilterDefinition<MigrationHistory>.Empty).ToListAsync();
            List<TestDoc> actualTestDocs = await _docCollection.Find(FilterDefinition<TestDoc>.Empty).ToListAsync();

            actualHistoryDocs.Select(x => x.Version.ToString()).Should().BeEquivalentTo(expectedVersions);
            actualTestDocs.Select(x => x.ValueA.ToString()).Should().BeEquivalentTo(expectedVersions);
            actualTestDocs.Select(x => x.ValueB).Should().AllBe(ServiceTestValue);
        }

        private static Assembly CompileAndLoadAssemblyWithMigration()
        {
            string code = @"
                using Kot.MongoDB.Migrations.DI.IntegrationTests;
                using Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations;

                namespace Kot.MigrationsAssembly
                {
                    public class SimpleMigration004 : TestMigrationBase
                    {
                        public SimpleMigration004(ITestService testService) : base(""0.0.4"", testService)
                        {
                        }
                    }
                }
            ";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            MetadataReference[] references = new[]
            {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location),
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoClient).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoMigration).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IntegrationTests).GetTypeInfo().Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create("Kot.MigrationsAssembly")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree);

            using var memoryStream = new MemoryStream();
            compilation.Emit(memoryStream);

            return AppDomain.CurrentDomain.Load(memoryStream.ToArray());
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

        static class LogStrings
        {
            public const string EmptyMigrations = "[Information] Starting migration.\n" +
                "[Debug] Acquiring DB lock.\n" +
                "[Debug] Creating indexes for migrations history collection.\n" +
                "[Debug] Getting current DB version.\n" +
                "[Information] Current DB version is \"0.0.0\".\n" +
                "[Debug] Locating migrations.\n" +
                "[Information] Found 0 migrations.\n" +
                "[Information] The DB is up-to-date.\n" +
                "[Debug] Releasing DB lock.\n" +
                "[Debug] DB lock released.\n" +
                "[Information] Migration completed.\n";
        }
    }
}
