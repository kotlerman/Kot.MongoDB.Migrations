using ConsoleApp.Net6;
using Kot.MongoDB.Migrations;
using Microsoft.Extensions.Hosting;

class HostedService : IHostedService
{
    private readonly IMigrator _migrator;
    private readonly IHost _host;
    private readonly HostedServiceParams _parameters;

    public HostedService(IMigrator migrator, IHost host, HostedServiceParams parameters)
    {
        _migrator = migrator;
        _host = host;
        _parameters = parameters;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        MigrationResult result = await _migrator.MigrateAsync(_parameters.TargetVersion, cancellationToken);

        switch (result.Type)
        {
            case MigrationResultType.UpToDate:
                Console.WriteLine("DB is already up to date.");
                break;
            case MigrationResultType.Upgraded:
                Console.WriteLine("DB upgraded from version '{0}' to '{1}', {2} migrations applied.",
                    result.InitialVersion, result.FinalVersion, result.AppliedMigrations.Count);
                break;
            case MigrationResultType.Downgraded:
                Console.WriteLine("DB downgraded from version '{0}' to '{1}', {2} migrations unapplied.",
                    result.InitialVersion, result.FinalVersion, result.AppliedMigrations.Count);
                break;
            case MigrationResultType.Cancelled:
                Console.WriteLine("Migration was cancelled, because concurrent migration process was detected.");
                break;
        }

        await _host.StopAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}