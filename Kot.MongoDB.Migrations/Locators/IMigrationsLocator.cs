using System.Collections.Generic;

namespace Kot.MongoDB.Migrations.Locators
{
    public interface IMigrationsLocator
    {
        IEnumerable<IMongoMigration> Locate();
    }
}