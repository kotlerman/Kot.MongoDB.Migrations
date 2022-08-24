using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    /// <summary>
    /// Migrations locator that loads migrations from a namespace.
    /// </summary>
    /// <remarks>
    /// This locator looks for migrations in all assemblies of the current domain, loading only those that
    /// are located within a specified namespace.
    /// </remarks>
    public class NamespaceMigrationsLocator : IMigrationsLocator
    {
        private static readonly Type MigrationType = typeof(IMongoMigration);

        private readonly IMigrationInstantiator _instantiator;
        private readonly string _namespace;

        /// <summary>
        /// Initializes a new instance of <see cref="NamespaceMigrationsLocator"/> that loads migration types from the specified
        /// <paramref name="namespace"/> and instantiates them using the <paramref name="instantiator"/>.
        /// </summary>
        /// <param name="instantiator">Migrations instantiator used to create instances of migration types.</param>
        /// <param name="namespace">Namespace to load migration types from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="instantiator"/> is <see langword="null"/>, or
        /// <paramref name="namespace"/> is <see langword="null"/> or empty.</exception>
        public NamespaceMigrationsLocator(IMigrationInstantiator instantiator, string @namespace)
        {
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
            _namespace = string.IsNullOrEmpty(@namespace) ? throw new ArgumentNullException(nameof(@namespace)) : @namespace;
        }

        /// <inheritdoc/>
        /// <exception cref="Exceptions.DuplicateMigrationVersionException">Migrations with the same version were found.</exception>
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
