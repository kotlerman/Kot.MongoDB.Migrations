namespace Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations
{
    internal class SimpleMigration001 : TestMigrationBase
    {
        public SimpleMigration001(ITestService testService) : base("0.0.1", testService)
        {
        }
    }
}
