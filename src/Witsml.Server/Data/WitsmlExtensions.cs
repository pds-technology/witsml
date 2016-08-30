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
using System.Collections.Generic;
using System.Linq;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Witsml131Schemas = Energistics.DataAccess.WITSML131.ComponentSchemas;
using Witsml141Schemas = Energistics.DataAccess.WITSML141.ComponentSchemas;
using Witsml200Schemas = Energistics.DataAccess.WITSML200.ComponentSchemas;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Provides extension methods for common WITSML data objects.
    /// </summary>
    public static class WitsmlExtensions
    {
        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        public static void Add(this Witsml131.CapServer capServer, Functions function, string dataObject)
        {
            if (capServer.Function == null)
                capServer.Function = new List<Witsml131Schemas.Function>();

            var name = "WMLS_" + function.ToString();
            var func = capServer.Function.FirstOrDefault(x => x.Name == name);

            if (func == null)
            {
                capServer.Function.Add(func = new Witsml131Schemas.Function()
                {
                    Name = name,
                    DataObject = new List<string>()
                });
            }

            func.DataObject.Add(dataObject);
        }

        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        public static void Add(this Witsml141.CapServer capServer, Functions function, string dataObject)
        {
            Add(capServer, function, new Witsml141Schemas.ObjectWithConstraint(dataObject));
        }

        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        /// <param name="maxDataNodes">The maximum data nodes.</param>
        /// <param name="maxDataPoints">The maximum data points.</param>
        public static void Add(this Witsml141.CapServer capServer, Functions function, string dataObject, int maxDataNodes, int maxDataPoints)
        {
            Add(capServer, function, new Witsml141Schemas.ObjectWithConstraint(dataObject)
            {
                MaxDataNodes = maxDataNodes,
                MaxDataPoints = maxDataPoints
            });
        }

        /// <summary>
        /// Adds support for the specified function and data object to the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="dataObject">The data object.</param>
        public static void Add(this Witsml141.CapServer capServer, Functions function, Witsml141Schemas.ObjectWithConstraint dataObject)
        {
            if (capServer.Function == null)
                capServer.Function = new List<Witsml141Schemas.Function>();

            var name = "WMLS_" + function.ToString();
            var func = capServer.Function.FirstOrDefault(x => x.Name == name);

            if (func == null)
            {
                capServer.Function.Add(func = new Witsml141Schemas.Function()
                {
                    Name = name,
                    DataObject = new List<Witsml141Schemas.ObjectWithConstraint>()
                });
            }

            func.DataObject.Add(dataObject);
        }

        /// <summary>
        /// Updates the dTimCreation and dTimLastChange properties in common data.
        /// </summary>
        /// <param name="commonData">The common data.</param>
        /// <returns>The instance of common data.</returns>
        public static Witsml131Schemas.CommonData Create(this Witsml131Schemas.CommonData commonData)
        {
            if (commonData == null)
                commonData = new Witsml131Schemas.CommonData();

            commonData.DateTimeCreation = DateTime.UtcNow;
            commonData.DateTimeLastChange = DateTime.UtcNow;

            return commonData;
        }

        /// <summary>
        /// Updates the dTimCreation and dTimLastChange properties in common data.
        /// </summary>
        /// <param name="commonData">The common data.</param>
        /// <returns>The instance of common data.</returns>
        public static Witsml141Schemas.CommonData Create(this Witsml141Schemas.CommonData commonData)
        {
            if (commonData == null)
                commonData = new Witsml141Schemas.CommonData();

            commonData.DateTimeCreation = DateTimeOffset.UtcNow;
            commonData.DateTimeLastChange = DateTimeOffset.UtcNow;

            return commonData;
        }

        /// <summary>
        /// Updates the Creation and LastUpdate properties in the citation.
        /// </summary>
        /// <param name="citation">The citation.</param>
        /// <returns>The instance of the citation.</returns>
        public static Witsml200Schemas.Citation Create(this Witsml200Schemas.Citation citation)
        {
            if (citation == null)
                citation = new Witsml200Schemas.Citation();

            citation.Creation = DateTime.UtcNow;
            citation.LastUpdate = DateTime.UtcNow;

            return citation;
        }
    }
}
