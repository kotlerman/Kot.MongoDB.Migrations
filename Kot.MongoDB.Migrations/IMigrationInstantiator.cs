using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    public interface IMigrationInstantiator
    {
        IMongoMigration Instantiate(Type migrationType);
    }
}
