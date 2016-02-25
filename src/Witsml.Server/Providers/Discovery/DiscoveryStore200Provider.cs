using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;

namespace PDS.Witsml.Server.Providers.Discovery
{
    /// <summary>
    /// Provides information about resources available in a WITSML store for version 1.4.1.1.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Providers.Discovery.IDiscoveryStoreProvider" />
    [Export(typeof(IDiscoveryStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStore200Provider : IDiscoveryStoreProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryStore200Provider"/> class.
        /// </summary>
        public DiscoveryStore200Provider()
        {
        }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList{Resource}}"/> instance containing the event data.</param>
        public void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (DiscoveryStoreProvider.RootUri.Equals(args.Message.Uri))
            {
                args.Context.Add(
                    DiscoveryStoreProvider.New(
                        uri: UriFormats.Witsml200.Root,
                        contentType: ContentTypes.Witsml200,
                        resourceType: ResourceTypes.UriProtocol,
                        name: "WITSML Store (2.0)",
                        count: -1));
            }
        }
    }
}
