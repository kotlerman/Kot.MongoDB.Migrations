namespace Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations.Subfolder
{
    internal class SimpleMigration003 : TestMigrationBase
    {
        public SimpleMigration003(ITestService testService) : base("0.0.3", testService)
        {
        }
    }
}
