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
using System.ComponentModel.Composition;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data.Common
{
    /// <summary>
    /// Performs validtion on recurring elements of type AbstractActivityParameter from the Common 2.1 schema.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.DataObjectValidator{AbstractActivityParameter}" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.Common.IRecurringElementValidator" />
    [Export200("AbstractActivityParameter", typeof(IRecurringElementValidator))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AbstractActivityParameter200Validator : DataObjectValidator<AbstractActivityParameter>, IRecurringElementValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractActivityParameter200Validator"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        [ImportingConstructor]
        public AbstractActivityParameter200Validator(IContainer container) : base(container)
        {
        }

        /// <summary>
        /// Validates the elementList of a specified childType for the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="childType">Type of the child elements.</param>
        /// <param name="currentItems">The current items.</param>
        /// <param name="elementList">The list of all child elements being validated.</param>
        public void Validate(Functions function, Type childType, IEnumerable currentItems, IList<XElement> elementList)
        {
        }
    }
}
