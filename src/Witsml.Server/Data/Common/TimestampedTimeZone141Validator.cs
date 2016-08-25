//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML141.ComponentSchemas;

namespace PDS.Witsml.Server.Data.Common
{
    /// <summary>
    /// Performs validtion on recurring elements of type TimestampedTimeZone from the WITSML141 schema.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.Common.IRecurringElementValidator" />
    [Export141("TimestampedTimeZone", typeof(IRecurringElementValidator))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class TimestampedTimeZone141Validator : DataObjectValidator<TimestampedTimeZone>, IRecurringElementValidator
    {
        /// <summary>
        /// Validates the elementList of a specified childType for the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="childType">Type of the child elements.</param>
        /// <param name="currentItems">The current items.</param>
        /// <param name="elementList">The list of all child elements being validated.</param>
        /// <exception cref="WitsmlException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Validate(Functions function, Type childType, IEnumerable currentItems, List<XElement> elementList)
        {
            if (function != Functions.UpdateInStore)
                return;

            var itemCount = (currentItems as IEnumerable<TimestampedTimeZone>)?.Count();
            var hasItems = (itemCount != null && itemCount > 0);

            for (var i = 0; i < elementList.Count; i++)
            {
                // We don't need to check the first one if there are no current items
                if (i == 0 && !hasItems)
                    continue;

                var newTimestampTimeZone = ParseNestedElement(childType, elementList[i]) as TimestampedTimeZone;

                if (!newTimestampTimeZone.DateTimeSpecified)
                    throw new WitsmlException(function.GetNonConformingErrorCode(), 
                        "The dTim attribute must be populated in the second and subsequent occurrences if the local time zone changes during acquisition.");

            }
        }
    }
}
