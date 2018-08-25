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

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Defines the supported list of version-qualified data object names.
    /// </summary>
    public static partial class ObjectNames
    {
        private static readonly string Version131 = OptionsIn.DataVersion.Version131.Value;
        private static readonly string Version141 = OptionsIn.DataVersion.Version141.Value;
        private static readonly string Version200 = OptionsIn.DataVersion.Version200.Value;

        /// <summary>
        /// The data object name for a 1.4.1.1 ChangeLog.
        /// </summary>
        public static readonly ObjectName ChangeLog141 = new ObjectName(ObjectTypes.ChangeLog, Version141);
    }
}
