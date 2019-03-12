//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Collections.Generic;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Kafka data adapter that encapsulates CRUD functionality for WITSML objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.MessageDataAdapter{T}" />
    public abstract class KafkaDataAdapter<T> : MessageDataAdapter<T> where T : class
    {
        private readonly IDictionary<string, object> _config;
        private readonly StringSerializer _keySerializer;
        private readonly StringSerializer _valueSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaDataAdapter{T}"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="objectName">The object name.</param>
        protected KafkaDataAdapter(IContainer container, ObjectName objectName) : base(container, objectName)
        {
            _keySerializer = new StringSerializer(Encoding.UTF8);
            _valueSerializer = new StringSerializer(Encoding.UTF8);

            _config = new Dictionary<string, object>
            {
                {KafkaSettings.DebugKey, KafkaSettings.DebugContexts},
                {KafkaSettings.BrokerListKey, KafkaSettings.BrokerList}
            };
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="key">The key.</param>
        /// <param name="payload">The payload.</param>
        protected override void SendMessageCore(string topic, string key, string payload)
        {
            using (var producer = new Producer<string, string>(_config, _keySerializer, _valueSerializer))
            {
                Logger.Debug($"{producer.Name} producing on topic: {topic}; key: {key}; message:{Environment.NewLine}{payload}");

                var task = producer.ProduceAsync(topic, key, payload);
                var result = task.Result;

                Logger.Debug($"Partition: {result.Partition}; Offset: {result.Offset}");
            }
        }
    }
}