using Kot.MongoDB.Migrations.Locators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    public class MigratorBuilder
    {
        private readonly IMongoClient _mongoClient;
        private readonly MigrationOptions _options;
        private readonly IMigrationInstantiator _instantiator = new ActivatorMigrationInstantiator();

        private IMigrationsLocator _locator;

        private MigratorBuilder(IMongoClient mongoClient, MigrationOptions options)
        {
            _mongoClient = mongoClient;
            _options = options;
        }

        public static MigratorBuilder FromMongoClient(IMongoClient mongoClient, MigrationOptions options)
        {
            if (mongoClient == null)
            {
                throw new ArgumentNullException(nameof(mongoClient));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new MigratorBuilder(mongoClient, options);
        }

        public static MigratorBuilder FromConnectionString(string connectionString, MigrationOptions options)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new MigratorBuilder(new MongoClient(connectionString), options);
        }

        public MigratorBuilder LoadMigrationsFromCurrentDomain()
        {
            _locator = new CurrentDomainMigrationsLocator(_instantiator);
            return this;
        }

        public MigratorBuilder LoadMigrationsFromAssembly(Assembly assembly)
        {
            _locator = new AssemblyMigrationsLocator(_instantiator, assembly);
            return this;
        }

        public MigratorBuilder LoadMigrationsFromExecutingAssembly() => LoadMigrationsFromAssembly(Assembly.GetExecutingAssembly());

        public MigratorBuilder LoadMigrationsFromNamespace(string @namespace)
        {
            _locator = new NamespaceMigrationsLocator(_instantiator, @namespace);
            return this;
        }

        public MigratorBuilder LoadMigrations(IEnumerable<IMongoMigration> migrations)
        {
            _locator = new CollectionMigrationsLocator(migrations);
            return this;
        }

        public IMigrator Build()
        {
            if (_locator == null)
            {
                throw new InvalidOperationException("Migrations location was not specified.");
            }

            return new Migrator(_locator, _mongoClient, _options);
        }
    }
}
