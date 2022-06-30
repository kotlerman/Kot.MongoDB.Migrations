using Kot.MongoDB.Migrations.Locators;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Kot.MongoDB.Migrations.DI
{
    public class DIMigrationsLocationConfigurator
    {
        private readonly IServiceCollection _serviceCollection;

        internal bool IsLocatorSelected { get; private set; }

        internal DIMigrationsLocationConfigurator(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public void LoadMigrationsFromCurrentDomain()
        {
            EnsureLocatorNotYetSelected();
            RegisterInstantiator();
            _serviceCollection.AddSingleton<IMigrationsLocator, CurrentDomainMigrationsLocator>();
            IsLocatorSelected = true;
        }

        public void LoadMigrationsFromAssembly(Assembly assembly)
        {
            EnsureLocatorNotYetSelected();
            RegisterInstantiator();
            _serviceCollection.AddSingleton<IMigrationsLocator, AssemblyMigrationsLocator>(
                provider => new AssemblyMigrationsLocator(provider.GetRequiredService<IMigrationInstantiator>(), assembly));
            IsLocatorSelected = true;
        }

        public void LoadMigrationsFromExecutingAssembly() => LoadMigrationsFromAssembly(Assembly.GetExecutingAssembly());

        public void LoadMigrationsFromNamespace(string @namespace)
        {
            EnsureLocatorNotYetSelected();
            RegisterInstantiator();
            _serviceCollection.AddSingleton<IMigrationsLocator, NamespaceMigrationsLocator>(
                provider => new NamespaceMigrationsLocator(provider.GetRequiredService<IMigrationInstantiator>(), @namespace));
            IsLocatorSelected = true;
        }

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
