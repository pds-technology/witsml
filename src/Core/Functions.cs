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

using System.ComponentModel;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Enumeration of WITSML API methods.
    /// </summary>
    public enum Functions
    {
        #region SOAP
        /// <summary>
        /// Get Base Message
        /// </summary>
        [Description("Get Base Message")]
        GetBaseMsg,

        /// <summary>
        /// Get Capabilities
        /// </summary>
        [Description("Get Capabilities")]
        GetCap,

        /// <summary>
        /// Get Version
        /// </summary>
        [Description("Get Version")]
        GetVersion,

        /// <summary>
        /// Get From Store
        /// </summary>
        [Description("Get From Store")]
        GetFromStore,

        /// <summary>
        /// Add to Store
        /// </summary>
        [Description("Add To Store")]
        AddToStore,

        /// <summary>
        /// Update in Store
        /// </summary>
        [Description("Update In Store")]
        UpdateInStore,

        /// <summary>
        /// Delete From Store
        /// </summary>
        [Description("Delete From Store")]
        DeleteFromStore,
        #endregion

        #region ETP
        /// <summary>
        /// Get Object
        /// </summary>
        [Description("Get Object")]
        GetObject,

        /// <summary>
        /// Put Object
        /// </summary>
        [Description("Put Object")]
        PutObject,

        /// <summary>
        /// Delete Object
        /// </summary>
        [Description("Delete Object")]
        DeleteObject,

        /// <summary>
        /// Get Part
        /// </summary>
        [Description("Get Part")]
        GetPart,

        /// <summary>
        /// Get Parts by Range
        /// </summary>
        [Description("Get Parts by Range")]
        GetPartsByRange,

        /// <summary>
        /// Put Part
        /// </summary>
        [Description("Put Part")]
        PutPart,

        /// <summary>
        /// Delete Part
        /// </summary>
        [Description("Delete Part")]
        DeletePart,

        /// <summary>
        /// Delete Parts by Range
        /// </summary>
        [Description("Delete Parts by Range")]
        DeletePartsByRange,

        /// <summary>
        /// Find Parts
        /// </summary>
        [Description("Find Parts")]
        FindParts,

        /// <summary>
        /// Find Objects
        /// </summary>
        [Description("Find Objects")]
        FindObjects,

        /// <summary>
        /// Get Resources
        /// </summary>
        [Description("Get Resources")]
        GetResources,

        /// <summary>
        /// Find Reources
        /// </summary>
        [Description("Find Resources")]
        FindResources,

        /// <summary>
        /// Get Tree Resources
        /// </summary>
        [Description("Get Tree Resources")]
        GetTreeResources,

        /// <summary>
        /// Get Graph Resources
        /// </summary>
        [Description("Get Graph Resources")]
        GetGraphResources
        #endregion
    }
}
