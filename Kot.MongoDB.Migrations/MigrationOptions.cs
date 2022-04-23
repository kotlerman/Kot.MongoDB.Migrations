namespace Kot.MongoDB.Migrations
{
    public class MigrationOptions
    {
        public string DatabaseName { get; set; }

        public string MigrationsCollectionName { get; set; }

        public TransactionScope TransactionScope { get; set; }
    }
}
