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

using System;
using System.Collections.Generic;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;

namespace PDS.Witsml.Data
{
    /// <summary>
    /// Provides methods to generate data.
    /// </summary>
    public class DataGenerator
    {
        /// <summary>
        /// The format applied to timestamps
        /// </summary>
        public readonly string TimestampFormat = "yyMMdd-HHmmss-fff";

        /// <summary>
        /// Creates a <see cref="Guid"/>.
        /// </summary>
        /// <returns>The Guid in string</returns>
        public string Uid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a name with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The name.</returns>
        public string Name(string prefix = null)
        {
            if (String.IsNullOrWhiteSpace(prefix))
                return DateTime.Now.ToString(TimestampFormat);

            return String.Format("{0} - {1}", prefix, DateTime.Now.ToString(TimestampFormat));
        }

        /// <summary>
        /// Lists the specified instances.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        public List<T> List<T>(params T[] instances)
        {
            return new List<T>(instances);
        }

        /// <summary>
        /// Converts the specified curve class string to a <see cref="PropertyKind"/> instance.
        /// </summary>
        /// <param name="curveClass">The curve class.</param>
        /// <returns>A new <see cref="PropertyKind"/> instance.</returns>
        public PropertyKind ToPropertyKind(string curveClass)
        {
            QuantityClassKind quantityClass;

            if (!Enum.TryParse(curveClass, out quantityClass))
                quantityClass = QuantityClassKind.dimensionless;

            return new PropertyKind
            {
                QuantityClass = quantityClass
            };
        }
    }
}
