using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations.Locators
{
    /// <summary>
    /// Migrations locator that loads specified migrations.
    /// </summary>
    public class CollectionMigrationsLocator : IMigrationsLocator
    {
        private readonly IEnumerable<IMongoMigration> _migrations;

        /// <summary>
        /// Initializes a new instance of <see cref="CollectionMigrationsLocator"/> that loads specified <paramref name="migrations"/>.
        /// </summary>
        /// <param name="migrations">Migrations to be loaded.</param>
        /// <exception cref="Exceptions.DuplicateMigrationVersionException"><paramref name="migrations"/> collection contains
        /// migrations with the same version.</exception>
        public CollectionMigrationsLocator(IEnumerable<IMongoMigration> migrations)
        {
            _migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
            DuplicateVersionChecker.EnsureNoDuplicateVersions(migrations);
        }

        /// <inheritdoc/>
        public IEnumerable<IMongoMigration> Locate() => _migrations;
    }
}
