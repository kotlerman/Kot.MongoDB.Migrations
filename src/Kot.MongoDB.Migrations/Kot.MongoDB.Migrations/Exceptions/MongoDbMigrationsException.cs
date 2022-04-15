using System;

namespace Kot.MongoDB.Migrations.Exceptions
{
    public class MongoDbMigrationsException : Exception
    {
        public MongoDbMigrationsException(string message) : base(message)
        {
        }
    }
}
