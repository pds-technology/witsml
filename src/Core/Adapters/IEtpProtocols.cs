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

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Defines common properties and methods related to ETP protocols.
    /// </summary>
    public interface IEtpProtocols
    {
        /// <summary>
        /// Gets the Core protocol identifier.
        /// </summary>
        int Core { get; }

        /// <summary>
        /// Gets the ChannelStreaming protocol identifier.
        /// </summary>
        int ChannelStreaming { get; }

        /// <summary>
        /// Gets the ChannelDataFrame protocol identifier.
        /// </summary>
        int ChannelDataFrame { get; }

        /// <summary>
        /// Gets the ChannelSubscribe protocol identifier.
        /// </summary>
        int ChannelSubscribe { get; }

        /// <summary>
        /// Gets the ChannelDataLoad protocol identifier.
        /// </summary>
        int ChannelDataLoad { get; }

        /// <summary>
        /// Gets the Discovery protocol identifier.
        /// </summary>
        int Discovery { get; }

        /// <summary>
        /// Gets the DiscoveryQuery protocol identifier.
        /// </summary>
        int DiscoveryQuery { get; }

        /// <summary>
        /// Gets the Store protocol identifier.
        /// </summary>
        int Store { get; }

        /// <summary>
        /// Gets the StoreNotification protocol identifier.
        /// </summary>
        int StoreNotification { get; }

        /// <summary>
        /// Gets the StoreQuery protocol identifier.
        /// </summary>
        int StoreQuery { get; }

        /// <summary>
        /// Gets the GrowingObject protocol identifier.
        /// </summary>
        int GrowingObject { get; }

        /// <summary>
        /// Gets the GrowingObjectNotification protocol identifier.
        /// </summary>
        int GrowingObjectNotification { get; }

        /// <summary>
        /// Gets the GrowingObjectQuery protocol identifier.
        /// </summary>
        int GrowingObjectQuery { get; }

        /// <summary>
        /// Gets the DataArray protocol identifier.
        /// </summary>
        int DataArray { get; }

        /// <summary>
        /// Gets the WitsmlSoap protocol identifier.
        /// </summary>
        int WitsmlSoap { get; }
    }
}