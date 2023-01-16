using ConsoleApp.Net6;
using Kot.MongoDB.Migrations;
using Kot.MongoDB.Migrations.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

const string ConnectionString = "mongodb://localhost:27017";
const string DbName = "sample_db";

if (args.Length > 1)
{
    Console.WriteLine("Expected 1 or 0 arguments, got {0}.", args.Length);
    return;
}

DatabaseVersion? targetVersion = null;

if (args.Length == 1)
{
    try
    {
        targetVersion = (DatabaseVersion)args[0];
    }
    catch
    {
        Console.WriteLine("Unexpected version format.");
        return;
    }
}

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddMongoMigrations(ConnectionString, new MigrationOptions(DbName), x => x.LoadMigrationsFromCurrentDomain());
        services.AddSingleton(new HostedServiceParams { TargetVersion = targetVersion });
        services.AddHostedService<HostedService>();
    })
    .UseSerilog((_, logging) =>
    {
        logging.MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File("log.txt");
    })
    .Build();

await host.RunAsync();