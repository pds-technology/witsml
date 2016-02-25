using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;

namespace PDS.Witsml.Server.Providers.Discovery
{
    [Export(typeof(IDiscoveryStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStoreProvider : DiscoveryStoreHandler
    {
        public const string RootUri = "/";

        [ImportMany]
        public IEnumerable<IDiscoveryStoreProvider> Providers { get; set; }

        protected override void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            foreach (var provider in Providers.OrderBy(x => x.DataSchemaVersion))
            {
                provider.GetResources(args);
            }
        }

        public static Resource New(string uri, ResourceTypes resourceType, string contentType, string name, int count = 0)
        {
            return new Resource()
            {
                Uuid = Guid.NewGuid().ToString(),
                Uri = uri,
                Name = name,
                HasChildren = count,
                ContentType = contentType,
                ResourceType = resourceType.ToString(),
                CustomData = new Dictionary<string, string>(),
                LastChanged = new Energistics.Datatypes.DateTime()
                {
                    Offset = 0,
                    Time = 0
                }
            };
        }
    }
}
