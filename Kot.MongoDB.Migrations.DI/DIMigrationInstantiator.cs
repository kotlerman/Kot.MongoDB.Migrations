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
            => (IMongoMigration)ActivatorUtilities.CreateInstance(_serviceProvider, migrationType);
    }
}
