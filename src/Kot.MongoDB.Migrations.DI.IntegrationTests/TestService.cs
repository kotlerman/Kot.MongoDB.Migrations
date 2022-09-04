namespace Kot.MongoDB.Migrations.DI.IntegrationTests
{
    public interface ITestService
    {
        string TestValue { get; set; }
    }

    public class TestService : ITestService
    {
        public string TestValue { get; set; }
    }
}
