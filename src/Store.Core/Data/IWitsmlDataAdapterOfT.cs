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

using System.Collections.Generic;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Transactions;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Defines methods that can be used to perform CRUD operations on WITSML data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    public interface IWitsmlDataAdapter<T>
    {
        /// <summary>
        /// Gets the server sort order.
        /// </summary>
        string ServerSortOrder { get; }

        /// <summary>
        /// Gets a reference to a new <see cref="IWitsmlTransaction"/> instance.
        /// </summary>
        /// <returns>A new <see cref="IWitsmlTransaction"/> instance.</returns>
        IWitsmlTransaction GetTransaction();

        /// <summary>
        /// Gets a value indicating whether validation is enabled for this data adapter.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns><c>true</c> if validation is enabled for this data adapter; otherwise, <c>false</c>.</returns>
        bool IsValidationEnabled(Functions function, WitsmlQueryParser parser, T dataObject);

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        List<T> Query(WitsmlQueryParser parser, ResponseContext context);

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        void Add(WitsmlQueryParser parser, T dataObject);

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        void Update(WitsmlQueryParser parser, T dataObject);

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        void Replace(WitsmlQueryParser parser, T dataObject);

        /// <summary>
        /// Deletes or partially updates the specified object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        void Delete(WitsmlQueryParser parser);

        /// <summary>
        /// Determines whether the data object exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the data object exists; otherwise, false</returns>
        bool Exists(EtpUri uri);

        /// <summary>
        /// Gets the count of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The count of related data objects.</returns>
        int Count(EtpUri? parentUri = null);

        /// <summary>
        /// Determines if the specified URI has child data objects.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>If there are any related data objects.</returns>
        bool Any(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        List<T> GetAll(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>.
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        T Get(EtpUri uri, params string[] fields);

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        void Delete(EtpUri uri);
    }
}
