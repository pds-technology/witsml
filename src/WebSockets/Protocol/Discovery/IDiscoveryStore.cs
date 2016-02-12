using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace Energistics.Protocol.Discovery
{
    public interface IDiscoveryStore : IProtocolHandler
    {
        void GetResourcesResponse(MessageHeader request, IList<Resource> resources);

        event ProtocolEventHandler<GetResources, IList<Resource>> OnGetResources;
    }
}
