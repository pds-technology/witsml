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
using Energistics.Etp.Common;
using Etp11 = Energistics.Etp.v11;
using Etp12 = Energistics.Etp.v12;

namespace PDS.WITSMLstudio.Store.Providers.Discovery
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
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The ProtocolEventArgs{GetResources, IList{Resource}} instance containing the event data.</param>
        void GetResources(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp11.Protocol.Discovery.GetResources, IList<Etp11.Datatypes.Object.Resource>> args);

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The ProtocolEventArgs{GetTreeResources, IList{Resource}} instance containing the event data.</param>
        void GetResources(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.Discovery.GetTreeResources, IList<Etp12.Datatypes.Object.Resource>> args);

        /// <summary>
        /// Gets a collection of resources associated to the specified URI.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="args">The ProtocolEventArgs{FindResources, IList{Resource}} instance containing the event data.</param>
        void FindResources(IEtpAdapter etpAdapter, ProtocolEventArgs<Etp12.Protocol.DiscoveryQuery.FindResources, Etp12.Protocol.DiscoveryQuery.ResourceResponse> args);
    }
}
