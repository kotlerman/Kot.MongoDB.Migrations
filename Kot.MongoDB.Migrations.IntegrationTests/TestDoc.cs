using MongoDB.Bson.Serialization.Attributes;

namespace Kot.MongoDB.Migrations.IntegrationTests
{
    [BsonIgnoreExtraElements]
    internal class TestDoc
    {
        public const string CollectionName = "DocCollection";

        public string Value { get; set; }
    }
}
