namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Migration transaction scope.
    /// </summary>
    public enum TransactionScope
    {
        /// <summary>
        /// Apply migrations without transactions.
        /// </summary>
        None,

        /// <summary>
        /// Apply each migration in a separate transaction.
        /// </summary>
        SingleMigration,

        /// <summary>
        /// Apply all migrations in a single transaction.
        /// </summary>
        AllMigrations
    }
}
