using System.Collections.Generic;

namespace Kot.MongoDB.Migrations
{
    public interface IMigrationsLocator
    {
        IEnumerable<IMongoMigration> Locate();
    }
}