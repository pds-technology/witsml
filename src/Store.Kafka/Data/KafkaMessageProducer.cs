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
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides a method to send data object messages using a Kafka producer.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.MessageProducerBase" />
    /// <seealso cref="System.IDisposable" />
    [Export(typeof(IDataObjectMessageProducer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class KafkaMessageProducer : MessageProducerBase, IDisposable
    {
        #region Fields & Properties

        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(KafkaMessageProducer));

        private IKafkaProducerProvider ProducerProvider { get; }

        private IProducer<string, string> Producer => ProducerProvider.Producer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaMessageProducer"/> class.
        /// </summary>
        public KafkaMessageProducer() : this(new KafkaProducerProvider())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaMessageProducer"/> class.
        /// </summary>
        [ImportingConstructor]
        public KafkaMessageProducer(IKafkaProducerProvider producerProvider)
        {
            ProducerProvider = producerProvider;
            _log.Debug("Instance created.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="key">The key.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>An awaitable task.</returns>
        public override async Task SendMessageAsync(string topic, string key, string payload)
        {
            _log.Debug($"{Producer.Name} producing on topic: {topic}; key: {key}; message:{Environment.NewLine}{payload}");

            var message = await Producer.ProduceAsync(topic,
                new Message<string, string>
                {
                    Key = key, Value = payload
                });

            _log.Debug($"Partition: {message.Partition}; Offset: {message.Offset}");
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Producer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}