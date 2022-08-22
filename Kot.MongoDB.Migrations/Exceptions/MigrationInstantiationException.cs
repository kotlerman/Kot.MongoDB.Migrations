using System;
using System.Collections.Generic;
using System.Text;

namespace Kot.MongoDB.Migrations.Exceptions
{
    /// <summary>
    /// Represents a migration type instantiation error.
    /// </summary>
    public class MigrationInstantiationException : MongoDbMigrationsException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationInstantiationException"/> class with a specified migration type
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="migrationType">Migration type that failed to be instantiated.</param>
        /// <param name="innerException">The exception that is the cause of the current exception or a null reference
        /// if no inner exception is specified.</param>
        public MigrationInstantiationException(Type migrationType, Exception innerException)
            : base($"Failed to instantiate migration class '{migrationType.FullName}'.", innerException)
        {
        }
    }
}
