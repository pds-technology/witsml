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

using PDS.WITSMLstudio.Store.Kafka.Properties;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Encapsulates all message broker configuration settings.
    /// </summary>
    public static class KafkaSettings
    {
        /// <summary>
        /// Initial list of brokers as a CSV list of broker host or host:port.
        ///
        /// Preferably at least 2 are set.
        /// </summary>
        public static string BrokerList = Settings.Default.KafkaBrokerList;

        /// <summary>
        /// When set to true, the producer will ensure that messages are successfully produced exactly once
        /// and in the original produce order. The following configuration properties are adjusted
        /// automatically (if not modified by the user) when idempotence is enabled:
        /// 
        ///     max.in.flight.requests.per.connection=5 (must be less than or equal to 5)
        ///     retries=INT32_MAX (must be greater than 0)
        ///     acks=all
        ///     queuing.strategy=fifo
        ///
        /// Producer instantiation will fail if user-supplied configuration is incompatible.        
        /// </summary>
        public static bool EnableIdempotence = Settings.Default.KafkaEnableIdempotence;

        public static string ConsumerGroupIdPrefix = Settings.Default.KafkaConsumerGroupIdPrefix;

        public static string SecurityProtocol = Settings.Default.KafkaSecurityProtocol;
        public static string SaslMechanism = Settings.Default.KafkaSaslMechanism;
        public static string SaslUsername = Settings.Default.KafkaSaslUsername;
        public static string SaslPassword = Settings.Default.KafkaSaslPassword;
        public static string CompressionType = Settings.Default.KafkaCompressionType;
        public static int MessageMaxBytes = Settings.Default.KafkaMessageMaxBytes;

        public const string SecurityProtocolKey = "security.protocol";
        public const string SaslMechanismKey = "sasl.mechanism";
        public const string SaslUsernameKey = "sasl.username";
        public const string SaslPasswordKey = "sasl.password";
    }
}
