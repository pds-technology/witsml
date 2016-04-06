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
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Protocol.Core;

namespace Energistics.Protocol.Discovery
{
    public class DiscoveryCustomerHandler : EtpProtocolHandler, IDiscoveryCustomer
    {
        private readonly IDictionary<long, string> _requests;

        public DiscoveryCustomerHandler() : base(Protocols.Discovery, "customer", "store")
        {
            _requests = new Dictionary<long, string>();
        }

        public virtual void GetResources(string uri)
        {
            var header = CreateMessageHeader(Protocols.Discovery, MessageTypes.Discovery.GetResources);

            var getResources = new GetResources()
            {
                Uri = uri
            };

            _requests[header.MessageId] = uri;

            Session.SendMessage(header, getResources);
        }

        public event ProtocolEventHandler<GetResourcesResponse, string> OnGetResourcesResponse;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.Discovery.GetResourcesResponse:
                    HandleGetResourcesResponse(header, decoder.Decode<GetResourcesResponse>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected override void HandleAcknowledge(MessageHeader header, Acknowledge acknowledge)
        {
            // Handle case when "No Data" Acknowledge message was received
            if (header.MessageFlags == (int)MessageFlags.NoData)
            {
                var uri = GetRequestedUri(header);
                HandleAcknowledge(header, acknowledge, uri);
                return;
            }

            base.HandleAcknowledge(header, acknowledge);
        }

        protected virtual void HandleAcknowledge(MessageHeader header, Acknowledge acknowledge, string uri)
        {
            var args = Notify(OnGetResourcesResponse, header, new GetResourcesResponse(), uri);
            HandleGetResourcesResponse(args);
        }

        protected virtual void HandleGetResourcesResponse(MessageHeader header, GetResourcesResponse getResourcesResponse)
        {
            var uri = GetRequestedUri(header);
            var args = Notify(OnGetResourcesResponse, header, getResourcesResponse, uri);
            HandleGetResourcesResponse(args);
        }

        protected virtual void HandleGetResourcesResponse(ProtocolEventArgs<GetResourcesResponse, string> args)
        {
        }

        private string GetRequestedUri(MessageHeader header)
        {
            var uri = string.Empty;

            if (_requests.TryGetValue(header.CorrelationId, out uri) && header.MessageFlags != (int)MessageFlags.MultiPart)
            {
                _requests.Remove(header.CorrelationId);
            }

            return uri;
        }
    }
}
