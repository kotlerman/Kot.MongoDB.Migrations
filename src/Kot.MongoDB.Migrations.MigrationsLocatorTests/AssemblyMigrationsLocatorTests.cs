using FluentAssertions;
using Kot.MongoDB.Migrations.Locators;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests
{
    [TestFixture, Order(0)]
    public class AssemblyMigrationsLocatorTests
    {
        private readonly string[] _migrationVersions = new[] { "2.0.1", "2.0.2", "2.0.3" };

        [Test]
        public void Success()
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();
            var locator = new AssemblyMigrationsLocator(instantiator, Assembly.GetExecutingAssembly());

            // Act
            IMongoMigration[] migrations = locator.Locate().ToArray();

            // Assert
            migrations.Length.Should().Be(_migrationVersions.Length);

            for (int i = 0; i < _migrationVersions.Length; i++)
            {
                Assert.AreEqual(_migrationVersions[i], migrations[i].Version.ToString());
            }
        }

        [Test]
        public void NullInstantiator_ThrowsException()
        {
            // Act && Assert
            Assert.Throws<ArgumentNullException>(() => new AssemblyMigrationsLocator(null, Assembly.GetExecutingAssembly()));
        }

        [Test]
        public void NullAssembly_ThrowsException()
        {
            // Act && Assert
            var instantiator = new ActivatorMigrationInstantiator();
            Assert.Throws<ArgumentNullException>(() => new AssemblyMigrationsLocator(instantiator, null));
        }
    }
}
