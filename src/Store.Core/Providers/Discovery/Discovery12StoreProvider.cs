//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.v12.Datatypes.Object;
using Energistics.Etp.v12.Protocol.Discovery;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Providers.Discovery
{
    /// <summary>
    /// Process messages received for the Store role of the Discovery protocol.
    /// </summary>
    /// <seealso cref="Energistics.Etp.v12.Protocol.Discovery.DiscoveryStoreHandler" />
    [Export(typeof(IDiscoveryStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class Discovery12StoreProvider : DiscoveryStoreHandler
    {
        /// <summary>
        /// Gets or sets the collection of providers implementing the <see cref="IDiscoveryStoreProvider"/> interface.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IDiscoveryStoreProvider> Providers { get; set; }

        /// <summary>
        /// Handles the GetTreeResources message of the Discovery protocol.
        /// </summary>
        /// <param name="args">The ProtocolEventArgs{GetTreeResources, IList{Resource}} instance containing the event data.</param>
        protected override void HandleGetTreeResources(ProtocolEventArgs<GetTreeResources, IList<Resource>> args)
        {
            if (!EtpUris.IsRootUri(args.Message.Context.Uri))
            {
                var uri = this.CreateAndValidateUri(args.Message.Context.Uri, args.Header.MessageId);

                if (!uri.IsValid)
                {
                    args.Cancel = true;
                    return;
                }

                if (!this.ValidateUriParentHierarchy(uri, args.Header.MessageId))
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

                try
                {
                    provider.GetResources(Session.Adapter, args);
                }
                catch (ContainerException ex)
                {
                    this.UnsupportedObject(ex, args.Message.Context.Uri, args.Header.MessageId);
                    return;
                }
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
                Uuid = uuid ?? string.Empty,
                Uri = uri,
                Name = name,
                TargetCount = count,
                ContentType = uri.ContentType,
                ResourceType = (ResourceKind)(int)resourceType,
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
        /// <param name="count">Elements count</param>
        /// <returns>The resource instance.</returns>
        public static Resource NewProtocol(EtpUri protocolUri, string folderName, int count = -1)
        {
            return New(
                uuid: string.Empty,
                uri: protocolUri,
                resourceType: ResourceTypes.UriProtocol,
                name: folderName,
                count: count);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Resource" /> using the specified parameters.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="folderName">The folder name.</param>
        /// <param name="hasChildren">The child count.</param>
        /// <param name="appendFolderName">if set to <c>true</c> append folder name.</param>
        /// <returns>A new <see cref="Resource"/> instance.</returns>
        public static Resource NewFolder(EtpUri parentUri, EtpContentType contentType, string folderName, int hasChildren = -1, bool appendFolderName = false)
        {
            var folderUri = parentUri;

            if (!parentUri.ObjectType.EqualsIgnoreCase(contentType.ObjectType))
                folderUri = folderUri.Append(contentType.ObjectType);

            if (appendFolderName)
                folderUri = folderUri.Append(folderName);

            var resource = New(
                uuid: string.Empty,
                uri: folderUri,
                resourceType: ResourceTypes.Folder,
                name: folderName,
                count: hasChildren);

            resource.ContentType = contentType;

            return resource;
        }
    }
}
