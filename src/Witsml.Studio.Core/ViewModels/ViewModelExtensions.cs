//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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

namespace PDS.Witsml.Studio.Core.ViewModels
{
    /// <summary>
    /// Provides static helper methods for core view model types.
    /// </summary>
    public static class ViewModelExtensions
    {
        /// <summary>
        /// Finds a resource by URI.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindByUri(this IList<ResourceViewModel> resources, string uri)
        {
            return resources.Find(x => x.Resource.Uri == uri);
        }

        /// <summary>
        /// Finds the selected resource.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel FindSelected(this IList<ResourceViewModel> resources)
        {
            return resources.Find(x => x.IsSelected);
        }

        /// <summary>
        /// Finds a resource by evaluating the specified predicate on each item in the collection.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        public static ResourceViewModel Find(this IList<ResourceViewModel> resources, Func<ResourceViewModel, bool> predicate)
        {
            foreach (var resource in resources)
            {
                if (predicate(resource))
                    return resource;

                var found = Find(resource.Children, predicate);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
