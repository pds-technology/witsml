//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Encapsulates the composition container configuration settings.
    /// </summary>
    public class ContainerConfig
    {
        /// <summary>
        /// Gets or sets the list of excluded assemblies.
        /// </summary>
        public List<string> ExcludedAssemblies { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the list of types to include from the excluded assemblies.
        /// </summary>
        public List<string> IncludedTypes { get; set; } = new List<string>();
    }
}
