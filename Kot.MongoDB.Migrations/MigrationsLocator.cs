using Kot.MongoDB.Migrations.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kot.MongoDB.Migrations
{
    public class MigrationsLocator : IMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;

        public MigrationsLocator(IMigrationInstantiator instantiator)
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

            EnsureNoDuplicateVersions(migrations);

            return migrations;
        }

        private static void EnsureNoDuplicateVersions(IEnumerable<IMongoMigration> migrations)
        {
            DatabaseVersion? prevVersion = null;

            foreach (IMongoMigration migration in migrations)
            {
                if (migration.Version == prevVersion)
                {
                    throw new DuplicateMigrationVersionException(prevVersion.Value);
                }

                prevVersion = migration.Version;
            }
        }
    }
}
