using MongoDB.Bson;
using System;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Represents a migration lock document.
    /// </summary>
    public class MigrationLock
    {
        /// <summary>
        /// Migration lock identifier.
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Time the lock was acquired at.
        /// </summary>
        public DateTime AcquiredAt { get; set; }
    }
}
