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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Etp11 = Energistics.Etp.v11;
using Etp12 = Energistics.Etp.v12;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio.Store.Providers.Store
{
    /// <summary>
    /// Defines methods that can be used to perform CRUD operations via ETP for WITSML 2.0 objects.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Providers.Store.IStoreStoreProvider" />
    [Export200(typeof(IStoreStoreProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class StoreStore200Provider : IStoreStoreProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoreStore200Provider" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        [ImportingConstructor]
        public StoreStore200Provider(IContainer container)
        {
            Container = container;
        }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The container.</value>
        public IContainer Container { get; }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion => OptionsIn.DataVersion.Version200.Value;

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        public void GetObject(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp11.Protocol.Store.GetObject, Etp11.Datatypes.Object.DataObject> args)
        {
            GetObject(etpAdapter, args.Message.Uri, args.Context);
        }

        /// <summary>
        /// Gets the object details for the specified URIs.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetDataObjects}" /> instance containing the event data.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dataObject">The data object.</param>
        public void GetObject(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.Store.GetDataObjects> args, string uri, Etp12.Datatypes.Object.DataObject dataObject)
        {
            GetObject(etpAdapter, uri, dataObject);
        }

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        public void FindObjects(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.StoreQuery.FindObjects, Etp12.Protocol.StoreQuery.DataObjectResponse> args)
        {
            string serverSortOrder;
            FindObjects(etpAdapter, args.Message.Uri, args.Context.DataObjects, out serverSortOrder);
            args.Context.ServerSortOrder = serverSortOrder;
        }

        private void GetObject(IEtpAdapter etpAdapter, string uri, Energistics.Etp.Common.Datatypes.Object.IDataObject dataObject)
        {
            var etpUri = new EtpUri(uri);
            var dataAdapter = Container.Resolve<IEtpDataProvider>(new ObjectName(etpUri.ObjectType, etpUri.Family, etpUri.GetDataSchemaVersion()));
            var entity = dataAdapter.Get(etpUri) as Witsml200.AbstractObject;
            var lastChanged = (entity?.Citation.LastUpdate).ToUnixTimeMicroseconds().GetValueOrDefault();

            etpAdapter.SetDataObject(dataObject, entity, etpUri, GetName(entity), lastChanged: lastChanged);
        }

        private void FindObjects(IEtpAdapter etpAdapter, string uri, IList<Etp12.Datatypes.Object.DataObject> context, out string serverSortOrder)
        {
            var etpUri = new EtpUri(uri);
            var dataAdapter = Container.Resolve<IEtpDataProvider>(new ObjectName(etpUri.ObjectType, etpUri.Family, etpUri.GetDataSchemaVersion()));

            serverSortOrder = dataAdapter.ServerSortOrder;

            foreach (var result in dataAdapter.GetAll(etpUri))
            {
                var entity = result as Witsml200.AbstractObject;
                var lastChanged = (entity?.Citation.LastUpdate).ToUnixTimeMicroseconds().GetValueOrDefault();
                var dataObject = new Etp12.Datatypes.Object.DataObject();

                etpAdapter.SetDataObject(dataObject, entity, etpUri, GetName(entity), lastChanged: lastChanged);
                context.Add(dataObject);
            }
        }

        private string GetName(Witsml200.AbstractObject entity)
        {
            return entity == null ? string.Empty : entity.Citation.Title;
        }
    }
}
