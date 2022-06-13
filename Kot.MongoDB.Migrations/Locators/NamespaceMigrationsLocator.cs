using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    public class NamespaceMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;
        private readonly string _namespace;

        public NamespaceMigrationsLocator(IMigrationInstantiator instantiator, string @namespace)
        {
            _instantiator = instantiator;
            _namespace = string.IsNullOrEmpty(@namespace) ? throw new ArgumentNullException(nameof(@namespace)) : @namespace;
        }

        public IEnumerable<IMongoMigration> Locate()
        {
            IEnumerable<IMongoMigration> migrations = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.Namespace == _namespace && MigrationType.IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => _instantiator.Instantiate(type))
                .OrderBy(migration => migration.Version)
                .ToList();

            DuplicateVersionChecker.EnsureNoDuplicateVersions(migrations);

            return migrations;
        }
    }
}
