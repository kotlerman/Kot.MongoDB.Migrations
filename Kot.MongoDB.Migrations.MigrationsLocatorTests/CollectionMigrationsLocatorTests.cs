using FluentAssertions;
using Kot.MongoDB.Migrations.Exceptions;
using Kot.MongoDB.Migrations.Locators;
using Kot.MongoDB.Migrations.MigrationsLocatorTests.Migrations;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests
{
    [TestFixture, Order(0)]
    public class CollectionMigrationsLocatorTests
    {
        [Test]
        public void Success()
        {
            // Arrange
            var migrations = new MongoMigration[]
            {
                new SimpleMigration201(),
                new SimpleMigration202()
            };
            var locator = new CollectionMigrationsLocator(migrations);

            // Act
            IEnumerable<IMongoMigration> actualMigrations = locator.Locate();

            // Assert
            actualMigrations.Should().BeEquivalentTo(migrations);
        }

        [Test]
        public void DuplicateMigrationVersion_ThrowsException()
        {
            // Arrange
            var migrations = new MongoMigration[]
            {
                new SimpleMigration201(),
                new SimpleMigration201()
            };

            // Assert
            Assert.Throws<DuplicateMigrationVersionException>(() => new CollectionMigrationsLocator(migrations));
        }

        [Test]
        public void NullCollection_ThrowsException()
        {
            // Act && Assert
            Assert.Throws<ArgumentNullException>(() => new CollectionMigrationsLocator(null));
        }
    }
}
