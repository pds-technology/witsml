//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using Energistics.DataAccess;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Represents a serializer for <see cref="Timestamp"/> instances.
    /// </summary>
    /// <seealso cref="MongoDB.Bson.Serialization.Serializers.SerializerBase{Timestamp}" />
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
