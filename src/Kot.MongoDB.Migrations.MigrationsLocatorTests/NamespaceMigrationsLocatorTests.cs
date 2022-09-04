using FluentAssertions;
using Kot.MongoDB.Migrations.Locators;
using NUnit.Framework;
using System;
using System.Linq;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests
{
    [TestFixture, Order(0)]
    public class NamespaceMigrationsLocatorTests
    {
        private readonly string[] _migrationVersions = new[] { "2.0.3" };

        [Test]
        public void Success()
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();
            var locator = new NamespaceMigrationsLocator(instantiator, "Kot.MongoDB.Migrations.MigrationsLocatorTests.Migrations.Subfolder");

            // Act
            IMongoMigration[] migrations = locator.Locate().ToArray();

            // Assert
            migrations.Length.Should().Be(_migrationVersions.Length);

            for (int i = 0; i < _migrationVersions.Length; i++)
            {
                Assert.AreEqual(_migrationVersions[i], migrations[i].Version.ToString());
            }
        }

        [TestCase(null, Description = "Failure_NullNamespace")]
        [TestCase("", Description = "Failure_EmptyNamespace")]
        public void Failure_EmptyNamespace(string @namespace)
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();

            // Act && Assert
            Assert.Throws<ArgumentNullException>(() => new NamespaceMigrationsLocator(instantiator, @namespace));
        }

        [Test]
        public void Failure_NullInstantiator()
        {
            // Act && Assert
            Assert.Throws<ArgumentNullException>(() => new NamespaceMigrationsLocator(null, "Test"));
        }
    }
}
