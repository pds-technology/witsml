//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Datatypes;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.Framework;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Providers.Core
{
    [Export(typeof(ICoreServer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CoreServerProvider : CoreServerHandler
    {
        private static readonly int MaxMessageRate = Settings.Default.MaxMessageRate;
        private const string Consumer = "consumer";

        protected override void HandleRequestSession(MessageHeader header, RequestSession requestSession)
        {
            // The base implementation sends the OpenSession message
            base.HandleRequestSession(header, requestSession);

            // Is the client requesting the ChannelStreaming consumer role
            var isClientProducer = HasConsumerRole(RequestedProtocols);

            // Does the server support the ChannelStreaming consumer role
            if (isClientProducer && Session.CanHandle<IChannelStreamingConsumer>())
            {
                // Start a ChannelStreaming session
                Session.Handler<IChannelStreamingConsumer>()
                    .Start(maxMessageRate: MaxMessageRate);
            }
        }

        private bool HasConsumerRole(IList<SupportedProtocol> protocols)
        {
            return protocols.Any(x => x.Protocol == (int)Protocols.ChannelStreaming && Consumer.EqualsIgnoreCase(x.Role));
        }
    }
}
