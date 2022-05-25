using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    public class ActivatorMigrationInstantiator : IMigrationInstantiator
    {
        public IMongoMigration Instantiate(Type migrationType) => (IMongoMigration)Activator.CreateInstance(migrationType);
    }
}
