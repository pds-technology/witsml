//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using Energistics.DataAccess;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Represents a data provider that implements support for WITSML API functions.
    /// </summary>
    public interface IWitsmlDataProvider
    {
        /// <summary>
        /// Retrieves data objects from the data store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>Queried objects.</returns>
        WitsmlResult<IEnergisticsCollection> GetFromStore(RequestContext context);

        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult AddToStore(RequestContext context);

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult UpdateInStore(RequestContext context);

        /// <summary>
        /// Deletes or partially update object from store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        WitsmlResult DeleteFromStore(RequestContext context);
    }
}
