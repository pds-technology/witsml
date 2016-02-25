using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;

namespace PDS.Witsml.Server.Providers.Discovery
{
    /// <summary>
    /// Defines properties and methods that can be used to discover resources available in a WITSML store.
    /// </summary>
    public interface IDiscoveryStoreProvider
    {
        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        string DataSchemaVersion { get; }

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList{Resource}}"/> instance containing the event data.</param>
        void GetResources(ProtocolEventArgs<GetResources, IList<Resource>> args);
    }
}
