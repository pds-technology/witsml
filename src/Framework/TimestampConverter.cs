//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Converts a <see cref="Timestamp"/> to and from JSON.
    /// </summary>
    public class TimestampConverter : IsoDateTimeConverter
    {
        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns><c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Timestamp) || objectType == typeof(Timestamp?);
        }

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Cast Timestamp and Timestamp? to DateTimeOffset
            value = (DateTimeOffset) (Timestamp) value;

            base.WriteJson(writer, value, serializer);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read. If there is no existing value then <c>null</c> will be used.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            objectType = Nullable.GetUnderlyingType(objectType) == null
                ? typeof(DateTimeOffset)
                : typeof(DateTimeOffset?);

            if (existingValue != null)
            {
                // Cast Timestamp and Timestamp? to DateTimeOffset
                existingValue = (DateTimeOffset) (Timestamp) existingValue;
            }

            var result = base.ReadJson(reader, objectType, existingValue, serializer);

            if (result != null)
            {
                // Cast DateTime and DateTimeOffset to Timestamp
                result = (Timestamp) (DateTimeOffset) result;
            }

            return result;
        }
    }
}
