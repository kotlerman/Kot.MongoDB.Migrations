using Kot.MongoDB.Migrations;
using MongoDB.Driver;
using WebApiApp.Net6.Documents;

namespace WebApiApp.Net6.Migrations;

internal class Migration002_UserLoginIndex : MongoMigration
{
    public Migration002_UserLoginIndex() : base("0.0.2")
    {
    }

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        IMongoCollection<User> usersCol = db.GetCollection<User>("users");
        IndexKeysDefinition<User> index = Builders<User>.IndexKeys.Ascending(x => x.Login);
        var options = new CreateIndexOptions { Name = "login_asc" };
        await usersCol.Indexes.CreateOneAsync(new CreateIndexModel<User>(index, options), cancellationToken: cancellationToken);
    }

    public override async Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
    {
        IMongoCollection<User> usersCol = db.GetCollection<User>("users");
        await usersCol.Indexes.DropOneAsync("login_asc", cancellationToken);
    }
}
