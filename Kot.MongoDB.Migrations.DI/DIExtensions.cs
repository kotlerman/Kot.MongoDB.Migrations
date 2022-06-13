using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Kot.MongoDB.Migrations.DI
{
    public static class DIExtensions
    {
        public static IServiceCollection AddMongoMigrations(this IServiceCollection serviceCollection, MigrationOptions options)
        {
            serviceCollection.AddSingleton(options);
            serviceCollection.AddSingleton<IMigrationInstantiator, DIMigrationInstantiator>();
            serviceCollection.AddSingleton<IMigrationsLocator, CurrentDomainMigrationsLocator>();
            serviceCollection.AddSingleton<IMigrator, Migrator>();
            return serviceCollection;
        }

        public static IServiceCollection AddMongoMigrations(this IServiceCollection serviceCollection, IMongoClient mongoClient,
            MigrationOptions options)
        {
            serviceCollection.AddSingleton(options);
            serviceCollection.AddSingleton<IMigrationInstantiator, DIMigrationInstantiator>();
            serviceCollection.AddSingleton<IMigrationsLocator, CurrentDomainMigrationsLocator>();
            serviceCollection.AddSingleton<IMigrator>(provider => new Migrator(
                provider.GetRequiredService<IMigrationsLocator>(), mongoClient, provider.GetRequiredService<MigrationOptions>()));
            return serviceCollection;
        }

        public static IServiceCollection AddMongoMigrations(this IServiceCollection serviceCollection, string connectionString,
            MigrationOptions options)
        {
            return AddMongoMigrations(serviceCollection, new MongoClient(connectionString), options);
        }
    }
}
