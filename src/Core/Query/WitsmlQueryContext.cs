//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Security;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common.Datatypes;
using log4net;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Linq;

namespace PDS.WITSMLstudio.Query
{
    /// <summary>
    ///  Manages the context for WITSML connections and data.
    /// </summary>
    public class WitsmlQueryContext : WitsmlContext
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlQueryContext));
        private readonly DataObjectTemplate _template = new DataObjectTemplate();

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
        /// Initializes a new instance of the <see cref="WitsmlQueryContext"/> class.
        /// </summary>
        /// <param name="connection">The witsml web service connection.</param>
        /// <param name="version">The version.</param>
        public WitsmlQueryContext(WITSMLWebServiceConnection connection, WMLSVersion version)
            : base(connection, version)
        {
        }

        /// <summary>
        /// Gets the supported get from store objects.
        /// </summary>
        /// <returns>The array of supported get from store objects.</returns>
        public override string[] GetSupportedGetFromStoreObjects()
        {
            string suppMsgOut, capabilitiesOut;

            var returnCode = ExecuteQuery(Functions.GetCap, null, null, null, out capabilitiesOut, out suppMsgOut);

            if (returnCode < 1 || string.IsNullOrEmpty(capabilitiesOut))
                return new string[] { };

            var supportedObjects = new List<string>();
            var xml = XDocument.Parse(capabilitiesOut);

            if (xml.Root != null)
            {
                var ns = xml.Root.GetDefaultNamespace();
                xml.Descendants(ns + "function")
                    .Where(x => x.HasAttributes && x.Attribute("name")?.Value == "WMLS_GetFromStore")
                    .Descendants()
                    .ForEach(x => supportedObjects.Add(x.Value));
            }

            return supportedObjects.ToArray();
        }

        /// <summary>
        /// Gets all wells.
        /// </summary>
        /// <returns>The wells.</returns>
        public override IEnumerable<IDataObject> GetAllWells()
        {
            var queryIn = QueryTemplates.GetTemplate(ObjectTypes.Well, "WITSML", DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);

            _template.Add(queryIn, "//well", "timeZone", "wellDatum", "wellLocation");
            _template.Add(queryIn, "//well/wellDatum", "name");

            AddCommonDataElements(queryIn, "//well");

            return GetObjects<IDataObject>(ObjectTypes.Well, queryIn.ToString(), optionsIn: OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the wellbores.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The wellbores.</returns>
        public override IEnumerable<IWellObject> GetWellbores(EtpUri parentUri)
        {
            var queryIn = QueryTemplates.GetTemplate(ObjectTypes.Wellbore, parentUri.Family, DataSchemaVersion, OptionsIn.ReturnElements.IdOnly);

            _template.Set(queryIn, "//@uidWell", parentUri.ObjectId);

            if (!IsVersion131(parentUri))
                _template.Add(queryIn, "//wellbore", "isActive");

            AddCommonDataElements(queryIn, "//wellbore");

            return GetObjects<IWellObject>(ObjectTypes.Wellbore, queryIn.ToString(), optionsIn: OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the name and IDs of active wellbores.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="logXmlResponse">If set to <c>true</c> then log the XML response.</param>
        /// <returns>The name and IDs of the wellbores.</returns>
        public override IEnumerable<IWellObject> GetActiveWellbores(EtpUri parentUri, bool logXmlResponse = true)
        {
            if (IsVersion131(parentUri))
                return new List<IWellObject>();

            var queryIn = GetTemplateAndSetIds(ObjectTypes.Wellbore, parentUri, OptionsIn.ReturnElements.IdOnly);
            var xpath = $"//{ObjectTypes.Wellbore}";

            _template.Add(queryIn, xpath, "isActive");
            _template.Set(queryIn, $"{xpath}/isActive", true);

            return GetObjects<IWellObject>(ObjectTypes.Wellbore, queryIn.ToString(), logXmlResponse: logXmlResponse, returnNullIfError: true, optionsIn: OptionsIn.ReturnElements.IdOnly);
        }

        /// <summary>
        /// Gets the wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="logXmlResponse">If set to <c>true</c> then log the XML response.</param>
        /// <returns>The wellbore objects of specified type.</returns>
        public override IEnumerable<IWellboreObject> GetWellboreObjects(string objectType, EtpUri parentUri, bool logXmlResponse = true)
        {
            var queryIn = GetTemplateAndSetIds(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);

            AddCommonDataElements(queryIn, $"//{objectType}");

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), optionsIn: OptionsIn.ReturnElements.Requested, logXmlResponse: logXmlResponse);
        }

        /// <summary>
        /// Gets the names and IDs of wellbore objects.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The names and IDs of wellbore objects of specified type.</returns>
        public override IEnumerable<IWellboreObject> GetWellboreObjectIds(string objectType, EtpUri parentUri)
        {
            var queryIn = GetTemplateAndSetIds(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);

            var queryOptionsIn = IsVersion131(parentUri)
                ? OptionsIn.ReturnElements.Requested
                : OptionsIn.ReturnElements.IdOnly;

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), optionsIn: queryOptionsIn);
        }

        /// <summary>
        /// Gets the object identifier only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object identifier.</returns>
        public override IDataObject GetObjectIdOnly(string objectType, EtpUri uri)
        {
            var queryIn = GetTemplateAndSetIds(objectType, uri, OptionsIn.ReturnElements.IdOnly);

            var queryOptionsIn = IsVersion131(uri)
                ? OptionsIn.ReturnElements.Requested
                : OptionsIn.ReturnElements.IdOnly;

            return GetObjects<IDataObject>(objectType, queryIn.ToString(), optionsIn: queryOptionsIn).FirstOrDefault();
        }

        /// <summary>
        /// Gets the growing object header only.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The header for the specified growing objects.</returns>
        public override IWellboreObject GetGrowingObjectHeaderOnly(string objectType, EtpUri uri)
        {
            var templateType = OptionsIn.ReturnElements.IdOnly;
            var queryOptionsIn = OptionsIn.ReturnElements.HeaderOnly;

            if (IsVersion131(uri))
            {
                templateType = OptionsIn.ReturnElements.HeaderOnly;
                queryOptionsIn = OptionsIn.ReturnElements.Requested;
            }

            var queryIn = GetTemplateAndSetIds(objectType, uri, templateType);

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), optionsIn: queryOptionsIn).FirstOrDefault();
        }

        /// <summary>
        /// Gets the name and IDs of growing objects with active status.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="logXmlResponse">If set to <c>true</c> then log the XML response.</param>
        /// <returns>/// The name and IDs of the wellbore objects of specified type.</returns>
        public override IEnumerable<IWellboreObject> GetGrowingObjects(string objectType, EtpUri parentUri, bool logXmlResponse = true)
        {
            var queryIn = GetTemplateAndSetIds(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);
            var xpath = $"//{objectType}";

            _template.Add(queryIn, xpath, "objectGrowing");
            _template.Set(queryIn, $"{xpath}/objectGrowing", true);

            var queryOptionsIn = IsVersion131(parentUri)
                ? OptionsIn.ReturnElements.Requested
                : OptionsIn.ReturnElements.IdOnly;

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), logXmlResponse: logXmlResponse, returnNullIfError: true, optionsIn: queryOptionsIn);
        }

        /// <summary>
        /// Gets the growing objects id-only with active status.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="parentUri">The parent URI.</param>
        /// <param name="indexType">Type of the index.</param>
        /// <returns>The wellbore objects of specified type with header.</returns>
        public override IEnumerable<IWellboreObject> GetGrowingObjectsWithStatus(string objectType, EtpUri parentUri, string indexType = null)
        {
            var queryIn = GetTemplateAndSetIds(objectType, parentUri, OptionsIn.ReturnElements.IdOnly);
            var xpath = $"//{objectType}";

            _template.Add(queryIn, xpath, "objectGrowing");

            if (ObjectTypes.Log.EqualsIgnoreCase(objectType))
            {
                _template.Add(queryIn, xpath, "indexType", "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "direction", "indexCurve");
                _template.Add(queryIn, $"{xpath}/startIndex", "@uom");
                _template.Add(queryIn, $"{xpath}/endIndex", "@uom");

                try
                {
                    if (!string.IsNullOrEmpty(indexType))
                    {
                        var indexTypeName = (LogIndexType)typeof(LogIndexType).ParseEnum(indexType);
                        _template.Set(queryIn, $"{xpath}/indexType", indexTypeName.GetName());
                    }
                }
                catch
                {
                    //ignore
                }
            }
            else if (ObjectTypes.Trajectory.EqualsIgnoreCase(objectType))
            {
                _template.Add(queryIn, xpath, "mdMn", "mdMx");
                _template.Add(queryIn, $"{xpath}/mdMn", "@uom");
                _template.Add(queryIn, $"{xpath}/mdMx", "@uom");
            }
            else if (ObjectTypes.MudLog.EqualsIgnoreCase(objectType))
            {
                _template.Add(queryIn, xpath, "startMd", "endMd");
                _template.Add(queryIn, $"{xpath}/startMd", "@uom");
                _template.Add(queryIn, $"{xpath}/endMd", "@uom");
            }

            AddCommonDataElements(queryIn, xpath);

            return GetObjects<IWellboreObject>(objectType, queryIn.ToString(), OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Gets the object details.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>The object detail.</returns>
        public override IDataObject GetObjectDetails(string objectType, EtpUri uri)
        {
            return GetObjectDetails(objectType, uri, OptionsIn.ReturnElements.All);
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
            var templateType = IsVersion131(uri)
                ? OptionsIn.ReturnElements.All
                : OptionsIn.ReturnElements.IdOnly;

            var queryIn = GetTemplateAndSetIds(objectType, uri, templateType);

            return GetObjects<IDataObject>(objectType, queryIn.ToString(), optionsIn.ToArray()).FirstOrDefault();
        }

        /// <summary>
        /// Gets the objects of the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="queryIn">The query in.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>The data objects.</returns>
        public IEnumerable<T> GetObjects<T>(string objectType, string queryIn, params OptionsIn[] optionsIn) where T : IDataObject
        {
            return GetObjects<T>(objectType, queryIn, true, true, false, optionsIn);
        }

        /// <summary>
        /// Gets the objects of the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="queryIn">The query in.</param>
        /// <param name="logXmlRequest">if set to <c>true</c> log XML request.</param>
        /// <param name="logXmlResponse">if set to <c>true</c> log XML response.</param>
        /// <param name="returnNullIfError">if set to <c>true</c> and if there was an error querying return null, else empty.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <returns>The data objects.</returns>
        public IEnumerable<T> GetObjects<T>(string objectType, string queryIn, bool logXmlRequest = true, bool logXmlResponse = true, bool returnNullIfError = false, params OptionsIn[] optionsIn) where T : IDataObject
        {
            var result = ExecuteGetFromStoreQuery(objectType, queryIn, OptionsIn.Join(optionsIn), logXmlRequest, logXmlResponse);

            var dataObjects = (IEnumerable<T>)result?.Items ?? (returnNullIfError ? null : Enumerable.Empty<T>());

            return dataObjects?.OrderBy(x => x.Name);
        }

        private IEnergisticsCollection ExecuteGetFromStoreQuery(string objectType, string xmlIn, string optionsIn, bool logQuery = true, bool logResponse = true)
        {
            IEnergisticsCollection result = null;
            string suppMsgOut, xmlOut;
            var originalXmlIn = xmlIn;

            if (Connection.CompressRequests)
                ClientCompression.Compress(ref xmlIn, ref optionsIn);

            if (logQuery)
                LogQuery(Functions.GetFromStore, objectType, originalXmlIn, optionsIn);

            var returnCode = ExecuteQuery(Functions.GetFromStore, objectType, xmlIn, optionsIn, out xmlOut, out suppMsgOut);

            if (returnCode < 1)
            {
                if (logResponse)
                    LogResponse(Functions.GetFromStore, objectType, xmlIn, optionsIn, null, returnCode, suppMsgOut);

                return null;
            }

            try
            {
                // Handle servers that compress the response to a compressed request.
                if (Connection.CompressRequests)
                    xmlOut = ClientCompression.SafeDecompress(xmlOut);

                if (returnCode > 0)
                {
                    var document = WitsmlParser.Parse(xmlOut);
                    var family = ObjectTypes.GetFamily(document.Root);
                    var listType = ObjectTypes.GetObjectGroupType(objectType, family, DataSchemaVersion);

                    result = WitsmlParser.Parse(listType, document.Root) as IEnergisticsCollection;
                }
            }
            catch (WitsmlException ex)
            {
                _log.ErrorFormat("Error parsing query response: {0}{2}{2}{1}", xmlOut, ex, Environment.NewLine);
                returnCode = (short)ex.ErrorCode;
                suppMsgOut = ex.Message + " " + ex.GetBaseException().Message;
            }

            if (logResponse)
                LogResponse(Functions.GetFromStore, objectType, originalXmlIn, optionsIn, xmlOut, returnCode, suppMsgOut);

            return result;
        }

        private short ExecuteQuery(Functions functionType, string objectType, string xmlIn, string optionsIn, out string xmlOut, out string suppMsgOut)
        {
            using (var client = Connection.CreateClientProxy().WithUserAgent())
            {
                var wmls = (IWitsmlClient)client;
                xmlOut = null;
                suppMsgOut = string.Empty;
                short returnCode = 0;

                try
                {
                    switch (functionType)
                    {
                        case Functions.GetCap:
                            returnCode = wmls.WMLS_GetCap(new OptionsIn.DataVersion(DataSchemaVersion), out xmlOut, out suppMsgOut);
                            break;
                        case Functions.GetFromStore:
                            returnCode = wmls.WMLS_GetFromStore(objectType, xmlIn, optionsIn, null, out xmlOut, out suppMsgOut);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _log.ErrorFormat("Error querying store: {0}", ex);
                    returnCode = -1;
                    suppMsgOut = "Error querying store:" + ex.GetBaseException().Message;
                }

                if (returnCode < 1)
                    _log.WarnFormat("Unsuccessful return code: {0}{2}{2}{1}", returnCode, suppMsgOut, Environment.NewLine);

                return returnCode;
            }
        }

        private XDocument GetTemplateAndSetIds(string objectType, EtpUri uri, OptionsIn.ReturnElements templateType)
        {
            var queryIn = QueryTemplates.GetTemplate(objectType, uri.Family, DataSchemaVersion, templateType);
            SetFilterCriteria(objectType, queryIn, uri);

            return queryIn;
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

        private void AddCommonDataElements(XDocument document, string xpath)
        {
            _template.Add(document, xpath, "commonData");
            _template.Add(document, $"{xpath}/commonData", "dTimCreation", "dTimLastChange", "itemState");
        }

        private bool IsVersion131(EtpUri uri)
        {
            return uri.Version == null ? OptionsIn.DataVersion.Version131.Equals(DataSchemaVersion) : OptionsIn.DataVersion.Version131.Equals(uri.Version);
        }
    }
}
