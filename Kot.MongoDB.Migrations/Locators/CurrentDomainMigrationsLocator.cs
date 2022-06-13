using Kot.MongoDB.Migrations.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kot.MongoDB.Migrations.Locators
{
    public class CurrentDomainMigrationsLocator : IMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;

        public CurrentDomainMigrationsLocator(IMigrationInstantiator instantiator)
        {
            _instantiator = instantiator;
        }

        public IEnumerable<IMongoMigration> Locate()
        {
            IEnumerable<IMongoMigration> migrations = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => MigrationType.IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => _instantiator.Instantiate(type))
                .OrderBy(migration => migration.Version)
                .ToList();

            DuplicateVersionChecker.EnsureNoDuplicateVersions(migrations);

            return migrations;
        }
    }
}
