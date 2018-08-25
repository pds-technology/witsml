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

namespace PDS.WITSMLstudio.Compatibility
{
    /// <summary>
    /// Enumeration of settings to control how unknown elements will be handled.
    /// </summary>
    public enum UnknownElementSetting
    {
        /// <summary>
        /// When an unknown element is encountered, the following error codes will be returned:
        /// Get: -469;
        /// Add: -409;
        /// Update: -483;
        /// Delete: -469;
        /// </summary>
        Error,

        /// <summary>
        /// When an unknown element is encountered, the following return codes will be used:
        /// Success: 1001;
        /// Partial Success: 1002;
        /// </summary>
        Warn,

        /// <summary>
        /// When an unknown element is encountered, it will be silently ignored.
        /// </summary>
        Ignore
    }
}
