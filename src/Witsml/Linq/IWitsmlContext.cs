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
using Energistics.DataAccess;
using Energistics.Datatypes;

namespace PDS.Witsml.Linq
{
    /// <summary>
    /// Defines the properties for a WITSML data context
    /// </summary>
    public interface IWitsmlContext
    {
        /// <summary>
        /// Gets or sets the log query action.
        /// </summary>
        Action<Functions, string, string, string> LogQuery { get; set; }

        /// <summary>
        /// Gets or sets the log response action.
        /// </summary>
        Action<Functions, string, string, string, string, short, string> LogResponse { get; set; }

        /// <summary>
        /// Gets all wells.
        /// </summary>
        /// <returns>The wells.</returns>
        IEnumerable<IDataObject> GetAllWells();

        /// <summary>
        /// Gets the wellbores.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbores.</returns>
        IEnumerable<IWellObject> GetWellbores(EtpUri parentUri);

        /// <summary>
        /// Gets the wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbore objects of specified type.</returns>
        IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri parentUri);

        /// <summary>
        /// Gets the growing object header only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The header for the specified growing objects.</returns>
        IWellboreObject GetGrowingObjectHeaderOnly(string objectType, EtpUri uri);

        /// <summary>
        /// Gets the object identifier only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object identifier.</returns>
        IDataObject GetObjectIdOnly(string objectType, EtpUri uri);

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object detail.</returns>
        IDataObject GetObjectDetails(string objectType, EtpUri uri);

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>The object detail.</returns>
        IDataObject GetObjectDetails(string objectType, EtpUri uri, params OptionsIn[] optionsIn);
    }
}
