using FluentAssertions;
using Kot.MongoDB.Migrations.Exceptions;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class ActivatorMigrationInstantiatorTests
    {
        private const string DbVersion = "1.0.0";

        [Test]
        public void Instantiation_Success()
        {
            // Arrange
            var activator = new ActivatorMigrationInstantiator();

            // Act && Assert
            IMongoMigration migration = activator.Instantiate(typeof(Migration_Ok));
            migration.Version.Should().Be(DbVersion);
        }

        [TestCase(typeof(Migration_Abstract))]
        [TestCase(typeof(Migration_NoPublicConstructor))]
        [TestCase(typeof(Migration_NoParameterlessConstructor))]
        public void Instantiation_Failure(Type migrationType)
        {
            // Arrange
            var activator = new ActivatorMigrationInstantiator();

            // Act
            Func<IMongoMigration> instantiateFunc = () => activator.Instantiate(migrationType);

            // Assert
            instantiateFunc.Should().Throw<MigrationInstantiationException>();
        }

        private class Migration_Ok : MongoMigration
        {
            public Migration_Ok() : base(DbVersion, DbVersion)
            {
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }

        private abstract class Migration_Abstract : MongoMigration
        {
            public Migration_Abstract() : base(DbVersion, DbVersion)
            {
            }
        }

        private class Migration_NoPublicConstructor : MongoMigration
        {
            private Migration_NoPublicConstructor() : base(DbVersion, DbVersion)
            {
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }

        private class Migration_NoParameterlessConstructor : MongoMigration
        {
            private Migration_NoParameterlessConstructor() : base(DbVersion, DbVersion)
            {
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }
    }
}
