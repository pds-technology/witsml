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

using System.Collections;
using System.Collections.Generic;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines the methods needed to support ETP.
    /// </summary>
    public interface IEtpDataProvider
    {
        /// <summary>
        /// Adds the content types managed by this data adapter to the collection of <see cref="EtpContentType"/>.
        /// </summary>
        /// <param name="contentTypes">A collection of content types.</param>
        void GetSupportedObjects(IList<EtpContentType> contentTypes);

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        IList GetAll(EtpUri? parentUri = null);

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        object Get(EtpUri uri);

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Put(DataObject dataObject);

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        WitsmlResult Delete(EtpUri uri);
    }
}
