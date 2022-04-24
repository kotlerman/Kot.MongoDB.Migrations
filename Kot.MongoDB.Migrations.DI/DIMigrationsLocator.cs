using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kot.MongoDB.Migrations.DI
{
    public class DIMigrationsLocator : MigrationsLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public DIMigrationsLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override IMongoMigration Instantiate(Type migrationType)
            => (IMongoMigration)ActivatorUtilities.CreateInstance(_serviceProvider, migrationType);
    }
}
