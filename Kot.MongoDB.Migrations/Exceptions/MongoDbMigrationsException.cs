using System;

namespace Kot.MongoDB.Migrations.Exceptions
{
    public class MongoDbMigrationsException : Exception
    {
        public MongoDbMigrationsException(string message) : base(message)
        {
        }

        public MongoDbMigrationsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
