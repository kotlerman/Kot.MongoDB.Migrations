using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Base class that represents a Mongo migration.
    /// </summary>
    public abstract class MongoMigration : IMongoMigration
    {
        /// <inheritdoc/>
        public DatabaseVersion Version { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MongoMigration"/>.
        /// </summary>
        /// <param name="version">Migration version.</param>
        protected MongoMigration(DatabaseVersion version)
        {
            Version = version;
            Name = GetType().Name;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MongoMigration"/>.
        /// </summary>
        /// <param name="version">Migration version.</param>
        /// <param name="name">Migration name.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
        protected MongoMigration(DatabaseVersion version, string name)
        {
            Version = version;
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentNullException(nameof(name)) : name;
        }

        /// <inheritdoc/>
        public abstract Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken);
    }
}
