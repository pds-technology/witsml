using System.Collections.Generic;
using System.Linq;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Discovery
{
    public class DiscoveryStoreHandler : EtpProtocolHandler, IDiscoveryStore
    {
        public DiscoveryStoreHandler() : base(Protocols.Discovery, "store")
        {
        }

        public virtual void GetResourcesResponse(MessageHeader request, IList<Resource> resources)
        {
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

        public event ProtocolEventHandler<GetResources, IList<Resource>> OnGetResources;

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

        protected virtual void HandleGetResources(MessageHeader header, GetResources getResources)
        {
            var args = Notify(OnGetResources, header, getResources, new List<Resource>());
            HandleGetResources(args);

            GetResourcesResponse(header, args.Context);
        }

        protected virtual void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
        }
    }
}
