using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    public interface IMigrator
    {
        Task MigrateAsync(DatabaseVersion targetVersion = default, CancellationToken cancellationToken = default);
    }
}