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
using System.Linq;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Encapsulates common properties needed for defining recurring element filters.
    /// </summary>
    public class RecurringElementFilter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringElementFilter" /> class.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="predicate">The predicate.</param>
        public RecurringElementFilter(string propertyPath, string expression, Func<object, bool> predicate)
        {
            PropertyPath = propertyPath;
            Expression = $"{propertyPath}.{expression}";
            Predicate = predicate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringElementFilter" /> class.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="isRecurringCriteria">if set to <c>true</c> is recurring criteria.</param>
        /// <param name="filters">The collection of filters.</param>
        public RecurringElementFilter(string propertyPath, bool isRecurringCriteria, params RecurringElementFilter[] filters)
        {
            PropertyPath = propertyPath;
            Filters = filters;

            var junction = isRecurringCriteria ? " OR " : " AND ";
            Expression = "(" + string.Join(junction, filters.Select(x => x.Expression)) + ")";

            Predicate = instance =>
            {
                return isRecurringCriteria
                    ? filters.Any(x => x.Predicate(instance))
                    : filters.All(x => x.Predicate(instance));
            };
        }

        /// <summary>
        /// Gets the property path.
        /// </summary>
        /// <value>The property path.</value>
        public string PropertyPath { get; }

        /// <summary>
        /// Gets the expression.
        /// </summary>
        /// <value>The expression.</value>
        public string Expression { get; }

        /// <summary>
        /// Gets the predicate.
        /// </summary>
        /// <value>The predicate.</value>
        public Func<object, bool> Predicate { get; }

        /// <summary>
        /// Gets the collection of filters.
        /// </summary>
        /// <value>The collection of filters.</value>
        public RecurringElementFilter[] Filters { get; }
    }
}
