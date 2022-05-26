using Kot.MongoDB.Migrations.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    public class ActivatorMigrationInstantiator : IMigrationInstantiator
    {
        public IMongoMigration Instantiate(Type migrationType)
        {
            try
            {
                return (IMongoMigration)Activator.CreateInstance(migrationType);
            }
            catch (Exception ex)
            {
                throw new MigrationInstantiationException(migrationType, ex);
            }
        }
    }
}
