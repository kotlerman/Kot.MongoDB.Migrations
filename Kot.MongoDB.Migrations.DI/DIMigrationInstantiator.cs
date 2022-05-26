using Kot.MongoDB.Migrations.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kot.MongoDB.Migrations.DI
{
    public class DIMigrationInstantiator : IMigrationInstantiator
    {
        private readonly IServiceProvider _serviceProvider;

        public DIMigrationInstantiator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

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
