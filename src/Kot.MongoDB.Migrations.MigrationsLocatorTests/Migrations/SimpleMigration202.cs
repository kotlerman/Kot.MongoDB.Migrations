using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests.Migrations
{
    internal class SimpleMigration202 : MongoMigration
    {
        public SimpleMigration202() : base("2.0.2", "2.0.2")
        {
        }

        public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
