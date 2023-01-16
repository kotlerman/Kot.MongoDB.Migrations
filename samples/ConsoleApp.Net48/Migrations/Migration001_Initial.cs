using ConsoleApp.Net48.Documents;
using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp.Net48.Migrations
{
    internal class Migration001_Initial : MongoMigration
    {
        public Migration001_Initial() : base("0.0.1")
        {
        }

        public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            IMongoCollection<User> usersCol = db.GetCollection<User>("users");
            await usersCol.InsertOneAsync(new User { Login = "admin" }, cancellationToken: cancellationToken);
        }

        public override async Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
        {
            await db.DropCollectionAsync("users", cancellationToken);
        }
    }
}
