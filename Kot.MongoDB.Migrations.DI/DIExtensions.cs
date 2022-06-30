using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;

namespace Kot.MongoDB.Migrations.DI
{
    public static class DIExtensions
    {
        public static IServiceCollection AddMongoMigrations(this IServiceCollection services, MigrationOptions options,
            Action<DIMigrationsLocationConfigurator> configure = null)
        {
            ConfigureMigrationsLocation(services, configure);
            services.AddSingleton(options);
            services.AddSingleton<IMigrator, Migrator>();
            return services;
        }

        public static IServiceCollection AddMongoMigrations(this IServiceCollection services, IMongoClient mongoClient,
            MigrationOptions options, Action<DIMigrationsLocationConfigurator> configure = null)
        {
            ConfigureMigrationsLocation(services, configure);
            services.AddSingleton(options);
            services.AddSingleton<IMigrator>(provider => new Migrator(
                provider.GetRequiredService<IMigrationsLocator>(), mongoClient, provider.GetRequiredService<MigrationOptions>()));
            return services;
        }

        public static IServiceCollection AddMongoMigrations(this IServiceCollection serviceCollection, string connectionString,
            MigrationOptions options, Action<DIMigrationsLocationConfigurator> configure = null)
        {
            return AddMongoMigrations(serviceCollection, new MongoClient(connectionString), options, configure);
        }

        private static void ConfigureMigrationsLocation(IServiceCollection services, Action<DIMigrationsLocationConfigurator> configure)
        {
            var locationsConfigurator = new DIMigrationsLocationConfigurator(services);

            configure?.Invoke(locationsConfigurator);

            if (!locationsConfigurator.IsLocatorSelected)
            {
                locationsConfigurator.LoadMigrationsFromCurrentDomain();
            }
        }
    }
}
