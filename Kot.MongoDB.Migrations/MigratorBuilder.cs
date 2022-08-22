using Kot.MongoDB.Migrations.Locators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// A builder that constructs an instance of <see cref="IMigrator"/>.
    /// </summary>
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

        /// <summary>
        /// Instantiate a new instance of <see cref="MigratorBuilder"/> using a Mongo client.
        /// </summary>
        /// <param name="mongoClient">Mongo client connected to the database that migrations should be applied to.</param>
        /// <param name="options">Migration options that customize migration behavior.</param>
        /// <returns><see cref="MigratorBuilder"/> that uses the specified <see cref="IMongoClient"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="mongoClient"/> or <paramref name="options"/>
        /// is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Instantiate a new instance of <see cref="MigratorBuilder"/> using a Connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to the database that migrations should be applied to.</param>
        /// <param name="options">Migration options that customize migration behavior.</param>
        /// <returns><see cref="MigratorBuilder"/> that uses the specified Connection string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is <see langword="null"/> or empty,
        /// or <paramref name="options"/> is <see langword="null"/>.</exception>
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

        /// <summary>
        /// Load migrations from the current domain.
        /// </summary>
        /// <returns><see cref="MigratorBuilder"/> that loads migrations from the current domain.</returns>
        public MigratorBuilder LoadMigrationsFromCurrentDomain()
        {
            _locator = new CurrentDomainMigrationsLocator(_instantiator);
            return this;
        }

        /// <summary>
        /// Load migrations from the specified assembly.
        /// </summary>
        /// <param name="assembly">Assembly to load migrations from.</param>
        /// <returns><see cref="MigratorBuilder"/> that loads migrations from the specified assembly.</returns>
        public MigratorBuilder LoadMigrationsFromAssembly(Assembly assembly)
        {
            _locator = new AssemblyMigrationsLocator(_instantiator, assembly);
            return this;
        }

        /// <summary>
        /// Load migrations from the executing assembly.
        /// </summary>
        /// <returns><see cref="MigratorBuilder"/> that loads migrations from the executing assembly.</returns>
        public MigratorBuilder LoadMigrationsFromExecutingAssembly() => LoadMigrationsFromAssembly(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Load migrations from the specified namespace.
        /// </summary>
        /// <param name="namespace">Namespace to load migrations from.</param>
        /// <returns><see cref="MigratorBuilder"/> that loads migrations from the specified namespace.</returns>
        public MigratorBuilder LoadMigrationsFromNamespace(string @namespace)
        {
            _locator = new NamespaceMigrationsLocator(_instantiator, @namespace);
            return this;
        }

        /// <summary>
        /// Load specified collection of migrations.
        /// </summary>
        /// <param name="migrations">Migrations to load.</param>
        /// <returns><see cref="MigratorBuilder"/> that loads specified migrations.</returns>
        public MigratorBuilder LoadMigrations(IEnumerable<IMongoMigration> migrations)
        {
            _locator = new CollectionMigrationsLocator(migrations);
            return this;
        }

        /// <summary>
        /// Build migrator.
        /// </summary>
        /// <returns>An instance of <see cref="IMigrator"/>.</returns>
        /// <exception cref="InvalidOperationException">Migrations location has not been specified.</exception>
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
