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
