using Kot.MongoDB.Migrations.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Migration instantiator that creates instances using <see cref="Activator"/>.
    /// </summary>
    public class ActivatorMigrationInstantiator : IMigrationInstantiator
    {
        /// <inheritdoc/>
        /// <exception cref="MigrationInstantiationException">Failed to create an instance of the specified migration type.</exception>
        public IMongoMigration Instantiate(Type migrationType)
        {
            try
            {
                return (IMongoMigration)Activator.CreateInstance(migrationType);
            }
            catch (Exception ex)
            {
                throw new MigrationInstantiationException(migrationType, ex);
            }
        }
    }
}
