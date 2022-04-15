using FluentAssertions;
using Kot.MongoDB.Migrations.Serializers;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using System.IO;
using System.Text.Json;

namespace Kot.MongoDB.Migrations.Tests
{
    [TestFixture]
    public class DatabaseVersionSerializerTests
    {
        [Test]
        public void Serialization_Success()
        {
            // Arrange
            var stringWriter = new StringWriter();
            var jsonWriter = new JsonWriter(stringWriter);
            var context = BsonSerializationContext.CreateRoot(jsonWriter);
            var serializer = new DatabaseVersionSerializer();
            var dbVersion = new DatabaseVersion(1, 2, 3);

            // Act
            serializer.Serialize(context, dbVersion);

            // Assert
            var actualJson = stringWriter.GetStringBuilder().ToString();
            var actualDbVersion = JsonSerializer.Deserialize<DatabaseVersionDoc>(actualJson);
            actualDbVersion.Should().BeEquivalentTo(dbVersion, opt => opt.ComparingByMembers<DatabaseVersion>());
        }

        [Test]
        public void Deserialization_Success()
        {
            // Arrange
            var dbVersion = new DatabaseVersion(1, 2, 3);
            var json = JsonSerializer.Serialize(dbVersion);
            var stringReader = new StringReader(json);
            var jsonReader = new JsonReader(stringReader);
            var context = BsonDeserializationContext.CreateRoot(jsonReader);
            var serializer = new DatabaseVersionSerializer();

            // Act
            var actualDbVersion = serializer.Deserialize(context);

            // Assert
            actualDbVersion.Should().BeEquivalentTo(dbVersion);
        }

        class DatabaseVersionDoc
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Patch { get; set; }
        }
    }
}
