//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
using System.Linq;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Discovery
{
    /// <summary>
    /// Base implementation of the <see cref="IDiscoveryStore"/> interface.
    /// </summary>
    /// <seealso cref="Energistics.Common.EtpProtocolHandler" />
    /// <seealso cref="Energistics.Protocol.Discovery.IDiscoveryStore" />
    public class DiscoveryStoreHandler : EtpProtocolHandler, IDiscoveryStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStoreHandler"/> class.
        /// </summary>
        public DiscoveryStoreHandler() : base(Protocols.Discovery, "store", "customer")
        {
        }

        /// <summary>
        /// Sends a GetResourcesResponse message to a customer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="resources">The list of <see cref="Resource" /> objects.</param>
        public virtual void GetResourcesResponse(MessageHeader request, IList<Resource> resources)
        {
            if (!resources.Any())
            {
                Acknowledge(request.MessageId, MessageFlags.NoData);
                return;
            }

            for (int i=0; i<resources.Count; i++)
            {
                var messageFlags = i < resources.Count - 1
                    ? MessageFlags.MultiPart
                    : MessageFlags.FinalPart;

                var header = CreateMessageHeader(Protocols.Discovery, MessageTypes.Discovery.GetResourcesResponse, request.MessageId, messageFlags);

                var getResourcesResponse = new GetResourcesResponse()
                {
                    Resource = resources[i]
                };

                Session.SendMessage(header, getResourcesResponse);
            }
        }

        /// <summary>
        /// Handles the GetResources event from a customer.
        /// </summary>
        public event ProtocolEventHandler<GetResources, IList<Resource>> OnGetResources;

        /// <summary>
        /// Decodes the message based on the message type contained in the specified <see cref="MessageHeader" />.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="decoder">The message decoder.</param>
        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Discovery.GetResources:
                    HandleGetResources(header, decoder.Decode<GetResources>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        /// <summary>
        /// Handles the GetResources message from a customer.
        /// </summary>
        /// <param name="header">The message header.</param>
        /// <param name="getResources">The GetResources message.</param>
        protected virtual void HandleGetResources(MessageHeader header, GetResources getResources)
        {
            var args = Notify(OnGetResources, header, getResources, new List<Resource>());
            HandleGetResources(args);

            GetResourcesResponse(header, args.Context);
        }

        /// <summary>
        /// Handles the GetResources message from a customer.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources}"/> instance containing the event data.</param>
        protected virtual void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
        }
    }
}
