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

using System.ComponentModel;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Enumeration of WITSML endpoint types.
    /// </summary>
    public enum WitsmlEndpointTypes
    {
        /// <summary>
        /// The SOAP endpoint type.
        /// </summary>
        [Description("SOAP")]
        Soap,

        /// <summary>
        /// The ETP endpoint type.
        /// </summary>
        [Description("ETP")]
        Etp
    }
}
