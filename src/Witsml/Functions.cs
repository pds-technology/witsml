//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.ComponentModel;

namespace PDS.Witsml
{
    /// <summary>
    /// Enumeration of WITSML API methods.
    /// </summary>
    public enum Functions
    {
        // SOAP
        [Description("Get Capabilities")]
        GetCap,
        [Description("Get Version")]
        GetVersion,
        [Description("Get From Store")]
        GetFromStore,
        [Description("Add To Store")]
        AddToStore,
        [Description("Update In Store")]
        UpdateInStore,
        [Description("Delete From Store")]
        DeleteFromStore,

        // ETP
        [Description("Get Object")]
        GetObject,
        [Description("Put Object")]
        PutObject,
        [Description("Delete Object")]
        DeleteObject
    }
}
