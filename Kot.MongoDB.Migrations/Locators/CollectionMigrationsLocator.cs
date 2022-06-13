using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    public class CollectionMigrationsLocator : IMigrationsLocator
    {
        private readonly IEnumerable<IMongoMigration> _migrations;

        public CollectionMigrationsLocator(IEnumerable<IMongoMigration> migrations)
        {
            DuplicateVersionChecker.EnsureNoDuplicateVersions(migrations);
            _migrations = migrations;
        }

        public IEnumerable<IMongoMigration> Locate() => _migrations;
    }
}
