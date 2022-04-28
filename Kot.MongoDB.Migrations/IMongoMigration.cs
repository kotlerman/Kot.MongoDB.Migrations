using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    public interface IMongoMigration
    {
        DatabaseVersion Version { get; }
        string Name { get; }
        Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);
        Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);
    }
}
