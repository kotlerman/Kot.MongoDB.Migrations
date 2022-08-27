using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Defines Mongo database migration method.
    /// </summary>
    public interface IMigrator
    {
        /// <summary>
        /// Migrate a database to the <paramref name="targetVersion"/>. If actual version of the database is less than the
        /// <paramref name="targetVersion"/>, all the migrations that have version up to this value will be applied.
        /// Otherwise, all the migrations that have version greater than the <paramref name="targetVersion"/> will be rolled back.
        /// </summary>
        /// <param name="targetVersion">Target version.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see cref="Task"/> representing the migration operation.</returns>
        Task MigrateAsync(DatabaseVersion targetVersion = default, CancellationToken cancellationToken = default);
    }
}