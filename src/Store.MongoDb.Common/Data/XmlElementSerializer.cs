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

using System.Xml;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Represents a serializer for <see cref="XmlElement"/> instances.
    /// </summary>
    /// <seealso cref="MongoDB.Bson.Serialization.Serializers.SerializerBase{XmlElement}" />
    public class XmlElementSerializer : SerializerBase<XmlElement>
    {
        private const string Xmlns = "xmlns";

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The value.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, XmlElement value)
        {
            var writer = context.Writer;

            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (!string.IsNullOrWhiteSpace(value.NamespaceURI) && !value.HasAttribute(Xmlns))
            {
                value.SetAttribute(Xmlns, value.NamespaceURI);
            }

            var json = JsonConvert.SerializeXmlNode(value);
            writer.WriteJavaScript(json);
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override XmlElement Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            if (reader.GetCurrentBsonType() == BsonType.Null)
            {
                return null;
            }

            var json = reader.ReadJavaScript();

            if (string.IsNullOrWhiteSpace(json) || json == JsonConvert.Null)
            {
                return null;
            }

            var doc = JsonConvert.DeserializeXmlNode(json);
            return doc.DocumentElement;
        }
    }
}
