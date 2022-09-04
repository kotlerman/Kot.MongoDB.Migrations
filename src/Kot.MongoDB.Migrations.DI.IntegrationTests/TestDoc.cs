using MongoDB.Bson.Serialization.Attributes;

namespace Kot.MongoDB.Migrations.DI.IntegrationTests
{
    [BsonIgnoreExtraElements]
    internal class TestDoc
    {
        public const string CollectionName = "DocCollection";

        public string ValueA { get; set; }

        public string ValueB { get; set; }
    }
}
