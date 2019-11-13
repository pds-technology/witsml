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
using Confluent.Kafka;

namespace PDS.WITSMLstudio.Store.Data
{
    [Export(typeof(IKafkaConsumerProvider))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class KafkaConsumerProvider : IKafkaConsumerProvider
    {
        private readonly Lazy<IConsumer<string, string>> _consumer;

        [ImportingConstructor]
        public KafkaConsumerProvider()
        {
            _consumer = new Lazy<IConsumer<string, string>>(BuildConsumer);
        }

        public KafkaConsumerProvider(ConsumerConfig config)
        {
            _consumer = new Lazy<IConsumer<string, string>>(() => BuildConsumer(config));
        }

        public IConsumer<string, string> Consumer => _consumer.Value;

        protected virtual IConsumer<string, string> BuildConsumer()
        {
            var config = KafkaUtil.CreateConsumerConfig();

            return BuildConsumer(config);
        }

        protected virtual IConsumer<string, string> BuildConsumer(ConsumerConfig config)
        {
            return new ConsumerBuilder<string, string>(config)
                .SetErrorHandler(KafkaUtil.OnClientError)
                .SetLogHandler(KafkaUtil.OnClientWarning)
                .Build();
        }
    }
}
