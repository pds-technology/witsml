using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.Discovery
{
    public class DiscoveryCustomerHandler : EtpProtocolHandler, IDiscoveryCustomer
    {
        private readonly IDictionary<long, string> _requests;

        public DiscoveryCustomerHandler() : base(Protocols.Discovery, "customer")
        {
            RequestedRole = "store";
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

        protected virtual void HandleGetResourcesResponse(MessageHeader header, GetResourcesResponse getResourcesResponse)
        {
            var uri = string.Empty;

            if (_requests.TryGetValue(header.CorrelationId, out uri) && header.MessageFlags == (int)MessageFlags.FinalPart)
            {
                _requests.Remove(header.CorrelationId);
            }

            var args = Notify(OnGetResourcesResponse, header, getResourcesResponse, uri);
            HandleGetResourcesResponse(args);
        }

        protected virtual void HandleGetResourcesResponse(ProtocolEventArgs<GetResourcesResponse, string> args)
        {
        }
    }
}
