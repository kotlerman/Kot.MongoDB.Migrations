[![Build](https://github.com/kotlerman/Kot.MongoDB.Migrations/actions/workflows/build.yml/badge.svg)](https://github.com/kotlerman/Kot.MongoDB.Migrations/actions/workflows/build.yml)
[![License](https://img.shields.io/badge/License-MIT-blue)](https://github.com/kotlerman/Kot.MongoDB.Migrations/blob/main/LICENSE)

# Kot.MongoDB.Migrations
This package enables you to create and run MongoDb migrations. Simple and clean, requires only [MongoDB.Driver](https://github.com/mongodb/mongo-csharp-driver). Supports wrapping migrations in a transaction. To use with DI containers, see [Kot.MongoDB.Migrations.DI](#kotmongodbmigrationsdi) below.

## Installation
Run the following command in the NuGet Package Manager Console:
```powershell
PM> Install-Package Kot.MongoDB.Migrations
```

## Usage
### Creating migration classes
Create classes that describe your migrations. Each class must implement *IMongoMigration*. You can also derive from the *MongoMigration* class that provides a simple implementation.
```csharp
class MyMigration : MongoMigration
{
    public MyMigration() : this("1.0.0") { }

    public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken ct)
    {
        ...
    }

    public override async Task DownAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken ct)
    {
        ...
    }
}
```

### Configuring migrator
Create an instance of *IMigrator* using *MigratorBuilder* (you can also create it manually, if you need). Provide a connection string or an instance of *IMongoClient* and *MigrationOptions*, then choose where to load migrations from.
```csharp
var options = new MigrationOptions("myDb");
var migrator = MigratorBuilder.FromConnectionString("mongodb://localhost:27017", options)
    .LoadMigrationsFromCurrentDomain()
    .Build();
```
Currently migrations can be loaded with the following methods:
- **LoadMigrationsFromCurrentDomain()** - load from current domain
- **LoadMigrationsFromExecutingAssembly()** - load from executing assembly
- **LoadMigrationsFromAssembly(Assembly assembly)** - load from a specified *assembly*
- **LoadMigrationsFromNamespace(string @namespace)** - load from a specified *@namespace* (scans all assemblies of current domain)
- **LoadMigrations(IEnumerable<IMongoMigration> migrations)** - load from a specified *migrations* collection


### Running migrations
To apply migrations, add the following code. This will check which migrations were applied before and determine which migrations should be applied now. You can also pass a target version as an argument in case you want to migrate (up/down) to a specific version.
```csharp
await migrator.MigrateAsync();
```

### Transactions
With the help of transactions you can ensure that a database stays in a consistent state when migration process fails. Make sure you know what can and what cannot be done with a database when using transactions (see official [docs](https://www.mongodb.com/docs/upcoming/core/transactions/)).
To specify how to wrap migrations in transactions, set *TransactionScope* property of *MigrationOptions*. There are 3 options:
- **None** - transactions are not used
- **SingleMigration** - each migration is wrapped in a separate transaction
- **AllMigrations** - all migrations a wrapped in a single transaction

Example:
```csharp
var options = new MigrationOptions("myDb")
{
    TransactionScope = TransactionScope.SingleMigration
};
```

When one of the last two options is used, an instance of *IClientSessionHandle* is passed to the *UpAsync* and *UpAsync* methods of migrations. You should pass it to all DB operations like this:
```csharp
public override async Task UpAsync(IMongoDatabase db, IClientSessionHandle session, CancellationToken cancellationToken)
{
    IMongoCollection<TestDoc> collection = db.GetCollection<TestDoc>("my_docs_collection");
    var doc = new TestDoc { SomeData = "Some data" };
    await collection.InsertOneAsync(session, doc, null, cancellationToken);
}
```

You can further configure transactions behavior by setting *ClientSessionOptions* property of *MigrationOptions*:
```csharp
var options = new MigrationOptions("myDb")
{
    TransactionScope = TransactionScope.SingleMigration,
    ClientSessionOptions = new ClientSessionOptions
    {
        DefaultTransactionOptions = new TransactionOptions(maxCommitTime: TimeSpan.FromMinutes(1))
    }
};
```

# Kot.MongoDB.Migrations.DI
This package enables you to use [Kot.MongoDB.Migrations](#kotmongodbmigrations) package with DI container. 

## Installation
Run the following command in the NuGet Package Manager Console:
```powershell
PM> Install-Package Kot.MongoDB.Migrations.DI
```

## Usage
Use extension method to register migration services. By default, migrations are loaded from current domain. You can configure this with different overloads of the extension method. It is also possible to pass an instance of *IMongoClient* instead of connection string or to not pass anything (in this case an instance of *IMongoClient* will be resolved from container).
```csharp
var options = new MigrationOptions("myDb");
services.AddMongoMigrations("mongodb://localhost:27017", options, config => config.LoadMigrationsFromCurrentDomain());
```
Then resolve an instance of *IMigrator* and use as described [above](#running-migrations).
