namespace Kot.MongoDB.Migrations.Exceptions
{
    /// <summary>
    /// Represents an error caused by trying to run migration when there is other migration in progress.
    /// </summary>
    public class MigrationInProgressException : MongoDbMigrationsException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationInProgressException"/> class.
        /// </summary>
        public MigrationInProgressException() : base("There is other migration in progress.")
        {
        }
    }
}
