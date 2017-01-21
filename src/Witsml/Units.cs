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

using Energistics.DataAccess.WITSML200.ReferenceData;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Define constants related to UOM library
    /// </summary>
    public static class Units
    {
        /// <summary>
        /// A dimensionless unit; default value when unit is null
        /// </summary>
        public const string None = "none";

        /// <summary>
        /// Gets the unit.
        /// </summary>
        /// <param name="uom">The uom.</param>
        /// <returns>The original uom if not null or empty; otherwise, a value of "none"</returns>
        public static string GetUnit(string uom)
        {
            return !string.IsNullOrWhiteSpace(uom) ? uom : None;
        }

        /// <summary>
        /// Gets the unit.
        /// </summary>
        /// <param name="uom">The uom.</param>
        /// <returns>The unit of measure enumeration name.</returns>
        public static string GetUnit(UnitOfMeasure uom)
        {
            return uom.GetName();
        }

        /// <summary>
        /// Gets the unit.
        /// </summary>
        /// <param name="uom">The uom.</param>
        /// <returns>The unit of measure enumeration name if not null; otherwise, a value of "none"</returns>
        public static string GetUnit(UnitOfMeasure? uom)
        {
            return uom.HasValue ? GetUnit(uom.Value) : None;
        }

        /// <summary>
        /// Gets the enum UnitOfMeasure from a string.
        /// </summary>
        /// <param name="uom">The uom.</param>
        /// <returns>The UnitOfMeasure if not null or empty; value null if otherwise</returns>
        public static UnitOfMeasure? GetUnitOfMeasure(string uom)
        {
            return (UnitOfMeasure?) typeof(UnitOfMeasure).ParseEnum(uom);
        }
    }
}
