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

using Energistics.Etp.Common;
using Etp11 = Energistics.Etp.v11;
using Etp12 = Energistics.Etp.v12;

namespace PDS.WITSMLstudio.Store.Providers.Store
{
    /// <summary>
    /// Defines methods that can be used to perform CRUD operations via ETP.
    /// </summary>
    public interface IStoreStoreProvider
    {
        /// <summary>
        /// Gets the data schema version supported by the provider.
        /// </summary>
        /// <value>The data schema version.</value>
        string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetObject, DataObject}" /> instance containing the event data.</param>
        void GetObject(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp11.Protocol.Store.GetObject, Etp11.Datatypes.Object.DataObject> args);

        /// <summary>
        /// Gets the object details for the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{GetDataObjects}" /> instance containing the event data.</param>
        /// <param name="uri">The data object URI.</param>
        /// <param name="dataObject">The data object.</param>
        void GetObject(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.Store.GetDataObjects> args, string uri, Etp12.Datatypes.Object.DataObject dataObject);

        /// <summary>
        /// Gets the object details for the specified URIs.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The <see cref="ProtocolEventArgs{FindObjects, DataObject}"/> instance containing the event data.</param>
        void FindObjects(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.StoreQuery.FindObjects, Etp12.Protocol.StoreQuery.DataObjectResponse> args);
    }
}
