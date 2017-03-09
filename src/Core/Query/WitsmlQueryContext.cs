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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.Datatypes;
using log4net;
using PDS.Framework;
using PDS.Witsml.Data;
using PDS.Witsml.Linq;

namespace PDS.Witsml.Query
{
    /// <summary>
    ///  Manages the context for WITSML connections and data.
    /// </summary>
    public class WitsmlQueryContext : WitsmlContext
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlQueryContext));
        private static readonly DataObjectTemplate _template = new DataObjectTemplate();

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlQueryContext"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="version">The version.</param>
        /// <param name="timeoutInMinutes">The timeout in minutes.</param>
        public WitsmlQueryContext(string url, WMLSVersion version, double timeoutInMinutes = 1.5)
            : base(url, timeoutInMinutes, version)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlQueryContext"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="version">The version.</param>
        /// <param name="timeoutInMinutes">The timeout in minutes.</param>
        public WitsmlQueryContext(string url, string username, string password, WMLSVersion version, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, version)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlQueryContext"/> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="version">The version.</param>
        /// <param name="timeoutInMinutes">The timeout in minutes.</param>
        public WitsmlQueryContext(string url, string username, SecureString password, WMLSVersion version, double timeoutInMinutes = 1.5)
            : base(url, username, password, timeoutInMinutes, version)
        {
        }

        /// <summary>
        /// Gets all wells.
        /// </summary>
        /// <returns>
        /// The wells.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override IEnumerable<IDataObject> GetAllWells()
        {
            var queryIn = QueryTemplates.GetTemplate(ObjectTypes.Well, DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);

            return GetObjects<IDataObject>(ObjectTypes.Well, queryIn.ToString(), OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the wellbores.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>
        /// The wellbores.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override IEnumerable<IWellObject> GetWellbores(EtpUri parentUri)
        {
            var queryIn = QueryTemplates.GetTemplate(ObjectTypes.Wellbore, DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);

            _template.Set(queryIn, "//@uidWell", parentUri.ObjectId);
            _template.Add(queryIn, "//wellbore", "isActive");

            return GetObjects<IWellObject>(ObjectTypes.Wellbore, queryIn.ToString(), OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbore objects of specified type.</returns>
        public override IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri parentUri)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);
            SetFilterCriteria(objectType, queryIn, parentUri);

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the growing object header only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The header for the specified growing objects.</returns>
        public override IWellboreObject GetGrowingObjectHeaderOnly(string objectType, EtpUri uri)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, DataSchemaVersion, OptionsIn.ReturnElements.HeaderOnly);
            SetFilterCriteria(objectType, queryIn, uri);

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), OptionsIn.ReturnElements.Requested).FirstOrDefault();
        }

        /// <summary>
        /// Gets the growing objects id-only with active status.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The URI.</param>
        /// <returns>
        /// The wellbore objects of specified type with header.
        /// </returns>
        public override IEnumerable<IWellboreObject> GetGrowingObjectsWithStatus(string objectType, EtpUri parentUri)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);
            SetFilterCriteria(objectType, queryIn, parentUri);

            if (ObjectTypes.Log.EqualsIgnoreCase(objectType))
                _template.Add(queryIn, "//log", "objectGrowing");
            else if (ObjectTypes.Trajectory.EqualsIgnoreCase(objectType))
                _template.Add(queryIn, "//trajectory", "objectGrowing");

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the object identifier only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object identifier.</returns>
        public override IDataObject GetObjectIdOnly(string objectType, EtpUri uri)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);
            SetFilterCriteria(objectType, queryIn, uri);

            return GetObjects<IDataObject>(objectType, queryIn.ToString(), OptionsIn.ReturnElements.Requested).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object detail.</returns>
        public override IDataObject GetObjectDetails(string objectType, EtpUri uri)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, DataSchemaVersion, OptionsIn.ReturnElements.All);
            SetFilterCriteria(objectType, queryIn, uri);

            return GetObjects<IDataObject>(objectType, queryIn.ToString(), OptionsIn.ReturnElements.Requested).FirstOrDefault();
        }

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>The object detail.</returns>
        public override IDataObject GetObjectDetails(string objectType, EtpUri uri, params OptionsIn[] optionsIn)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, DataSchemaVersion, OptionsIn.ReturnElements.All);
            SetFilterCriteria(objectType, queryIn, uri);

            var filteredOptionsIn = new List<OptionsIn> { OptionsIn.ReturnElements.Requested };

            optionsIn.ForEach(o =>
            {
                if (o.Value != OptionsIn.ReturnElements.All.Value)
                    filteredOptionsIn.Add(o);
            });

            return GetObjects<IDataObject>(objectType, queryIn.ToString(), filteredOptionsIn.ToArray()).FirstOrDefault();
        }

        /// <summary>
        /// Gets the objects of the specified query.
        /// </summary>
        /// <param name="objectType"></param>
        /// <param name="queryIn"></param>
        /// <param name="optionsIn"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetObjects<T>(string objectType, string queryIn, params OptionsIn[] optionsIn) where T : IDataObject
        {
            var result = ExecuteQuery(objectType, queryIn, OptionsIn.Join(optionsIn));
            var dataObjects = (IEnumerable<T>)result?.Items ?? Enumerable.Empty<T>();
            return dataObjects.OrderBy(x => x.Name);
        }

        private IEnergisticsCollection ExecuteQuery(string objectType, string xmlIn, string optionsIn)
        {
            LogQuery(Functions.GetFromStore, objectType, xmlIn, optionsIn);

            using (var client = Connection.CreateClientProxy())
            {
                var wmls = (IWitsmlClient)client;
                string suppMsgOut, xmlOut = string.Empty;
                IEnergisticsCollection result = null;
                short returnCode;

                try
                {
                    returnCode = wmls.WMLS_GetFromStore(objectType, xmlIn, optionsIn, null, out xmlOut, out suppMsgOut);
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Error querying store: {0}", ex);
                    returnCode = -1;
                    suppMsgOut = "Error querying store:" + ex.GetBaseException().Message;
                }

                try
                {
                    if (returnCode > 0)
                    {
                        var listType = ObjectTypes.GetObjectGroupType(objectType, DataSchemaVersion);
                        var document = WitsmlParser.Parse(xmlOut);

                        result = WitsmlParser.Parse(listType, document.Root) as IEnergisticsCollection;
                    }
                }
                catch (WitsmlException ex)
                {
                    _log.ErrorFormat("Error parsing query response: {0}{2}{2}{1}", xmlOut, ex, Environment.NewLine);
                    returnCode = (short)ex.ErrorCode;
                    suppMsgOut = ex.Message + " " + ex.GetBaseException().Message;
                }

                LogResponse(Functions.GetFromStore, objectType, xmlIn, optionsIn, xmlOut, returnCode, suppMsgOut);
                return result;
            }
        }

        private void SetFilterCriteria(string objectType, XDocument document, EtpUri uri)
        {
            var objectIds = uri.GetObjectIds()
                .ToDictionary(x => x.ObjectType, x => x.ObjectId, StringComparer.InvariantCultureIgnoreCase);

            if (!string.IsNullOrWhiteSpace(uri.ObjectId))
                _template.Set(document, "//@uid", uri.ObjectId);

            if (objectIds.ContainsKey(ObjectTypes.Well) && !ObjectTypes.Well.EqualsIgnoreCase(objectType))
                _template.Set(document, "//@uidWell", objectIds[ObjectTypes.Well]);

            if (objectIds.ContainsKey(ObjectTypes.Wellbore) && !ObjectTypes.Wellbore.EqualsIgnoreCase(objectType))
                _template.Set(document, "//@uidWellbore", objectIds[ObjectTypes.Wellbore]);
        }
    }
}
