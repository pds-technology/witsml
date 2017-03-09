//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
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

using System.Collections.Generic;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Defines the strongly typed methods needed to support ETP.
    /// </summary>
    public interface IEtpDataProvider<T>
    {
        /// <summary>
        /// Determines whether the data object exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the data object exists; otherwise, false</returns>
        bool Exists(EtpUri uri);

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        List<T> GetAll(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        T Get(EtpUri uri);

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        void Put(DataObject dataObject);

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        void Delete(EtpUri uri);

        /// <summary>
        /// Ensures the data object exists with the specified URI, otherwise, it is created.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        void Ensure(EtpUri uri);
    }
}
