using Kot.MongoDB.Migrations.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Kot.MongoDB.Migrations
{
    /// <summary>
    /// Represents a migration history record.
    /// </summary>
    public class MigrationHistory
    {
        /// <summary>
        /// Migration history record identifier.
        /// </summary>
        public ObjectId Id { get; set; }

        /// <summary>
        /// Version that the database was updated to as the migration was applied.
        /// </summary>
        [BsonSerializer(typeof(DatabaseVersionSerializer))]
        public DatabaseVersion Version { get; set; }

        /// <summary>
        /// Migration name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Time the migration was applied at.
        /// </summary>
        public DateTime AppliedAt { get; set; }
    }
}
