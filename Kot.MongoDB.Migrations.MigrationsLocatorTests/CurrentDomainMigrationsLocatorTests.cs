using FluentAssertions;
using Kot.MongoDB.Migrations.Locators;
using NUnit.Framework;
using System.Linq;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests
{
    [TestFixture, Order(0)]
    public class CurrentDomainMigrationsLocatorTests
    {
        private readonly string[] _migrationVersions = new[] { "2.0.1", "2.0.2", "2.0.3" };

        [Test]
        public void Success()
        {
            // Arrange
            var instantiator = new ActivatorMigrationInstantiator();
            var locator = new CurrentDomainMigrationsLocator(instantiator);

            // Act
            IMongoMigration[] migrations = locator.Locate().ToArray();

            // Assert
            migrations.Length.Should().Be(_migrationVersions.Length);

            for (int i = 0; i < _migrationVersions.Length; i++)
            {
                Assert.AreEqual(_migrationVersions[i], migrations[i].Version.ToString());
            }
        }
    }
}
