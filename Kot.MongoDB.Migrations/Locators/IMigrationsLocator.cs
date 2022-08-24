using System.Collections.Generic;

namespace Kot.MongoDB.Migrations.Locators
{
    /// <summary>
    /// Defines Mongo migrations loading method.
    /// </summary>
    public interface IMigrationsLocator
    {
        /// <summary>
        /// Load migrations.
        /// </summary>
        /// <returns>An enumerable collection of migrations.</returns>
        IEnumerable<IMongoMigration> Locate();
    }
}