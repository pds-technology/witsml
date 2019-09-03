//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using Energistics.DataAccess;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Represents a serializer for <see cref="ExtensibleEnum{TEnum}"/> instances.
    /// </summary>
    /// <seealso cref="MongoDB.Bson.Serialization.Serializers.SerializerBase{ExtensibleEnum{TEnum}}" />
    public class ExtensibleEnumSerializer<TEnum> : StructSerializerBase<ExtensibleEnum<TEnum>>
        where TEnum : struct, IComparable
    {
        private BsonStringSerializer _serializer;
        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampSerializer"/> class.
        /// </summary>
        public ExtensibleEnumSerializer()
            : base()
        {
            _serializer = new BsonStringSerializer();
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ExtensibleEnum<TEnum> value)
        {
            _serializer.Serialize(context, args, value.ToString());
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override ExtensibleEnum<TEnum> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var dataType = context.Reader.GetCurrentBsonType();
            var enumValue = string.Empty;

            if (dataType == BsonType.String)
            {
                enumValue = context.Reader.ReadString();
            }
            else if (dataType == BsonType.Int32)
            {
                var intValue = context.Reader.ReadInt32();
                enumValue = ((TEnum)(object)intValue).ToString();
            }

            return new ExtensibleEnum<TEnum>(enumValue);
        }
    }
}
