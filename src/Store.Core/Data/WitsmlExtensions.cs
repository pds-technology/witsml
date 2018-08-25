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
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Witsml131Schemas = Energistics.DataAccess.WITSML131.ComponentSchemas;
using Witsml141Schemas = Energistics.DataAccess.WITSML141.ComponentSchemas;
using Witsml200Schemas = Energistics.DataAccess.WITSML200.ComponentSchemas;
using Prodml200Schemas = Energistics.DataAccess.PRODML200.ComponentSchemas;
using Resqml210Schemas = Energistics.DataAccess.RESQML210.ComponentSchemas;

namespace PDS.WITSMLstudio.Store.Data
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
        /// Set growing timeout period for growing object type in the capServer instance.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="seconds">The growing timeout period in seconds.</param>
        public static void SetGrowingTimeoutPeriod(this Witsml141.CapServer capServer, string objectType, int seconds)
        {
            var growingTimeoutPeriod = new Witsml141Schemas.GrowingTimeoutPeriod(seconds)
            {
                DataObject = objectType
            };

            if (capServer.GrowingTimeoutPeriod == null)
                capServer.GrowingTimeoutPeriod = new List<Witsml141Schemas.GrowingTimeoutPeriod> { growingTimeoutPeriod };
            else
                capServer.GrowingTimeoutPeriod.Add(growingTimeoutPeriod);
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
        /// Updates the Creation and LastUpdate properties in the citation.
        /// </summary>
        /// <param name="citation">The citation.</param>
        /// <returns>The instance of the citation.</returns>
        public static Prodml200Schemas.Citation Create(this Prodml200Schemas.Citation citation)
        {
            if (citation == null)
                citation = new Prodml200Schemas.Citation();

            citation.Creation = DateTime.UtcNow;
            citation.LastUpdate = DateTime.UtcNow;
            citation.Originator = WitsmlOperationContext.Current.User;
            citation.Format = typeof(WitsmlExtensions).Assembly.FullName;

            return citation;
        }

        /// <summary>
        /// Updates the Creation and LastUpdate properties in the citation.
        /// </summary>
        /// <param name="citation">The citation.</param>
        /// <returns>The instance of the citation.</returns>
        public static Resqml210Schemas.Citation Create(this Resqml210Schemas.Citation citation)
        {
            if (citation == null)
                citation = new Resqml210Schemas.Citation();

            citation.Creation = DateTime.UtcNow;
            citation.LastUpdate = DateTime.UtcNow;
            citation.Originator = WitsmlOperationContext.Current.User;
            citation.Format = typeof(WitsmlExtensions).Assembly.FullName;

            return citation;
        }

        /// <summary>
        /// Creates a <see cref="Witsml200Schemas.DataObjectReference" /> for the specified URI.
        /// </summary>
        /// <typeparam name="TObject">The data object type.</typeparam>
        /// <param name="reference">The data object reference.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="Witsml200Schemas.DataObjectReference" /> instance.</returns>
        public static Witsml200Schemas.DataObjectReference Create<TObject>(this Witsml200Schemas.DataObjectReference reference, EtpUri uri) where TObject : Witsml200.AbstractObject, new()
        {
            if (reference != null) return reference;

            var objectType = ObjectTypes.GetObjectType<TObject>();

            if (!objectType.EqualsIgnoreCase(uri.ObjectType))
                uri = new TObject().GetUri();

            return new Witsml200Schemas.DataObjectReference
            {
                ContentType = uri.ContentType,
                Uuid = uri.ObjectId ?? Guid.Empty.ToString(),
                Title = uri.ObjectId ?? ObjectTypes.Unknown
            };
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
            var documentInfo = property?.GetValue(dataObject);

            if (property != null && documentInfo == null)
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
            return totalPoints > function.GetLogMaxDataPoints();
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
        public static bool IsDataNodesValid(this Functions function, string dataObject, int nodeCount)
        {
            if (ObjectTypes.Log.EqualsIgnoreCase(dataObject))
            {
                return nodeCount > function.GetLogMaxNodes();
            }
            if (ObjectTypes.Trajectory.EqualsIgnoreCase(dataObject))
            {
                return nodeCount > function.GetTrajectoryMaxNodes();
            }
            if (ObjectTypes.MudLog.EqualsIgnoreCase(dataObject))
            {
                return nodeCount > function.GetMudLogMaxNodes();
            }
            // Return error as the object is not supported
            return true;
        }

        /// <summary>
        /// Get the Log MaxDataNodes for the function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The MaxDataNodes value.</returns>
        public static int GetLogMaxNodes(this Functions function)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                    return WitsmlSettings.LogMaxDataNodesGet;
                case Functions.AddToStore:
                    return WitsmlSettings.LogMaxDataNodesAdd;
                case Functions.UpdateInStore:
                    return WitsmlSettings.LogMaxDataNodesUpdate;
                case Functions.DeleteFromStore:
                    return WitsmlSettings.LogMaxDataNodesDelete;
                case Functions.PutObject:
                    return Math.Min(WitsmlSettings.LogMaxDataNodesAdd, WitsmlSettings.LogMaxDataNodesUpdate);
                default:
                    return WitsmlSettings.LogMaxDataNodesGet;
            }
        }

        /// <summary>
        /// Get the Trajectory MaxDataNodes for the function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The MaxDataNodes value.</returns>
        public static int GetTrajectoryMaxNodes(this Functions function)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                    return WitsmlSettings.TrajectoryMaxDataNodesGet;
                case Functions.AddToStore:
                    return WitsmlSettings.TrajectoryMaxDataNodesAdd;
                case Functions.UpdateInStore:
                    return WitsmlSettings.TrajectoryMaxDataNodesUpdate;
                case Functions.DeleteFromStore:
                    return WitsmlSettings.TrajectoryMaxDataNodesDelete;
                case Functions.PutObject:
                    return Math.Min(WitsmlSettings.TrajectoryMaxDataNodesAdd, WitsmlSettings.TrajectoryMaxDataNodesUpdate);
                default:
                    return WitsmlSettings.TrajectoryMaxDataNodesGet;
            }
        }

        /// <summary>
        /// Get the MudLog MaxDataNodes for the function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The MaxDataNodes value.</returns>
        public static int GetMudLogMaxNodes(this Functions function)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                    return WitsmlSettings.MudLogMaxDataNodesGet;
                case Functions.AddToStore:
                    return WitsmlSettings.MudLogMaxDataNodesAdd;
                case Functions.UpdateInStore:
                    return WitsmlSettings.MudLogMaxDataNodesUpdate;
                case Functions.DeleteFromStore:
                    return WitsmlSettings.MudLogMaxDataNodesDelete;
                case Functions.PutObject:
                    return Math.Min(WitsmlSettings.MudLogMaxDataNodesAdd, WitsmlSettings.MudLogMaxDataNodesUpdate);
                default:
                    return WitsmlSettings.MudLogMaxDataNodesGet;
            }
        }

        /// <summary>
        /// Get the Log MaxDataPoints for the function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>The MaxDataPoints value.</returns>
        public static int GetLogMaxDataPoints(this Functions function)
        {
            switch (function)
            {
                case Functions.GetFromStore:
                    return WitsmlSettings.LogMaxDataPointsGet;
                case Functions.AddToStore:
                    return WitsmlSettings.LogMaxDataPointsAdd;
                case Functions.UpdateInStore:
                    return WitsmlSettings.LogMaxDataPointsUpdate;
                case Functions.DeleteFromStore:
                    return WitsmlSettings.LogMaxDataPointsDelete;
                case Functions.PutObject:
                    return Math.Min(WitsmlSettings.LogMaxDataPointsAdd, WitsmlSettings.LogMaxDataPointsUpdate);
                default:
                    return WitsmlSettings.LogMaxDataPointsGet;
            }
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
