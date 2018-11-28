//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using Energistics.Etp.v11;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Provides metadata for ETP 1.1 protocols.
    /// </summary>
    /// <seealso cref="IEtpProtocols" />
    public class Etp11Protocols : IEtpProtocols
    {
        /// <summary>
        /// Gets the Core protocol identifier.
        /// </summary>
        public int Core => (int) Protocols.Core;

        /// <summary>
        /// Gets the ChannelStreaming protocol identifier.
        /// </summary>
        public int ChannelStreaming => (int) Protocols.ChannelStreaming;

        /// <summary>
        /// Gets the ChannelDataFrame protocol identifier.
        /// </summary>
        public int ChannelDataFrame => (int) Protocols.ChannelDataFrame;

        /// <summary>
        /// Gets the ChannelSubscribe protocol identifier.
        /// </summary>
        public int ChannelSubscribe => -1;

        /// <summary>
        /// Gets the ChannelDataLoad protocol identifier.
        /// </summary>
        public int ChannelDataLoad => -1;

        /// <summary>
        /// Gets the Discovery protocol identifier.
        /// </summary>
        public int Discovery => (int) Protocols.Discovery;

        /// <summary>
        /// Gets the DiscoveryQuery protocol identifier.
        /// </summary>
        public int DiscoveryQuery => -1;

        /// <summary>
        /// Gets the Store protocol identifier.
        /// </summary>
        public int Store => (int) Protocols.Store;

        /// <summary>
        /// Gets the StoreNotification protocol identifier.
        /// </summary>
        public int StoreNotification => (int) Protocols.StoreNotification;

        /// <summary>
        /// Gets the StoreQuery protocol identifier.
        /// </summary>
        public int StoreQuery => -1;

        /// <summary>
        /// Gets the GrowingObject protocol identifier.
        /// </summary>
        public int GrowingObject => (int) Protocols.GrowingObject;

        /// <summary>
        /// Gets the GrowingObjectNotification protocol identifier.
        /// </summary>
        public int GrowingObjectNotification => -1;

        /// <summary>
        /// Gets the GrowingObjectQuery protocol identifier.
        /// </summary>
        public int GrowingObjectQuery => -1;

        /// <summary>
        /// Gets the DataArray protocol identifier.
        /// </summary>
        public int DataArray => (int) Protocols.DataArray;

        /// <summary>
        /// Gets the WitsmlSoap protocol identifier.
        /// </summary>
        public int WitsmlSoap => (int) Protocols.WitsmlSoap;
    }
}
