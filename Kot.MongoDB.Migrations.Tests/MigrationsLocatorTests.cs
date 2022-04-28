using Kot.MongoDB.Migrations.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class MigrationsLocatorTests
    {
        private const string TestMigrationTypePrefix = "MigrationsLocatorTest_MongoMigration";
        private readonly MigrationsLocator _locator = new();
        private readonly string[] _migrationVersions = new[] { "123.456.1", "123.456.2", "123.456.3" };

        [Test, Order(1)]
        public void Success()
        {
            // Arrange
            string[] migrationCodes = _migrationVersions.Select(GenerateMigrationCode).ToArray();
            CompileAndLoadAssemblyWithMigrations("migrationsA", migrationCodes);

            // Act
            IMongoMigration[] migrations = _locator.Locate()
                .Where(x => x.GetType().Name.StartsWith(TestMigrationTypePrefix))
                .ToArray();

            // Assert
            Assert.AreEqual(_migrationVersions.Length, migrations.Length);

            for (int i = 0; i < _migrationVersions.Length; i++)
            {
                Assert.AreEqual(_migrationVersions[i], migrations[i].Version.ToString());
            }
        }

        [Test, Order(2)]
        public void Failure_DuplicateMigrationVersion()
        {
            // Arrange
            string[] migrationCodes = new[]
            {
                GenerateMigrationCode(_migrationVersions[0]),
            };

            CompileAndLoadAssemblyWithMigrations("migrationsB", migrationCodes);

            // Act & Assert
            Assert.Throws<DuplicateMigrationVersionException>(() => _locator.Locate());
        }

        private static void CompileAndLoadAssemblyWithMigrations(string name, IEnumerable<string> migrationCodes)
        {
            SyntaxTree[] syntaxTrees = migrationCodes.Select(code => CSharpSyntaxTree.ParseText(code)).ToArray();
            MetadataReference[] references = new[]
            {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location),
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoClient).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MongoMigration).GetTypeInfo().Assembly.Location)
            };

            CSharpCompilation compilation = CSharpCompilation.Create(name)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTrees);

            using var memoryStream = new MemoryStream();
            compilation.Emit(memoryStream);

            AppDomain.CurrentDomain.Load(memoryStream.ToArray());
        }

        private static string GenerateMigrationCode(string version)
        {
            string className = TestMigrationTypePrefix + version.Replace(".", "_");

            return @$"
                using MongoDB.Driver;
                using System.Threading;
                using System.Threading.Tasks;
                using Kot.MongoDB.Migrations;

                namespace Test
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
