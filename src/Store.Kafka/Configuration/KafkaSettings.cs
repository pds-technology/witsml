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

using Energistics.Etp.Common;
using PDS.WITSMLstudio.Store.Kafka.Properties;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Enacpsulates all message broker configuration settings.
    /// </summary>
    public static class KafkaSettings
    {
        public static int PollingIntervalInMilliseconds = Settings.Default.KafkaPollingIntervalInMilliseconds;
        public static bool EnableAutoCommit = Settings.Default.KafkaEnableAutoCommit;
        public static string BrokerList = Settings.Default.KafkaBrokerList;
        public static string DebugContexts = Settings.Default.KafkaDebugContexts;
        public static string SecurityProtocol = Settings.Default.KafkaSecurityProtocol;
        public static string SaslMechanism = Settings.Default.KafkaSaslMechanism;
        public static string SaslUsername = Settings.Default.KafkaSaslUsername;
        public static string SaslPassword = Settings.Default.KafkaSaslPassword;

        public const string DebugKey = "debug";
        public const string GroupIdKey = "group.id";
        public const string BrokerListKey = "bootstrap.servers";
        public const string EnableAutoCommitKey = "enable.auto.commit";
        public const string SecurityProtocolKey = "security.protocol";
        public const string SaslMechanismKey = "sasl.mechanism";
        public const string SaslUsernameKey = "sasl.username";
        public const string SaslPasswordKey = "sasl.password";
    }
}
