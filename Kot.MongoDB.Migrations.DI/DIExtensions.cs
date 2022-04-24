using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Kot.MongoDB.Migrations.DI
{
    public static class DIExtensions
    {
        public static IServiceCollection AddMongoMigrations(this IServiceCollection serviceCollection, MigrationOptions options)
        {
            serviceCollection.AddSingleton(options);
            serviceCollection.AddSingleton<IMigrationsLocator, DIMigrationsLocator>();
            serviceCollection.AddSingleton<IMigrator, Migrator>();
            return serviceCollection;
        }

        public static IServiceCollection AddMongoMigrations(this IServiceCollection serviceCollection, string connectionString,
            MigrationOptions options)
        {
            var mongoClient = new MongoClient(connectionString);
            serviceCollection.AddSingleton(options);
            serviceCollection.AddSingleton<IMigrationsLocator, DIMigrationsLocator>();
            serviceCollection.AddSingleton<IMigrator>(provider => new Migrator(
                provider.GetRequiredService<IMigrationsLocator>(), mongoClient, provider.GetRequiredService<MigrationOptions>()));
            return serviceCollection;
        }
    }
}
