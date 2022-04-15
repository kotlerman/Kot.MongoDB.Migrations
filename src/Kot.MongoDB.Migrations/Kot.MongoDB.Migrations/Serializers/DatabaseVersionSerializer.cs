using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Kot.MongoDB.Migrations.Serializers
{
    public class DatabaseVersionSerializer : SerializerBase<DatabaseVersion>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DatabaseVersion value)
        {
            context.Writer.WriteStartDocument();

            context.Writer.WriteName(nameof(DatabaseVersion.Major));
            context.Writer.WriteInt32(value.Major);

            context.Writer.WriteName(nameof(DatabaseVersion.Minor));
            context.Writer.WriteInt32(value.Minor);

            context.Writer.WriteName(nameof(DatabaseVersion.Patch));
            context.Writer.WriteInt32(value.Patch);

            context.Writer.WriteEndDocument();
        }

        public override DatabaseVersion Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            int major = 0;
            int minor = 0;
            int patch = 0;

            context.Reader.ReadStartDocument();

            if (context.Reader.FindElement(nameof(DatabaseVersion.Major)))
            {
                major = context.Reader.ReadInt32();
            }

            if (context.Reader.FindElement(nameof(DatabaseVersion.Minor)))
            {
                minor = context.Reader.ReadInt32();
            }

            if (context.Reader.FindElement(nameof(DatabaseVersion.Patch)))
            {
                patch = context.Reader.ReadInt32();
            }

            context.Reader.ReadEndDocument();

            return new DatabaseVersion(major, minor, patch);
        }
    }
}
