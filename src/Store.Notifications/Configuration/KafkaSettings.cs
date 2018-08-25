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

using PDS.WITSMLstudio.Store.Notifications;

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
        public static string InsertTopicName = Settings.Default.KafkaInsertTopicName;
        public static string UpdateTopicName = Settings.Default.KafkaUpdateTopicName;
        public static string UpsertTopicName = Settings.Default.KafkaUpsertTopicName;
        public static string DeleteTopicName = Settings.Default.KafkaDeleteTopicName;
        public static string DebugContexts = Settings.Default.KafkaDebugContexts;

        public const string DebugKey = "debug";
        public const string GroupIdKey = "group.id";
        public const string BrokerListKey = "bootstrap.servers";
        public const string EnableAutoCommitKey = "enable.auto.commit";
    }
}
