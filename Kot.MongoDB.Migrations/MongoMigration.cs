using MongoDB.Driver;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    public abstract class MongoMigration : IMongoMigration
    {
        public DatabaseVersion Version { get; }

        public string Name { get; }

        protected MongoMigration(DatabaseVersion version, string name)
        {
            Version = version;
            Name = name;
        }

        public abstract Task DownAsync(IMongoDatabase db, IClientSessionHandle session);

        public abstract Task UpAsync(IMongoDatabase db, IClientSessionHandle session);
    }
}
