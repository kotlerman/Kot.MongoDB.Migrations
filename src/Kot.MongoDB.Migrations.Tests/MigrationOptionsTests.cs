using FluentAssertions;
using NUnit.Framework;
using System;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class MigrationOptionsTests
    {
        private const string DatabaseName = "db";

        [Test]
        public void CreateOptions_Success()
        {
            // Act
            var options = new MigrationOptions(DatabaseName);

            // Assert
            options.DatabaseName.Should().Be(DatabaseName);
            options.MigrationsCollectionName.Should().NotBeNullOrWhiteSpace();
            options.TransactionScope.Should().Be(TransactionScope.None);
        }

        [TestCase(null, "collection", TestName = "CreateOptions_NullDbName")]
        [TestCase("   ", "collection", TestName = "CreateOptions_WhitespaceDbName")]
        [TestCase("db", null, TestName = "CreateOptions_NullCollectionName")]
        [TestCase("db", "   ", TestName = "CreateOptions_WhitespaceCollectionName")]
        public void CreateOptions_NullArgument(string dbName, string collectionName)
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var options = new MigrationOptions(dbName)
                {
                    MigrationsCollectionName = collectionName
                };
            });
        }
    }
}
