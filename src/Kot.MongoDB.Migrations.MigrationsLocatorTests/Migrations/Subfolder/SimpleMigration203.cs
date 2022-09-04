using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.MigrationsLocatorTests.Migrations.Subfolder
{
    internal class SimpleMigration203 : MongoMigration
    {
        public SimpleMigration203() : base("2.0.3", "2.0.3")
        {
        }

        public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public override Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
