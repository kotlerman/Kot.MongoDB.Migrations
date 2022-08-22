using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Defines Mongo migrations instantiation method.
    /// </summary>
    public interface IMigrationInstantiator
    {
        /// <summary>
        /// Create an instance of a specified migration type.
        /// </summary>
        /// <param name="migrationType">Migration type.</param>
        /// <returns>An instance of the specified migration type.</returns>
        IMongoMigration Instantiate(Type migrationType);
    }
}
