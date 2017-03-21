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

using System.ComponentModel.Composition;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Store;
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
        public IContainer Container { get; private set; }

        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        public string DataSchemaVersion
        {
            get { return OptionsIn.DataVersion.Version200.Value; }
        }

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void GetObject(ProtocolEventArgs<GetObject, DataObject> args)
        {
            var uri = new EtpUri(args.Message.Uri);
            var dataAdapter = Container.Resolve<IEtpDataProvider>(new ObjectName(uri.ObjectType, uri.GetDataSchemaVersion()));
            var entity = dataAdapter.Get(uri) as Witsml200.AbstractObject;
            var lastChanged = (entity?.Citation.LastUpdate).ToUnixTimeMicroseconds().GetValueOrDefault();

            StoreStoreProvider.SetDataObject(args.Context, entity, uri, GetName(entity), lastChanged: lastChanged);
        }

        private string GetName(Witsml200.AbstractObject entity)
        {
            return entity == null ? string.Empty : entity.Citation.Title;
        }
    }
}
