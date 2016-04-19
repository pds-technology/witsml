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
using System.IO.Compression;
using System.Linq;
using Avro.IO;
using Avro.Specific;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.ChannelStreaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Energistics.Common
{
    /// <summary>
    /// Provides extension methods that can be used along with ETP message types.
    /// </summary>
    public static class EtpExtensions
    {
        private const string GzipEncoding = "gzip";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            ContractResolver = new EtpContractResolver(),
            Converters = new List<JsonConverter>()
            {
                new StringEnumConverter()
            }
        };

        /// <summary>
        /// Encodes the specified message header and body.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="body">The message body.</param>
        /// <param name="header">The message header.</param>
        /// <returns>The encoded byte array containing the message data.</returns>
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

        /// <summary>
        /// Decodes the message body using the specified decoder.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="decoder">The decoder.</param>
        /// <returns>The decoded message body.</returns>
        public static T Decode<T>(this Decoder decoder) where T : ISpecificRecord
        {
            var record = Activator.CreateInstance<T>();
            var reader = new SpecificReader<T>(record.Schema, record.Schema);

            reader.Read(record, decoder);

            return record;
        }

        /// <summary>
        /// Serializes the specified object instance.
        /// </summary>
        /// <param name="etpBase">The ETP base object.</param>
        /// <param name="instance">The object to serialize.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string Serialize(this EtpBase etpBase, object instance)
        {
            return etpBase.Serialize(instance, false);
        }

        /// <summary>
        /// Serializes the specified object instance.
        /// </summary>
        /// <param name="etpBase">The ETP base object.</param>
        /// <param name="instance">The object to serialize.</param>
        /// <param name="indent">if set to <c>true</c> the JSON output should be indented; otherwise, <c>false</c>.</param>
        /// <returns>The serialized JSON string.</returns>
        public static string Serialize(this EtpBase etpBase, object instance, bool indent)
        {
            var formatting = (indent) ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(instance, formatting, JsonSettings);
        }

        /// <summary>
        /// Determines whether the list of supported protocols contains the specified protocol and role combination.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        /// <param name="protocol">The requested protocol.</param>
        /// <param name="role">The requested role.</param>
        /// <returns>A value indicating whether the specified protocol and role combination is supported.</returns>
        public static bool Contains(this IList<SupportedProtocol> supportedProtocols, int protocol, string role)
        {
            return supportedProtocols.Any(x => x.Protocol == protocol &&
                string.Equals(x.Role, role, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Determines whether the list of supported protocols indicates the producer is a simple streamer.
        /// </summary>
        /// <param name="supportedProtocols">The supported protocols.</param>
        /// <returns></returns>
        public static bool IsSimpleStreamer(this IList<SupportedProtocol> supportedProtocols)
        {
            return supportedProtocols.Any(x =>
            {
                DataValue dataValue;
                return (
                    x.Protocol == (int)Protocols.ChannelStreaming &&
                    x.ProtocolCapabilities.TryGetValue(ChannelStreamingProducerHandler.SimpleStreamer, out dataValue) &&
                    Convert.ToBoolean(dataValue.Item)
                );
            });
        }

        /// <summary>
        /// Decodes the data contained by the <see cref="DataObject"/> as an XML string.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The decoded XML string.</returns>
        public static string GetXml(this DataObject dataObject)
        {
            return System.Text.Encoding.UTF8.GetString(dataObject.GetData());
            //return System.Text.Encoding.Unicode.GetString(dataObject.GetData());
        }

        /// <summary>
        /// Encodes and optionally compresses the XML string for the <see cref="DataObject"/> data.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="xml">The XML string.</param>
        /// <param name="compress">if set to <c>true</c> the data will be compressed.</param>
        public static void SetXml(this DataObject dataObject, string xml, bool compress = true)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                dataObject.SetData(new byte[0], compress);
                return;
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(xml);

            //var bytes = System.Text.Encoding.Convert(
            //    System.Text.Encoding.UTF8,
            //    System.Text.Encoding.Unicode,
            //    System.Text.Encoding.UTF8.GetBytes(xml));

            dataObject.SetData(bytes, compress);
        }

        /// <summary>
        /// Gets the data contained by the <see cref="DataObject"/> and decompresses the byte array, if necessary.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The decompressed data as a byte array.</returns>
        private static byte[] GetData(this DataObject dataObject)
        {
            if (string.IsNullOrWhiteSpace(dataObject.ContentEncoding))
                return dataObject.Data;

            if (!GzipEncoding.Equals(dataObject.ContentEncoding, StringComparison.InvariantCultureIgnoreCase))
                throw new NotSupportedException("Content encoding not supported: " + dataObject.ContentEncoding);

            using (var uncompressed = new MemoryStream())
            {
                using (var compressed = new MemoryStream(dataObject.Data))
                using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                {
                    gzip.CopyTo(uncompressed);
                }

                return uncompressed.GetBuffer();
            }
        }

        /// <summary>
        /// Sets and optionally compresses the data for the <see cref="DataObject"/>.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="data">The data.</param>
        /// <param name="compress">if set to <c>true</c> the data will be compressed.</param>
        private static void SetData(this DataObject dataObject, byte[] data, bool compress = true)
        {
            var encoding = string.Empty;

            if (compress)
            {
                using (var compressed = new MemoryStream())
                {
                    using (var uncompressed = new MemoryStream(data))
                    using (var gzip = new GZipStream(compressed, CompressionMode.Compress, true))
                    {
                        uncompressed.CopyTo(gzip);
                    }

                    data = compressed.GetBuffer();
                    encoding = GzipEncoding;
                }
            }

            dataObject.ContentEncoding = encoding;
            dataObject.Data = data;
        }
    }
}
