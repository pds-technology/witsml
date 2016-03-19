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
    /// <summary>
    /// Process messages received for the Store role of the Discovery protocol.
    /// </summary>
    /// <seealso cref="Energistics.Protocol.Discovery.DiscoveryStoreHandler" />
    [Export(typeof(IDiscoveryStore))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DiscoveryStoreProvider : DiscoveryStoreHandler
    {
        /// <summary>
        /// Gets or sets the collection of providers implementing the <see cref="IDiscoveryStoreProvider"/> interface.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IDiscoveryStoreProvider> Providers { get; set; }

        /// <summary>
        /// Handles the GetResources message of the Discovery protocol.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetResources, IList{Resource}}"/> instance containing the event data.</param>
        protected override void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            foreach (var provider in Providers.OrderBy(x => x.DataSchemaVersion))
            {
                provider.GetResources(args);
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resource" /> using the specified parameters.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="resourceType">Type of the resource.</param>
        /// <param name="name">The name.</param>
        /// <param name="count">The count.</param>
        /// <returns>The resource instance.</returns>
        public static Resource New(string uuid, EtpUri uri, ResourceTypes resourceType, string name, int count = 0)
        {
            return new Resource()
            {
                Uuid = uuid,
                Uri = uri,
                Name = name,
                HasChildren = count,
                ContentType = uri.ContentType,
                ResourceType = resourceType.ToString(),
                CustomData = new Dictionary<string, string>(),
                LastChanged = 0 // TODO: provide LastChanged
            };
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resource" /> using the specified parameters.
        /// </summary>
        /// <param name="protocolUri">The protocol URI.</param>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns>The resource instance.</returns>
        public static Resource NewProtocol(EtpUri protocolUri, string folderName)
        {
            return New(
                uuid: Guid.NewGuid().ToString(),
                uri: protocolUri,
                resourceType: ResourceTypes.UriProtocol,
                name: folderName,
                count: -1);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resource" /> using the specified parameters.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns>The resource instance.</returns>
        public static Resource NewFolder(EtpUri parentUri, string objectType, string folderName)
        {
            var resource = New(
                uuid: Guid.NewGuid().ToString(),
                uri: parentUri.Append(folderName),
                resourceType: ResourceTypes.Folder,
                name: folderName,
                count: -1);

            resource.ContentType = new EtpContentType(resource.ContentType).For(objectType);

            return resource;
        }
    }
}
