using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests.Migrations
{
    internal class SimpleMigration201 : MongoMigration
    {
        public SimpleMigration201() : base("2.0.1", "2.0.1")
        {
        }

        public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
