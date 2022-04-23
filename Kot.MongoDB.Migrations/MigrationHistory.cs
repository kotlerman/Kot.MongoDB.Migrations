using Kot.MongoDB.Migrations.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Kot.MongoDB.Migrations
{
    public class MigrationHistory
    {
        public ObjectId Id { get; set; }

        [BsonSerializer(typeof(DatabaseVersionSerializer))]
        public DatabaseVersion Version { get; set; }

        public string Name { get; set; }

        public DateTime AppliedAt { get; set; }
    }
}
