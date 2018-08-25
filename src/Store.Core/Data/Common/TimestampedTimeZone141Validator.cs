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
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data.Common
{
    /// <summary>
    /// Performs validtion on recurring elements of type TimestampedTimeZone from the WITSML 1.4.1.1 schema.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.Common.IRecurringElementValidator" />
    [Export141("TimestampedTimeZone", typeof(IRecurringElementValidator))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TimestampedTimeZone141Validator : DataObjectValidator<TimestampedTimeZone>, IRecurringElementValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampedTimeZone141Validator"/> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        [ImportingConstructor]
        public TimestampedTimeZone141Validator(IContainer container) : base(container)
        {
        }

        /// <summary>
        /// Validates the elementList of a specified child type for the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="childType">Type of the child elements.</param>
        /// <param name="currentItems">The current items.</param>
        /// <param name="elementList">The list of all child elements being validated.</param>
        public void Validate(Functions function, Type childType, IEnumerable currentItems, IList<XElement> elementList)
        {
            if (function != Functions.AddToStore && function != Functions.UpdateInStore && function != Functions.PutObject)
                return;

            var hasItems = (currentItems as IEnumerable<TimestampedTimeZone>)?.Any() ?? false;

            for (var i = 0; i < elementList.Count; i++)
            {
                // We don't need to check the first one if there are no current items
                if (i == 0 && !hasItems)
                    continue;

                var newTimestampTimeZone = ParseNestedElement(childType, elementList[i]) as TimestampedTimeZone;

                if (newTimestampTimeZone != null && !newTimestampTimeZone.DateTimeSpecified)
                    throw new WitsmlException(function.GetNonConformingErrorCode(), 
                        "The dTim attribute must be populated in the second and subsequent occurrences if the local time zone changes during acquisition.");
            }
        }
    }
}
