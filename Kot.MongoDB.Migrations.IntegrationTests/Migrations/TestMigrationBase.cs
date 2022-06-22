using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kot.MongoDB.Migrations.IntegrationTests.Migrations
{
    internal abstract class TestMigrationBase : MongoMigration
    {
        public TestMigrationBase(string version) : base(version, version)
        {
        }

        public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            var doc = new TestDoc { Value = Name };
            var collection = db.GetCollection<TestDoc>(TestDoc.CollectionName);
            await collection.InsertOneAsync(doc, cancellationToken: cancellationToken);
        }

        public override Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
