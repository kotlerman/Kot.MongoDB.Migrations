using Kot.MongoDB.Migrations.Exceptions;
using Kot.MongoDB.Migrations.Locators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests
{
    [TestFixture, Order(1)]
    public class FailureMigrationsLocatorTests
    {
        private const string MigrationsNamespace = "Kot.MongoDB.Migrations.MigrationsLocatorTests.Migrations.Duplicates";
        private static readonly Random Rand = new Random();
        private static Assembly CreatedAssembly;

        [OneTimeSetUp]
        public void Setup()
        {
            string[] migrationCodes = new[]
            {
                GenerateMigrationCode("2.0.5"),
                GenerateMigrationCode("2.0.5")
            };
            CreatedAssembly = CompileAndLoadAssemblyWithMigrations("migrationsA", migrationCodes);
        }

        [Test]
        public void CurrentDomainMigrationsLocator_DuplicateMigrationVersion()
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();
            var locator = new CurrentDomainMigrationsLocator(instantiator);

            // Act & Assert
            Assert.Throws<DuplicateMigrationVersionException>(() => locator.Locate());
        }

        [Test]
        public void AssemblyMigrationsLocator_DuplicateMigrationVersion()
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();
            var locator = new AssemblyMigrationsLocator(instantiator, CreatedAssembly);

            // Act & Assert
            Assert.Throws<DuplicateMigrationVersionException>(() => locator.Locate());
        }

        [Test]
        public void NamespaceMigrationsLocator_DuplicateMigrationVersion()
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();
            var locator = new NamespaceMigrationsLocator(instantiator, MigrationsNamespace);

            // Act & Assert
            Assert.Throws<DuplicateMigrationVersionException>(() => locator.Locate());
        }

        private static Assembly CompileAndLoadAssemblyWithMigrations(string name, IEnumerable<string> migrationCodes)
        {
            SyntaxTree[] syntaxTrees = migrationCodes.Select(code => CSharpSyntaxTree.ParseText(code)).ToArray();
            MetadataReference[] references = new[]
            {
#if NET6_0
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location),
#endif
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoClient).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoMigration).GetTypeInfo().Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(name)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTrees);

            using (var memoryStream = new MemoryStream())
            {
                compilation.Emit(memoryStream);
                return AppDomain.CurrentDomain.Load(memoryStream.ToArray());
            }
        }

        private static string GenerateMigrationCode(string version)
        {
            string className = "DuplicateMigration" + version.Replace(".", "_") + "_" + Rand.Next();

            return $@"
                using MongoDB.Driver;
                using System.Threading;
                using System.Threading.Tasks;
                using Kot.MongoDB.Migrations;

                namespace {MigrationsNamespace}
                {{
                    public class {className} : MongoMigration
                    {{
                        public {className}() : base(""{version}"", ""TestName"")
                        {{
                        }}

                        public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken token)
                            => Task.CompletedTask;

                        public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken token)
                            => Task.CompletedTask;
                    }}
                }}
            ";
        }
    }
}
