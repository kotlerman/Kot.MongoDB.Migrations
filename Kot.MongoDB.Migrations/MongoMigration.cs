using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    public abstract class MongoMigration : IMongoMigration
    {
        public DatabaseVersion Version { get; }

        public string Name { get; }

        protected MongoMigration(DatabaseVersion version)
        {
            Version = version;
            Name = GetType().Name;
        }

        protected MongoMigration(DatabaseVersion version, string name)
        {
            Version = version;
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentNullException(nameof(name)) : name;
        }

        public abstract Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);

        public abstract Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);
    }
}
