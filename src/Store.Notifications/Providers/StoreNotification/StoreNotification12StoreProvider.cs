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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Energistics.Etp.v12.Protocol.StoreNotification;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Providers.StoreNotification
{
    /// <summary>
    /// Default implementation of a Store Notification Store provider.
    /// </summary>
    /// <seealso cref="StoreNotification12StoreProviderBase" />
    [Export(typeof(IStoreNotificationStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class StoreNotification12StoreProvider : StoreNotification12StoreProviderBase
    {
        private readonly IDictionary<string, object> _config;
        private readonly StringDeserializer _keyDeserializer;
        private readonly StringDeserializer _valueDeserializer;
        private readonly TimeSpan _timeout;
        private Consumer<string, string> _consumer;
        private bool _isCancelled;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreNotification12StoreProvider"/> class.
        /// </summary>
        public StoreNotification12StoreProvider()
        {
            _timeout = TimeSpan.FromMilliseconds(KafkaSettings.PollingIntervalInMilliseconds);
            _keyDeserializer = new StringDeserializer(Encoding.UTF8);
            _valueDeserializer = new StringDeserializer(Encoding.UTF8);

            _config = new Dictionary<string, object>
            {
                {KafkaSettings.DebugKey, KafkaSettings.DebugContexts},
                {KafkaSettings.BrokerListKey, KafkaSettings.BrokerList},
                {KafkaSettings.EnableAutoCommitKey, KafkaSettings.EnableAutoCommit}
            };
        }

        /// <summary>
        /// Ensures the connection to the message broker.
        /// </summary>
        protected override void EnsureConnection()
        {
            // No action if consumer already subscribed or broker list not configured
            if (_consumer != null || string.IsNullOrWhiteSpace(KafkaSettings.BrokerList)) return;

            // Set the group identifier
            _config[KafkaSettings.GroupIdKey] = Session.ApplicationName;

            // Create and configure a new Consumer instance
            _consumer = new Consumer<string, string>(_config, _keyDeserializer, _valueDeserializer);

            _consumer.OnPartitionsAssigned += (sender, partitions) =>
            {
                Logger?.Debug($"Assigned partitions: [{string.Join(", ", partitions)}], member id: {_consumer.MemberId}");
                _consumer.Assign(partitions);
            };

            _consumer.OnPartitionsRevoked += (sender, partitions) =>
            {
                Logger?.Warn($"Revoked partitions: [{string.Join(", ", partitions)}]");
                _consumer.Unassign();
            };

            _consumer.OnError += (sender, error) =>
            {
                Logger?.Error($"Error: {error}");
            };

            _consumer.OnMessage += OnMessage;
            _consumer.Subscribe(new[] {KafkaSettings.UpsertTopicName, KafkaSettings.DeleteTopicName});

            Task.Run(() =>
            {
                try
                {
                    while (!_isCancelled)
                    {
                        _consumer?.Poll(_timeout);
                    }
                }
                catch (Exception ex)
                {
                    Logger?.Warn("Error polling message broker", ex);
                }
            });
        }

        /// <summary>
        /// Disconnects from the message broker.
        /// </summary>
        protected override void Disconnect()
        {
            _isCancelled = true;
            _consumer?.Dispose();
            _consumer = null;
        }

        private void OnMessage(object sender, Message<string, string> message)
        {
            Logger?.Debug($"Topic: {message.Topic}; Partition: {message.Partition}; Offset: {message.Offset}; {message.Value}");

            // Extract message values
            var uri = message.Key;
            var dataObject = message.Value;
            var timestamp = DateTime.UtcNow;

            // Detect Insert/Update/Delete based on topic name
            if (KafkaSettings.InsertTopicName.EqualsIgnoreCase(message.Topic))
            {
                OnNotifyInsert(uri, dataObject, timestamp);
            }
            else if (KafkaSettings.UpdateTopicName.EqualsIgnoreCase(message.Topic))
            {
                OnNotifyUpdate(uri, dataObject, timestamp);
            }
            else if (KafkaSettings.DeleteTopicName.EqualsIgnoreCase(message.Topic))
            {
                OnNotifyDelete(uri, dataObject, timestamp);
            }
        }
    }
}
