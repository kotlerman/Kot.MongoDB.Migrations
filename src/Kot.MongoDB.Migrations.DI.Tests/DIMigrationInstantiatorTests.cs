using FluentAssertions;
using Kot.MongoDB.Migrations.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.DI.Tests
{
    [TestFixture]
    public class DIMigrationInstantiatorTests
    {
        private const string DbVersion = "1.0.0";

        private DIMigrationInstantiator _instantiator;
        private ServiceProvider _serviceProvider;

        [SetUp]
        public void SetUp()
        {
            var serviceCollection = new ServiceCollection();
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _instantiator = new DIMigrationInstantiator(_serviceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider.Dispose();
        }

        [Test]
        public void Instantiation_Success()
        {
            // Act && Assert
            IMongoMigration migration = _instantiator.Instantiate(typeof(Migration_Ok));
            migration.Version.Should().Be(DbVersion);
        }

        [TestCase(typeof(Migration_Abstract))]
        [TestCase(typeof(Migration_NoPublicConstructor))]
        [TestCase(typeof(Migration_NoParameterlessConstructor))]
        public void Instantiation_Failure(Type migrationType)
        {
            // Act
            Func<IMongoMigration> instantiateFunc = () => _instantiator.Instantiate(migrationType);

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
