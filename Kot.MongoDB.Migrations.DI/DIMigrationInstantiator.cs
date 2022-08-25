using Kot.MongoDB.Migrations.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kot.MongoDB.Migrations.DI
{
    /// <summary>
    /// Migration instantiator that creates instances using <see cref="IServiceProvider"/>.
    /// </summary>
    public class DIMigrationInstantiator : IMigrationInstantiator
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DIMigrationInstantiator"/> that creates instances
        /// using <paramref name="serviceProvider"/>.
        /// </summary>
        /// <param name="serviceProvider">A service provider used to resolve dependencies of migration types.</param>
        public DIMigrationInstantiator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        /// <exception cref="MigrationInstantiationException">Failed to create an instance of the specified migration type.</exception>
        public IMongoMigration Instantiate(Type migrationType)
        {
            try
            {
                return (IMongoMigration)ActivatorUtilities.CreateInstance(_serviceProvider, migrationType);
            }
            catch (Exception ex)
            {
                throw new MigrationInstantiationException(migrationType, ex);
            }
        }
    }
}
