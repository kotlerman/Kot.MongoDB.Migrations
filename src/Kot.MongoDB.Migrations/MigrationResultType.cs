namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Type of migration result.
    /// </summary>
    public enum MigrationResultType
    {
        /// <summary>
        /// DB was up-to-date.
        /// </summary>
        UpToDate,

        /// <summary>
        /// DB was upgraded.
        /// </summary>
        Upgraded,

        /// <summary>
        /// DB was downgraded.
        /// </summary>
        Downgraded,

        /// <summary>
        /// Migration was cancelled.
        /// </summary>
        Cancelled
    }
}
