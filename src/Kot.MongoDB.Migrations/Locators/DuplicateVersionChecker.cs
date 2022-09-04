using Kot.MongoDB.Migrations.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    internal static class DuplicateVersionChecker
    {
        public static void EnsureNoDuplicateVersions(IEnumerable<IMongoMigration> migrations)
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
