using System;
using System.Collections.Generic;
using System.Linq;

namespace Kot.MongoDB.Migrations.Locators
{
    /// <summary>
    /// Migrations locator that loads migrations from the current domain.
    /// </summary>
    public class CurrentDomainMigrationsLocator : IMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;

        /// <summary>
        /// Initializes a new instance of <see cref="CurrentDomainMigrationsLocator"/> that loads migration types from
        /// the current domain and instantiates them using the <paramref name="instantiator"/>.
        /// </summary>
        /// <param name="instantiator">Migrations instantiator used to create instances of migration types.</param>
        /// <exception cref="ArgumentNullException"><paramref name="instantiator"/> is <see langword="null"/>.</exception>
        public CurrentDomainMigrationsLocator(IMigrationInstantiator instantiator)
        {
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
        }

        /// <inheritdoc/>
        /// <exception cref="Exceptions.DuplicateMigrationVersionException">Migrations with the same version were found.</exception>
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
