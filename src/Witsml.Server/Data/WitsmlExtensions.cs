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
using System.Xml.Linq;
using Energistics.DataAccess;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;
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
        /// <param name="maxDataNodes">The maximum data nodes.</param>
        public static void Add(this Witsml141.CapServer capServer, Functions function, string dataObject, int maxDataNodes)
        {
            Add(capServer, function, new Witsml141Schemas.ObjectWithConstraint(dataObject)
            {
                MaxDataNodes = maxDataNodes,
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

            commonData.DateTimeCreation = DateTimeOffset.UtcNow;
            commonData.DateTimeLastChange = commonData.DateTimeCreation;

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
            commonData.DateTimeLastChange = commonData.DateTimeCreation;

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
            citation.Originator = WitsmlOperationContext.Current.User;
            citation.Format = typeof(WitsmlExtensions).Assembly.FullName;

            return citation;
        }

        /// <summary>
        /// Determines whether the list has duplicate UIDs.
        /// </summary>
        /// <param name="list">The list of items with UIDs.</param>
        /// <returns>
        ///   <c>true</c> if the list has duplicate UIDs; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasDuplicateUids<T>(this List<T> list) where T : IUniqueId
        {
            var uids = new HashSet<string>();
            foreach (var item in list)
            {
                if (uids.ContainsIgnoreCase(item.Uid))
                    return true;
                uids.Add(item.Uid);
            }
            return false;
        }

        /// <summary>
        /// Sets the document information if it doesn't already exist.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="parser">The query parser.</param>
        /// <param name="username">The current username.</param>
        /// <returns>The data object instance.</returns>
        public static T SetDocumentInfo<T>(this T dataObject, WitsmlQueryParser parser, string username) where T : IEnergisticsCollection
        {
            var property = dataObject.GetType().GetProperty("DocumentInfo");
            var documentInfo = property.GetValue(dataObject);

            if (documentInfo == null)
            {
                var version = ObjectTypes.GetVersion(typeof(T));

                documentInfo = OptionsIn.DataVersion.Version131.Equals(version)
                    ? (object)CreateDocumentInfo131(parser, username)
                    : CreateDocumentInfo141(parser, username);

                property.SetValue(dataObject, documentInfo);
            }

            return dataObject;
        }


        /// <summary>
        /// Determines whether the logs total data points are valid for the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="totalPoints">The total points.</param>
        /// <returns>
        ///   <c>true</c> if the logs total data points are valid for the specified function; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsTotalDataPointsValid(this Functions function, int totalPoints)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                    return totalPoints > WitsmlSettings.LogMaxDataPointsGet;
                case Functions.AddToStore:
                    return totalPoints > WitsmlSettings.LogMaxDataPointsAdd;
                case Functions.UpdateInStore:
                    return totalPoints > WitsmlSettings.LogMaxDataPointsUpdate;
                case Functions.PutObject:
                    // Use the lesser of the two MaxDataPoint values
                    return totalPoints >
                           (WitsmlSettings.LogMaxDataPointsUpdate < WitsmlSettings.LogMaxDataPointsAdd
                               ? WitsmlSettings.LogMaxDataPointsUpdate
                               : WitsmlSettings.LogMaxDataPointsAdd);
                case Functions.DeleteFromStore:
                    return totalPoints > WitsmlSettings.LogMaxDataPointsDelete;
                default:
                    // Return error as the function is not supported
                    return true;
            }
        }

        /// <summary>
        /// Determines whether the WTISML object's node count is valid for the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="dataObject">The WITSML object.</param>
        /// <param name="nodeCount">The node count.</param>
        /// <returns>
        ///   <c>true</c> if the  WTISML object's node count is valid for the specified function; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDataNodesValid<T>(this Functions function, T dataObject, int nodeCount)
        {
            var objectType = dataObject.GetType();
            if (objectType == typeof(Witsml131.Log) || objectType == typeof(Witsml141.Log))
            {
                switch (function)
                {
                    case Functions.AddToStore:
                        return nodeCount > WitsmlSettings.LogMaxDataNodesAdd;
                    case Functions.UpdateInStore:
                        return nodeCount > WitsmlSettings.LogMaxDataNodesUpdate;
                    // Use the lesser of the two MaxDataNode values
                    case Functions.PutObject:
                        return nodeCount > 
                            (WitsmlSettings.LogMaxDataNodesUpdate < WitsmlSettings.LogMaxDataNodesAdd
                                   ? WitsmlSettings.LogMaxDataNodesUpdate
                                   : WitsmlSettings.LogMaxDataNodesAdd);
                    default:
                        // Return error as the function is not supported
                        return true;
                }
            }
            if (objectType == typeof(Witsml131.Trajectory) || objectType == typeof(Witsml141.Trajectory))
            {
                switch (function)
                {
                    case Functions.AddToStore:
                        return nodeCount > WitsmlSettings.TrajectoryMaxDataNodesAdd;
                    case Functions.UpdateInStore:
                        return nodeCount > WitsmlSettings.TrajectoryMaxDataNodesUpdate;
                    case Functions.PutObject:
                        // Use the lesser of the two MaxDataNode values
                        return nodeCount >
                               (WitsmlSettings.TrajectoryMaxDataNodesUpdate < WitsmlSettings.TrajectoryMaxDataNodesAdd
                                   ? WitsmlSettings.TrajectoryMaxDataNodesUpdate
                                   : WitsmlSettings.TrajectoryMaxDataNodesAdd);
                    case Functions.DeleteFromStore:
                        return nodeCount > WitsmlSettings.TrajectoryMaxDataNodesDelete;
                    default:
                        // Return error as the function is not supported
                        return true;
                }
            }
            if (objectType == typeof(Witsml131.MudLog) || objectType == typeof(Witsml141.MudLog))
            {
                switch (function)
                {
                    case Functions.AddToStore:
                        return nodeCount > WitsmlSettings.MudLogMaxDataNodesAdd;
                    case Functions.UpdateInStore:
                        return nodeCount > WitsmlSettings.MudLogMaxDataNodesUpdate;
                    case Functions.PutObject:
                        // Use the lesser of the two MaxDataNode values
                        return nodeCount >
                            (WitsmlSettings.MudLogMaxDataNodesUpdate < WitsmlSettings.MudLogMaxDataNodesAdd
                                   ? WitsmlSettings.MudLogMaxDataNodesUpdate
                                   : WitsmlSettings.MudLogMaxDataNodesAdd);
                    case Functions.DeleteFromStore:
                        return nodeCount > WitsmlSettings.MudLogMaxDataNodesDelete;
                    default:
                        // Return error as the function is not supported
                        return true;
                }
            }
            // Return error as the object is not supported
            return true;
        }

        private static Witsml131Schemas.DocumentInfo CreateDocumentInfo131(WitsmlQueryParser parser, string username)
        {
            var documentInfo = new Witsml131Schemas.DocumentInfo
            {
                DocumentName = new Witsml131Schemas.NameStruct(parser.ObjectType)
            };

            var documentInfoElement = parser.DocumentInfo();

            if (IncludeFileCreationInformation(documentInfoElement))
            {
                documentInfo.FileCreationInformation = new Witsml131Schemas.FileCreationType
                {
                    FileCreationDate = DateTimeOffset.UtcNow
                };

                if (IncludeFileCreator(documentInfoElement))
                {
                    documentInfo.FileCreationInformation.FileCreator = username;
                }
            }

            return documentInfo;
        }

        private static Witsml141Schemas.DocumentInfo CreateDocumentInfo141(WitsmlQueryParser parser, string username)
        {
            var documentInfo = new Witsml141Schemas.DocumentInfo
            {
                DocumentName = new Witsml141Schemas.NameStruct(parser.ObjectType)
            };

            var documentInfoElement = parser.DocumentInfo();

            if (IncludeFileCreationInformation(documentInfoElement))
            {
                documentInfo.FileCreationInformation = new Witsml141Schemas.DocumentFileCreation
                {
                    FileCreationDate = DateTimeOffset.UtcNow
                };

                if (IncludeFileCreator(documentInfoElement))
                {
                    documentInfo.FileCreationInformation.FileCreator = username;
                }
            }

            return documentInfo;
        }

        private static bool IncludeFileCreationInformation(XElement documentInfoElement)
        {
            var ns = documentInfoElement.GetDefaultNamespace();

            return documentInfoElement.IsEmpty
                || documentInfoElement.Elements(ns + ObjectTypes.FileCreationInformation).Any();
        }

        private static bool IncludeFileCreator(XElement documentInfoElement)
        {
            var ns = documentInfoElement.GetDefaultNamespace();
            var fileCreationElement = documentInfoElement.Element(ns + ObjectTypes.FileCreationInformation);

            return fileCreationElement == null
                || fileCreationElement.IsEmpty
                || fileCreationElement.Elements(ns + "fileCreator").Any();
        }
    }
}
