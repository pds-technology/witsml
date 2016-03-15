using Energistics.DataAccess;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Represents a serializer for <see cref="Timestamp"/> instances.
    /// </summary>
    /// <seealso cref="MongoDB.Bson.Serialization.Serializers.SerializerBase{Energistics.DataAccess.Timestamp}" />
    public class TimestampSerializer : SerializerBase<Timestamp>
    {
        private readonly DateTimeOffsetSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampSerializer"/> class.
        /// </summary>
        public TimestampSerializer()
        {
            _serializer = new DateTimeOffsetSerializer(BsonType.String);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Timestamp value)
        {
            _serializer.Serialize(context, args, value);
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override Timestamp Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return _serializer.Deserialize(context, args);
        }
    }
}
