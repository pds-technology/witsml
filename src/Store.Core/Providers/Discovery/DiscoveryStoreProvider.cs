//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Discovery;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Providers.Discovery
{
    /// <summary>
    /// Process messages received for the Store role of the Discovery protocol.
    /// </summary>
    /// <seealso cref="Energistics.Protocol.Discovery.DiscoveryStoreHandler" />
    [Export(typeof(IDiscoveryStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DiscoveryStoreProvider : DiscoveryStoreHandler
    {
        /// <summary>
        /// Gets or sets the collection of providers implementing the <see cref="IDiscoveryStoreProvider"/> interface.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IDiscoveryStoreProvider> Providers { get; set; }

        /// <summary>
        /// Gets the capabilities supported by the protocol handler.
        /// </summary>
        /// <returns>A collection of protocol capabilities.</returns>
        public override IDictionary<string, DataValue> GetCapabilities()
        {
            var capabilities = base.GetCapabilities();

            capabilities[MaxGetResourcesResponse] = new DataValue
            {
                Item = WitsmlSettings.MaxGetResourcesResponse
            };

            return capabilities;
        }

        /// <summary>
        /// Handles the GetResources message of the Discovery protocol.
        /// </summary>
        /// <param name="args">The ProtocolEventArgs{GetResources, IList{Resource}} instance containing the event data.</param>
        protected override void HandleGetResources(ProtocolEventArgs<GetResources, IList<Resource>> args)
        {
            if (!EtpUri.IsRoot(args.Message.Uri))
            {
                var uri = this.CreateAndValidateUri(args.Message.Uri, args.Header.MessageId);

                if (!uri.IsValid)
                {
                    args.Cancel = true;
                    return;
                }
            }

            var max = WitsmlSettings.MaxGetResourcesResponse;

            foreach (var provider in Providers.OrderBy(x => x.DataSchemaVersion))
            {
                // TODO: Optimize inside each version specific provider
                if (args.Context.Count >= max) break;

                provider.GetResources(args);
            }

            // Limit max number of GetResourcesResponse returned to customer
            while (args.Context.Count > max)
                args.Context.RemoveAt(args.Context.Count - 1);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resource" /> using the specified parameters.
        /// </summary>
        /// <param name="uuid">The UUID.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="resourceType">Type of the resource.</param>
        /// <param name="name">The name.</param>
        /// <param name="count">The count.</param>
        /// <param name="lastChanged">The last changed in microseconds.</param>
        /// <returns>The resource instance.</returns>
        public static Resource New(string uuid, EtpUri uri, ResourceTypes resourceType, string name, int count = 0, long lastChanged = 0)
        {
            return new Resource
            {
                Uuid = uuid,
                Uri = uri,
                Name = name,
                HasChildren = count,
                ContentType = uri.ContentType,
                ResourceType = resourceType.ToString(),
                CustomData = new Dictionary<string, string>(),
                LastChanged = lastChanged,
                ChannelSubscribable = uri.IsChannelSubscribable(),
                ObjectNotifiable = uri.IsObjectNotifiable()
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
        /// <param name="hasChildren">The child count.</param>
        /// <returns>A new <see cref="Resource"/> instance.</returns>
        public static Resource NewFolder(EtpUri parentUri, string objectType, string folderName, int hasChildren = -1)
        {
            var resource = New(
                uuid: Guid.NewGuid().ToString(),
                uri: parentUri.Append(folderName),
                resourceType: ResourceTypes.Folder,
                name: folderName,
                count: hasChildren);

            resource.ContentType = new EtpContentType(resource.ContentType).For(objectType);

            return resource;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resource" /> using the specified parameters.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="folderName">The name of the folder.</param>
        /// <param name="hasChildren">The child count.</param>
        /// <returns>A new <see cref="Resource"/> instance.</returns>
        public static Resource NewDecoratorFolder(EtpUri uri, string folderName, int hasChildren = -1)
        {
            return New(
                uuid: Guid.NewGuid().ToString(),
                uri: uri,
                resourceType: ResourceTypes.DecoratorFolder,
                name: folderName,
                count: hasChildren);
        }
    }
}
