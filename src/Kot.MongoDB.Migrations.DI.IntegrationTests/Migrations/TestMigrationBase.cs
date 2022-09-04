using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.DI.IntegrationTests.Migrations
{
    public abstract class TestMigrationBase : MongoMigration
    {
        private readonly ITestService _testService;

        public TestMigrationBase(string version, ITestService testService) : base(version, version)
        {
            _testService = testService;
        }

        public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var doc = new TestDoc
            {
                ValueA = Name,
                ValueB = _testService.TestValue
            };
            var collection = db.GetCollection<TestDoc>(TestDoc.CollectionName);
            await collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }

        public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
