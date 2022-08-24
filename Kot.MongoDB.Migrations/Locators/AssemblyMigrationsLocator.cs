using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    /// <summary>
    /// Migrations locator that loads migrations from an assembly.
    /// </summary>
    public class AssemblyMigrationsLocator : IMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;
        private readonly Assembly _assembly;

        /// <summary>
        /// Initializes a new instance of <see cref="AssemblyMigrationsLocator"/> that loads migration types from the specified
        /// <paramref name="assembly"/> and instantiates them using the <paramref name="instantiator"/>.
        /// </summary>
        /// <param name="instantiator">Migrations instantiator used to create instances of migration types.</param>
        /// <param name="assembly">Assembly to load migration types from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="instantiator"/> or <paramref name="assembly"/>
        /// is <see langword="null"/>.</exception>
        public AssemblyMigrationsLocator(IMigrationInstantiator instantiator, Assembly assembly)
        {
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        /// <inheritdoc/>
        /// <exception cref="Exceptions.DuplicateMigrationVersionException">Migrations with the same version were found.</exception>
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
