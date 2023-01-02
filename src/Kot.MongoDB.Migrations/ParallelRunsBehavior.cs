namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Specifies behavior when there is other migration in progress.
    /// </summary>
    public enum ParallelRunsBehavior
    {
        /// <summary>
        /// Cancel current run.
        /// </summary>
        Cancel,

        /// <summary>
        /// Throw an exception.
        /// </summary>
        Throw
    }
}
