using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    public class AssemblyMigrationsLocator : IMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;
        private readonly Assembly _assembly;

        public AssemblyMigrationsLocator(IMigrationInstantiator instantiator, Assembly assembly)
        {
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public IEnumerable<IMongoMigration> Locate()
        {
            IEnumerable<IMongoMigration> migrations = _assembly.GetTypes()
                .Where(type => MigrationType.IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => _instantiator.Instantiate(type))
                .OrderBy(migration => migration.Version)
                .ToList();

            DuplicateVersionChecker.EnsureNoDuplicateVersions(migrations);

            return migrations;
        }
    }
}
