using MongoDB.Driver;
using System;

namespace Kot.MongoDB.Migrations
{
    public class MigrationOptions
    {
        public static readonly string DefaultMigrationsCollectionName = "_migrations";

        private string _databaseName;
        private string _migrationsCollectionName = DefaultMigrationsCollectionName;

        public string DatabaseName
        {
            get => _databaseName;
            set => _databaseName = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException(nameof(DatabaseName))
                : value;
        }

        public string MigrationsCollectionName
        {
            get => _migrationsCollectionName;
            set => _migrationsCollectionName = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentNullException(nameof(MigrationsCollectionName))
                : value;
        }

        public TransactionScope TransactionScope { get; set; } = TransactionScope.None;

        public ClientSessionOptions ClientSessionOptions { get; set; }

        public MigrationOptions(string databaseName)
        {
            DatabaseName = databaseName;
        }
    }
}
