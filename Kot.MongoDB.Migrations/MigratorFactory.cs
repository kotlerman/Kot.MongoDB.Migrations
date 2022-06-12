using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    public class MigratorFactory
    {
        public static IMigrator Create(IMongoClient mongoClient, MigrationOptions options)
        {
            IMigrationInstantiator instantiator = new ActivatorMigrationInstantiator();
            IMigrationsLocator locator = new MigrationsLocator(instantiator);
            IMigrator migrator = new Migrator(locator, mongoClient, options);
            return migrator;
        }
    }
}
