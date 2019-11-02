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

using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.v12.Protocol.StoreQuery;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Providers.Store;

namespace PDS.WITSMLstudio.Store.Providers.StoreQuery
{
    /// <summary>
    /// Process messages received for the Store role of the StoreQuery protocol.
    /// </summary>
    [Export(typeof(IStoreQueryStore))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class StoreQuery12StoreProvider : StoreQueryStoreHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreQuery12StoreProvider"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        [ImportingConstructor]
        public StoreQuery12StoreProvider(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; }

        /// <summary>
        /// Handles the FindObjects message of the Store protocol.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{FindObjects, DataObject}"/> instance containing the event data.</param>
        protected override void HandleFindObjects(ProtocolEventArgs<FindObjects, DataObjectResponse> args)
        {
            try
            {
                var uri = this.CreateAndValidateUri(args.Message.Uri, args.Header.MessageId);
                if (!uri.IsValid)
                {
                    args.Cancel = true;
                    return;
                }

                if (!this.ValidateUriObjectType(uri, args.Header.MessageId))
                {
                    args.Cancel = true;
                    return;
                }

                WitsmlOperationContext.Current.Request = new RequestContext(Functions.GetObject, uri.ObjectType, null, null, null);

                var provider = Container.Resolve<IStoreStoreProvider>(new ObjectName(uri.GetDataSchemaVersion()));
                provider.FindObjects(Session.Adapter, args);

                // Check for empty query results
                if (!(args.Context?.DataObjects.Any()).GetValueOrDefault())
                {
                    Acknowledge(args.Header.MessageId, MessageFlags.NoData);
                    args.Cancel = true;
                }
            }
            catch (ContainerException ex)
            {
                this.UnsupportedObject(ex, args.Message.Uri, args.Header.MessageId);
                args.Cancel = true;
            }
        }
    }
}
