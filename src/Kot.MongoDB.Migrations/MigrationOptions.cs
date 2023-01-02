using MongoDB.Driver;
using System;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Allows the user to customize how migrations are applied.
    /// </summary>
    public class MigrationOptions
    {
        /// <summary>
        /// Default migrations collection name.
        /// </summary>
        public static readonly string DefaultMigrationsCollectionName = "_migrations";

        private string _databaseName;
        private string _migrationsCollectionName = DefaultMigrationsCollectionName;

        /// <summary>
        /// Name of the database where migrations should be applied.
        /// </summary>
        public string DatabaseName
        {
            get => _databaseName;
            set => _databaseName = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException(nameof(DatabaseName))
                : value;
        }

        /// <summary>
        /// Name of a collection where migration history should be stored.
        /// </summary>
        public string MigrationsCollectionName
        {
            get => _migrationsCollectionName;
            set => _migrationsCollectionName = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException(nameof(MigrationsCollectionName))
                : value;
        }

        /// <summary>
        /// Name of a collection that is used for locking.
        /// </summary>
        public string MigrationsLockCollectionName => MigrationsCollectionName + ".lock";

        /// <summary>
        /// Specifies whether migrations are applied as part of a transaction or independently.
        /// </summary>
        public TransactionScope TransactionScope { get; set; } = TransactionScope.None;

        /// <summary>
        /// Specifies client session options when <see cref="TransactionScope"/> is not <see cref="TransactionScope.None"/>.
        /// </summary>
        public ClientSessionOptions ClientSessionOptions { get; set; }

        /// <summary>
        /// Specifies expected behavior when migration is already in progress.
        /// </summary>
        public ParallelRunsBehavior ParallelRunsBehavior { get; set; } = ParallelRunsBehavior.Cancel;

        /// <summary>
        /// Instantiates a new instance of <see cref="MigrationOptions"/>.
        /// </summary>
        /// <param name="databaseName">Name of the database where migrations should be applied.</param>
        public MigrationOptions(string databaseName)
        {
            DatabaseName = databaseName;
        }
    }
}
