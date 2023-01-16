using Kot.MongoDB.Migrations;

namespace ConsoleApp.Net6;

internal class HostedServiceParams
{
    public DatabaseVersion? TargetVersion { get; set; }
}
