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
using Confluent.Kafka;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data
{
    public static class KafkaUtil
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(KafkaUtil));

        private static readonly List<SyslogLevel> _logLevels = new List<SyslogLevel>
        {
            SyslogLevel.Emergency,
            SyslogLevel.Alert,
            SyslogLevel.Critical,
            SyslogLevel.Error,
            SyslogLevel.Warning,
            SyslogLevel.Notice
        };

        public static ProducerConfig CreateProducerConfig()
        {
            var clientConfig = CreateClientConfig();

            CompressionType compressionType;
            if (!Enum.TryParse(KafkaSettings.CompressionType ?? "None", out compressionType))
            {
                compressionType = CompressionType.None;
            }

            return new ProducerConfig(clientConfig)
            {
                EnableIdempotence = KafkaSettings.EnableIdempotence,
                CompressionType = compressionType,
                MessageMaxBytes = KafkaSettings.MessageMaxBytes,
            };
        }

        public static ConsumerConfig CreateConsumerConfig()
        {
            var clientConfig = CreateClientConfig();

            return new ConsumerConfig(clientConfig)
            {
                GroupId = KafkaSettings.ConsumerGroupIdPrefix + DateTime.UtcNow.ToOADate()
            };
        }

        public static ClientConfig CreateClientConfig()
        {
            var advancedConfig = new Dictionary<string, string>
            {
                { KafkaSettings.SecurityProtocolKey, KafkaSettings.SecurityProtocol }
            };

            // pass username and password only if the mechanism is defined
            if (!string.IsNullOrWhiteSpace(KafkaSettings.SaslMechanism))
            {
                advancedConfig.Add(KafkaSettings.SaslMechanismKey, KafkaSettings.SaslMechanism);
                advancedConfig.Add(KafkaSettings.SaslUsernameKey, KafkaSettings.SaslUsername);
                advancedConfig.Add(KafkaSettings.SaslPasswordKey, KafkaSettings.SaslPassword);
            }

            var config = new ClientConfig(advancedConfig)
            {
                BootstrapServers = KafkaSettings.BrokerList
            };

            _log.DebugFormat("Broker List: {0}, Security Protocol: {1}, SASL Mechanism: {2}",
                KafkaSettings.BrokerList, KafkaSettings.SecurityProtocol, KafkaSettings.SaslMechanism);

            return config;
        }

        public static void OnClientError(IClient client, Error error)
        {
            var originator = error.IsBrokerError ? "broker" : error.IsLocalError ? "local" : "unknown";
            var fatal = error.IsFatal ? "fatal" : string.Empty;
            var err = error.IsError ? "error" : string.Empty;

            _log.Error($"{client.Name} {fatal}{err} occurred: originator {originator}, reason {error.Reason}");
        }

        public static void OnClientWarning(IClient client, LogMessage message)
        {
            if (_logLevels.Contains(message.Level))
            {
                _log.Warn($"{client.Name} [{message.Level}]{message.Facility}: {message.Name} - {message.Message}");
            }
        }
    }
}
