using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class MongoMigrationTests
    {
        [TestCase("", Description = "EmptyName_ThrowsException")]
        [TestCase(null, Description = "NullName_ThrowsException")]
        public void EmptyName_ThrowsException(string name)
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MigrationA(name));
        }

        class MigrationA : MongoMigration
        {
            public MigrationA(string name) : base(default, name)
            {
            }

            public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();

            public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
                => throw new NotImplementedException();
        }
    }
}
