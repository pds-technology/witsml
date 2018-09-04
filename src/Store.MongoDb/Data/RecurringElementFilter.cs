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
using System.Collections.Generic;
using System.Linq;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Encapsulates common properties needed for defining recurring element filters.
    /// </summary>
    public class RecurringElementFilter
    {
        private readonly string _expression;

        /// <summary>The $self property identifier.</summary>
        public const string Self = "$self";

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringElementFilter" /> class.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        public RecurringElementFilter(string propertyPath)
        {
            PropertyPath = propertyPath;
            Filters = new List<RecurringElementFilter>();
            Predicate = (dataObject, instance, filter) => Filters.All(x => x.Predicate(dataObject, instance, x));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringElementFilter" /> class.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="predicate">The predicate.</param>
        public RecurringElementFilter(string propertyPath, string expression, Func<object, object, RecurringElementFilter, bool> predicate)
        {
            PropertyPath = propertyPath;
            Predicate = predicate;
            Filters = new List<RecurringElementFilter>(0);
            _expression = $"{propertyPath}.{expression}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringElementFilter" /> class.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="filters">The collection of filters.</param>
        public RecurringElementFilter(string propertyPath, params RecurringElementFilter[] filters)
        {
            PropertyPath = propertyPath;
            Filters = new List<RecurringElementFilter>(filters);
            Predicate = (dataObject, instance, filter) => Filters.Any(x => x.Predicate(dataObject, instance, x));
            _expression = "(" + string.Join(" OR ", Filters.Select(x => x.Expression)) + ")";
        }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <value>The expression.</value>
        public string Expression => ToString();

        /// <summary>
        /// Gets the property path.
        /// </summary>
        /// <value>The property path.</value>
        public string PropertyPath { get; }

        /// <summary>
        /// Gets the predicate.
        /// </summary>
        /// <value>The predicate.</value>
        public Func<object, object, RecurringElementFilter, bool> Predicate { get; }

        /// <summary>
        /// Gets the collection of filters.
        /// </summary>
        /// <value>The collection of filters.</value>
        public List<RecurringElementFilter> Filters { get; }

        /// <summary>
        /// Gets or sets the collection of previous filters.
        /// </summary>
        /// <value>The collection of previous filters.</value>
        public List<RecurringElementFilter> PreviousFilters { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return _expression ?? string.Join(" AND ", Filters.Select(x => x.Expression));
        }

        /// <summary>
        /// Gets the property value from the specified object instance.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="instance">The object instance.</param>
        /// <returns>The property value.</returns>
        public T GetPropertyValue<T>(object instance)
        {
            return Self.Equals(PropertyPath)
                ? (T) instance
                : instance.GetPropertyValue<T>(PropertyPath);
        }
    }
}
