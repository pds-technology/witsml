//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.1
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
        // SOAP
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

        // ETP
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
        /// Growing Object Get
        /// </summary>
        [Description("Growing Object Get")]
        GrowingObjectGet,

        /// <summary>
        /// Growing Object Get Range
        /// </summary>
        [Description("Growing Object Get Range")]
        GrowingObjectGetRange,

        /// <summary>
        /// Growing Object Put
        /// </summary>
        [Description("Growing Object Put")]
        GrowingObjectPut,

        /// <summary>
        /// Growing Object Delete
        /// </summary>
        [Description("Growing Object Delete")]
        GrowingObjectDelete,

        /// <summary>
        /// Growing Object Delete Range
        /// </summary>
        [Description("Growing Object Delete Range")]
        GrowingObjectDeleteRange
    }
}
