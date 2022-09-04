using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Kot.MongoDB.Migrations.DI
{
    /// <summary>
    /// A configurator that configures where migrations should be loaded from.
    /// </summary>
    public class DIMigrationsLocationConfigurator
    {
        private readonly IServiceCollection _serviceCollection;

        internal bool IsLocatorSelected { get; private set; }

        internal DIMigrationsLocationConfigurator(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        /// <summary>
        /// Load migrations from the current domain.
        /// </summary>
        public void LoadMigrationsFromCurrentDomain()
        {
            EnsureLocatorNotYetSelected();
            RegisterInstantiator();
            _serviceCollection.AddSingleton<IMigrationsLocator, CurrentDomainMigrationsLocator>();
            IsLocatorSelected = true;
        }

        /// <summary>
        /// Load migrations from the specified assembly.
        /// </summary>
        /// <param name="assembly">Assembly to load migrations from.</param>
        public void LoadMigrationsFromAssembly(Assembly assembly)
        {
            EnsureLocatorNotYetSelected();
            RegisterInstantiator();
            _serviceCollection.AddSingleton<IMigrationsLocator, AssemblyMigrationsLocator>(
                provider => new AssemblyMigrationsLocator(provider.GetRequiredService<IMigrationInstantiator>(), assembly));
            IsLocatorSelected = true;
        }

        /// <summary>
        /// Load migrations from the executing assembly.
        /// </summary>
        public void LoadMigrationsFromExecutingAssembly() => LoadMigrationsFromAssembly(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Load migrations from the specified namespace.
        /// </summary>
        /// <param name="namespace">Namespace to load migrations from.</param>
        public void LoadMigrationsFromNamespace(string @namespace)
        {
            EnsureLocatorNotYetSelected();
            RegisterInstantiator();
            _serviceCollection.AddSingleton<IMigrationsLocator, NamespaceMigrationsLocator>(
                provider => new NamespaceMigrationsLocator(provider.GetRequiredService<IMigrationInstantiator>(), @namespace));
            IsLocatorSelected = true;
        }

        /// <summary>
        /// Load specified collection of migrations.
        /// </summary>
        /// <param name="migrations">Migrations to load.</param>
        public void LoadMigrations(IEnumerable<IMongoMigration> migrations)
        {
            EnsureLocatorNotYetSelected();
            _serviceCollection.AddSingleton<IMigrationsLocator, CollectionMigrationsLocator>(
                _ => new CollectionMigrationsLocator(migrations));
            IsLocatorSelected = true;
        }

        private void EnsureLocatorNotYetSelected()
        {
            if (IsLocatorSelected)
            {
                throw new InvalidOperationException("Migrations location has already been selected.");
            }
        }

        private void RegisterInstantiator()
        {
            _serviceCollection.AddSingleton<IMigrationInstantiator, DIMigrationInstantiator>();
        }
    }
}
