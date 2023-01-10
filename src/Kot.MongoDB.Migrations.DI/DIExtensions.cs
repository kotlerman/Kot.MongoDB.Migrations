using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;

namespace Kot.MongoDB.Migrations.DI
{
    /// <summary>
    /// Extension methods for setting up migration services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class DIExtensions
    {
        /// <summary>
        /// Registers services required to perform Mongo database migrations and configures migrations location. 
        /// Requires a registered Mongo client connected to the database that migrations should be applied to.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="options">Options that customize how migrations are applied.</param>
        /// <param name="configure">A delegate to configure migrations location.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMongoMigrations(this IServiceCollection services, MigrationOptions options,
            Action<DIMigrationsLocationConfigurator> configure = null)
        {
            ConfigureMigrationsLocation(services, configure);
            services.AddSingleton(options);
            services.AddSingleton<IMigrator, Migrator>();
            return services;
        }

        /// <summary>
        /// Registers services required to perform Mongo database migrations and configures migrations location.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="mongoClient">Mongo client connected to the database that migrations should be applied to.</param>
        /// <param name="options">Options that customize how migrations are applied.</param>
        /// <param name="configure">A delegate to configure migrations location.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMongoMigrations(this IServiceCollection services, IMongoClient mongoClient,
            MigrationOptions options, Action<DIMigrationsLocationConfigurator> configure = null)
        {
            ConfigureMigrationsLocation(services, configure);
            services.AddSingleton(options);
            services.AddSingleton<IMigrator>(provider => new Migrator(
                provider.GetRequiredService<IMigrationsLocator>(),
                mongoClient,
                provider.GetRequiredService<MigrationOptions>(),
                provider.GetService<ILogger<Migrator>>()));
            return services;
        }

        /// <summary>
        /// Registers services required to perform Mongo database migrations and configures migrations location.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="connectionString">Connection string to the database that migrations should be applied to.</param>
        /// <param name="options">Options that customize how migrations are applied.</param>
        /// <param name="configure">A delegate to configure migrations location.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMongoMigrations(this IServiceCollection services, string connectionString,
            MigrationOptions options, Action<DIMigrationsLocationConfigurator> configure = null)
        {
            return AddMongoMigrations(services, new MongoClient(connectionString), options, configure);
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
