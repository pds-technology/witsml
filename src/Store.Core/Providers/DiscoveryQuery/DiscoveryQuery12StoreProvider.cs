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
using Energistics.Etp.v12.Protocol.DiscoveryQuery;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Providers.Discovery;

namespace PDS.WITSMLstudio.Store.Providers.DiscoveryQuery
{
    /// <summary>
    /// Process messages received for the Store role of the DiscoveryQuery protocol.
    /// </summary>
    /// <seealso cref="Energistics.Etp.v12.Protocol.DiscoveryQuery.DiscoveryQueryStoreHandler" />
    [Export(typeof(IDiscoveryQueryStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DiscoveryQuery12StoreProvider : DiscoveryQueryStoreHandler
    {
        /// <summary>
        /// Gets or sets the collection of providers implementing the <see cref="IDiscoveryStoreProvider"/> interface.
        /// </summary>
        /// <value>The collection of providers.</value>
        [ImportMany]
        public IEnumerable<IDiscoveryStoreProvider> Providers { get; set; }

        /// <summary>
        /// Handles the FindResources message of the DiscoveryQuery protocol.
        /// </summary>
        /// <param name="args">The ProtocolEventArgs{FindResources, IList{Resource}} instance containing the event data.</param>
        protected override void HandleFindResources(ProtocolEventArgs<FindResources, ResourceResponse> args)
        {
            if (!EtpUris.IsRootUri(args.Message.Uri))
            {
                var uri = this.CreateAndValidateUri(args.Message.Uri, args.Header.MessageId);

                if (!uri.IsValid)
                {
                    args.Cancel = true;
                    return;
                }
            }

            var max = WitsmlSettings.MaxGetResourcesResponse;
            var resources = args.Context.Resources;

            foreach (var provider in Providers.OrderBy(x => x.DataSchemaVersion))
            {
                // TODO: Optimize inside each version specific provider
                if (resources.Count >= max) break;

                try
                {
                    provider.FindResources(Session.Adapter, args);
                }
                catch (ContainerException ex)
                {
                    this.UnsupportedObject(ex, args.Message.Uri, args.Header.MessageId);
                    return;
                }
            }

            // Limit max number of FindResourcesResponse returned to customer
            while (resources.Count > max)
                resources.RemoveAt(resources.Count - 1);

            // Check for empty query results
            if (!resources.Any())
            {
                Acknowledge(args.Header.MessageId, MessageFlags.NoData);
                args.Cancel = true;
            }
        }
    }
}
