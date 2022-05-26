using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations.Exceptions
{
    public class MigrationInstantiationException : MongoDbMigrationsException
    {
        public MigrationInstantiationException(Type migrationType, Exception innerException)
            : base($"Failed to instantiate migration class '{migrationType.FullName}'.", innerException)
        {
        }
    }
}
