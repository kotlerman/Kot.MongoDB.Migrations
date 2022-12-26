using System;
using System.Collections.Generic;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Represents a migration result.
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// Time when migration started.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Time when migration finished.
        /// </summary>
        public DateTime FinishTime { get; set; }

        /// <summary>
        /// Version of a database before migration.
        /// </summary>
        public DatabaseVersion? InitialVersion { get; set; }

        /// <summary>
        /// Version of a database after migration.
        /// </summary>
        public DatabaseVersion? FinalVersion { get; set; }

        /// <summary>
        /// List of migrations that were applied.
        /// </summary>
        public List<IMongoMigration> AppliedMigrations { get; set; }
    }
}
