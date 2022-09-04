using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Represents a Mongo migration.
    /// </summary>
    public interface IMongoMigration
    {
        /// <summary>
        /// Unique migration version.
        /// </summary>
        DatabaseVersion Version { get; }

        /// <summary>
        /// Migration name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// An action performed when migration is applied.
        /// </summary>
        /// <param name="db">Mongo database.</param>
        /// <param name="session">Client session. <see langword="null"/> when <see cref="MigrationOptions.TransactionScope"/>
        /// is <see cref="TransactionScope.None"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="Task"/> representing Up operation.</returns>
        Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);

        /// <summary>
        /// An action performed when migration is rolled back.
        /// </summary>
        /// <param name="db">Mongo database.</param>
        /// <param name="session">Client session. <see langword="null"/> when <see cref="MigrationOptions.TransactionScope"/>
        /// is <see cref="TransactionScope.None"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="Task"/> representing Down operation.</returns>
        Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);
    }
}
