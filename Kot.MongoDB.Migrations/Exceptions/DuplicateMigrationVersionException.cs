namespace Kot.MongoDB.Migrations.Exceptions
{
    /// <summary>
    /// Represents an error caused by existence of several migrations that have the same version.
    /// </summary>
    public class DuplicateMigrationVersionException : MongoDbMigrationsException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateMigrationVersionException"/> class with a specified version.
        /// </summary>
        /// <param name="version">Migration version.</param>
        public DuplicateMigrationVersionException(DatabaseVersion version)
            : base($"There are several migrations with the same version: {version}.")
        {
        }
    }
}
