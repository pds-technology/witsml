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
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides a method to send data object messages using a Kafka producer.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectMessageProducer" />
    [Export(typeof(IDataObjectMessageProducer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class KafkaMessageProducer : IDataObjectMessageProducer, IDisposable
    {
        #region Fields 

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(KafkaMessageProducer));

        private readonly IDictionary<string, object> _config;
        private readonly StringSerializer _keySerializer;
        private readonly StringSerializer _valueSerializer;

        private bool _disposed = false;

        #endregion

        #region Constructors 

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaMessageProducer"/> class.
        /// </summary>
        [ImportingConstructor]
        public KafkaMessageProducer()
        {
            _keySerializer = new StringSerializer(Encoding.UTF8);
            _valueSerializer = new StringSerializer(Encoding.UTF8);

            _config = new Dictionary<string, object>
            {
                {KafkaSettings.DebugKey, KafkaSettings.DebugContexts},
                {KafkaSettings.BrokerListKey, KafkaSettings.BrokerList}
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="key">The key.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>An awaitable task.</returns>
        public async Task SendMessageAsync(string topic, string key, string payload)
        {
            using (var producer = new Producer<string, string>(_config, _keySerializer, _valueSerializer))
            {
                _log.Debug($"{producer.Name} producing on topic: {topic}; key: {key}; message:{Environment.NewLine}{payload}");

                var message = await producer.ProduceAsync(topic, key, payload);

                if (message.Error)
                {
                    throw new KafkaException(message.Error);
                }

                _log.Debug($"Partition: {message.Partition}; Offset: {message.Offset}");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, 
        /// releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        #region Private / Protected Methods

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources
        /// <c>false</c> to release only unmanaged resources
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _keySerializer.Dispose();
                    _valueSerializer.Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }
}