namespace Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations
{
    internal class SimpleMigration002 : TestMigrationBase
    {
        public SimpleMigration002(ITestService testService) : base("0.0.2", testService)
        {
        }
    }
}
