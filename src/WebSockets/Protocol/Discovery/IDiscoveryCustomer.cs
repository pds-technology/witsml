using Energistics.Common;

namespace Energistics.Protocol.Discovery
{
    public interface IDiscoveryCustomer : IProtocolHandler
    {
        void GetResources(string uri);

        event ProtocolEventHandler<GetResourcesResponse, string> OnGetResourcesResponse;
    }
}
