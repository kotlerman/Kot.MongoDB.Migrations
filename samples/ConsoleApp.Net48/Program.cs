using Kot.MongoDB.Migrations;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ConsoleApp.Net48
{
    internal class Program
    {
        const string ConnectionString = "mongodb://localhost:27017";
        const string DbName = "sample_db";

        public static async Task Main(string[] args)
        {
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

            var options = new MigrationOptions(DbName);
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("log.txt")
                .CreateLogger();

            IMigrator migrator = MigratorBuilder.FromConnectionString(ConnectionString, options)
                .LoadMigrationsFromCurrentDomain()
                .WithLogger(new SerilogLoggerFactory(logger))
                .Build();

            MigrationResult result = await migrator.MigrateAsync(targetVersion);

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
        }
    }
}
