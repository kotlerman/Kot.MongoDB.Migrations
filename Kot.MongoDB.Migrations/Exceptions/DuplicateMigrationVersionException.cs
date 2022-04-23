namespace Kot.MongoDB.Migrations.Exceptions
{
    public class DuplicateMigrationVersionException : MongoDbMigrationsException
    {
        public DuplicateMigrationVersionException(DatabaseVersion version)
            : base($"There are several migrations with the same version: {version}.")
        {
        }
    }
}
