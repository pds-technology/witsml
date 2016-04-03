//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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

using System;
using System.Collections.Generic;
using System.IO;
using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Energistics.Common
{
    public static class EtpExtensions
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            ContractResolver = new EtpContractResolver(),
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter()
            }
        };

        public static byte[] Encode<T>(this T body, MessageHeader header) where T : ISpecificRecord
        {
            using (var stream = new MemoryStream())
            {
                // create avro binary encoder to write to memory stream
                var encoder = new BinaryEncoder(stream);

                // serialize header
                var headerWriter = new SpecificWriter<MessageHeader>(header.Schema);
                headerWriter.Write(header, encoder);

                // serialize body
                var bodyWriter = new SpecificWriter<T>(body.Schema);
                bodyWriter.Write(body, encoder);

                return stream.ToArray();
            }
        }

        public static T Decode<T>(this Decoder decoder) where T : ISpecificRecord
        {
            var record = Activator.CreateInstance<T>();
            var reader = new SpecificReader<T>(record.Schema, record.Schema);

            reader.Read(record, decoder);

            return record;
        }

        public static string Serialize(this EtpBase etpBase, object instance)
        {
            return etpBase.Serialize(instance, false);
        }

        public static string Serialize(this EtpBase etpBase, object instance, bool indent)
        {
            var formatting = (indent) ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(instance, formatting, JsonSettings);
        }
    }
}
