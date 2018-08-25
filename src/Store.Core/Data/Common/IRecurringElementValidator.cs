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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace PDS.WITSMLstudio.Store.Data.Common
{
    /// <summary>
    /// Defines validation method for recurring elements without a uid in the schema.
    /// </summary>
    public interface IRecurringElementValidator
    {
        /// <summary>
        /// Validates the elementList of a specified childType for the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="childType">Type of the child elements.</param>
        /// <param name="currentItems">The current items.</param>
        /// <param name="elementList">The list of all child elements being validated.</param>
        void Validate(Functions function, Type childType, IEnumerable currentItems, IList<XElement> elementList);
    }
}
