using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace Kot.MongoDB.Migrations.DI.Tests
{
    [TestFixture]
    public class DIMigrationsLocationConfiguratorTests
    {
        [Test]
        public void MultipleLoadSources_ThrowsException(
            [ValueSource(nameof(ConfiguratorActions))] Action<DIMigrationsLocationConfigurator> first,
            [ValueSource(nameof(ConfiguratorActions))] Action<DIMigrationsLocationConfigurator> second)
        {
            // Arrange
            var configurator = new DIMigrationsLocationConfigurator(new ServiceCollection());

            // Act & Assert
            first(configurator);
            Assert.Throws<InvalidOperationException>(() => second(configurator));
        }

        private static Action<DIMigrationsLocationConfigurator>[] ConfiguratorActions() => new Action<DIMigrationsLocationConfigurator>[]
        {
            (DIMigrationsLocationConfigurator config) => config.LoadMigrationsFromCurrentDomain(),
            (DIMigrationsLocationConfigurator config) => config.LoadMigrationsFromAssembly(Assembly.GetExecutingAssembly()),
            (DIMigrationsLocationConfigurator config) => config.LoadMigrationsFromExecutingAssembly(),
            (DIMigrationsLocationConfigurator config) => config.LoadMigrationsFromNamespace("Namespace"),
            (DIMigrationsLocationConfigurator config) => config.LoadMigrations(Enumerable.Empty<IMongoMigration>())
        };
    }
}
