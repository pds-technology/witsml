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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Energistics.DataAccess;
using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Properties;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store
{
    public abstract class DevKitAspect : DataGenerator
    {
        public static readonly bool DefaultAllowDuplicateNonRecurringElements = Settings.Default.AllowDuplicateNonRecurringElements;
        public static readonly bool DefaultLogAllowPutObjectWithData = Settings.Default.LogAllowPutObjectWithData;
        public static readonly bool DefaultTrajectoryAllowPutObjectWithData = Settings.Default.TrajectoryAllowPutObjectWithData;
        public static readonly bool DefaultMudLogAllowPutObjectWithData = Settings.Default.MudLogAllowPutObjectWithData;
        public static readonly InvalidDataRowSetting DefaultInvalidDataRowSetting = (InvalidDataRowSetting)Enum.Parse(typeof(InvalidDataRowSetting), Settings.Default.InvalidDataRowSetting);
        public static readonly UnknownElementSetting DefaultUnknownElementSetting = (UnknownElementSetting) Enum.Parse(typeof(UnknownElementSetting), Settings.Default.UnknownElementSetting);

        public static readonly int DefaultXmlOutDebugSize = WitsmlSettings.TruncateXmlOutDebugSize;
        public static readonly long DefaultDepthChunkRange = WitsmlSettings.DepthRangeSize;
        public static readonly long DefaultTimeChunkRange = WitsmlSettings.TimeRangeSize;
        public static readonly int DefaultLogMaxDataPointsGet = WitsmlSettings.LogMaxDataPointsGet;
        public static readonly int DefaultLogMaxDataPointsAdd = WitsmlSettings.LogMaxDataPointsAdd;
        public static readonly int DefaultLogMaxDataPointsUpdate = WitsmlSettings.LogMaxDataPointsUpdate;
        public static readonly int DefaultLogMaxDataPointsDelete = WitsmlSettings.LogMaxDataPointsDelete;
        public static readonly int DefaultLogMaxDataNodesGet = WitsmlSettings.LogMaxDataNodesGet;
        public static readonly int DefaultLogMaxDataNodesAdd = WitsmlSettings.LogMaxDataNodesAdd;
        public static readonly int DefaultLogMaxDataNodesUpdate = WitsmlSettings.LogMaxDataNodesUpdate;
        public static readonly int DefaultLogMaxDataNodesDelete = WitsmlSettings.LogMaxDataNodesDelete;
        public static readonly int DefaultTrajectoryMaxDataNodesGet = WitsmlSettings.TrajectoryMaxDataNodesGet;
        public static readonly int DefaultTrajectoryMaxDataNodesAdd = WitsmlSettings.TrajectoryMaxDataNodesAdd;
        public static readonly int DefaultTrajectoryMaxDataNodesUpdate = WitsmlSettings.TrajectoryMaxDataNodesUpdate;
        public static readonly int DefaultTrajectoryMaxDataNodesDelete = WitsmlSettings.TrajectoryMaxDataNodesDelete;
        public static readonly int DefaultMudLogMaxDataNodesGet = WitsmlSettings.MudLogMaxDataNodesGet;
        public static readonly int DefaultMudLogMaxDataNodesAdd = WitsmlSettings.MudLogMaxDataNodesAdd;
        public static readonly int DefaultMudLogMaxDataNodesUpdate = WitsmlSettings.MudLogMaxDataNodesUpdate;
        public static readonly int DefaultMudLogMaxDataNodesDelete = WitsmlSettings.MudLogMaxDataNodesDelete;
        public static readonly int DefaultMaxStationCount = WitsmlSettings.MaxStationCount;
        public static readonly int DefaultLogGrowingTimeoutPeriod = WitsmlSettings.LogGrowingTimeoutPeriod;
        public static readonly int DefaultTrajectoryGrowingTimeoutPeriod = WitsmlSettings.TrajectoryGrowingTimeoutPeriod;
        public static readonly int DefaultMudLogGrowingTimeoutPeriod = WitsmlSettings.MudLogGrowingTimeoutPeriod;
        public static readonly string DefaultTimeZone = WitsmlSettings.DefaultTimeZone;
        public static readonly int DefaultMaxDataLength = WitsmlSettings.MaxDataLength;
        public static readonly bool DefaultIsRequestCompressionEnabled = WitsmlSettings.IsRequestCompressionEnabled;

        public readonly string TimeZone = "-06:00";

        protected DevKitAspect(string url, WMLSVersion version, TestContext context)
        {
            ConnectionUrl = url;
            Container = ContainerFactory.Create();
            Container.BuildUp(this);
            ContextProviders.ForEach(x => x.Configure(this, context));

            Store = new WitsmlStore();
            Store.Container = Container;
            Container.BuildUp(Store);

            Proxy = new WITSMLWebServiceConnection(ConnectionUrl, version);
            Proxy.Timeout *= 5;

            Template = new DataObjectTemplate();
        }

        public IContainer Container { get; }

        [ImportMany]
        public List<ITestContextProvider> ContextProviders { get; set; }

        public WITSMLWebServiceConnection Proxy { get; }

        public WitsmlStore Store { get; }

        public DataObjectTemplate Template { get; }

        public abstract string DataSchemaVersion { get; }

        public string ConnectionUrl { get; set; }

        public static void RestoreDefaultSettings()
        {
            CompatibilitySettings.AllowDuplicateNonRecurringElements = DefaultAllowDuplicateNonRecurringElements;
            CompatibilitySettings.LogAllowPutObjectWithData = DefaultLogAllowPutObjectWithData;
            CompatibilitySettings.TrajectoryAllowPutObjectWithData = DefaultTrajectoryAllowPutObjectWithData;
            CompatibilitySettings.MudLogAllowPutObjectWithData = DefaultMudLogAllowPutObjectWithData;
            CompatibilitySettings.InvalidDataRowSetting = DefaultInvalidDataRowSetting;
            CompatibilitySettings.UnknownElementSetting = DefaultUnknownElementSetting;

            WitsmlSettings.TruncateXmlOutDebugSize = DefaultXmlOutDebugSize;
            WitsmlSettings.DepthRangeSize = DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DefaultTimeChunkRange;
            WitsmlSettings.LogMaxDataPointsGet = DefaultLogMaxDataPointsGet;
            WitsmlSettings.LogMaxDataPointsAdd = DefaultLogMaxDataPointsAdd;
            WitsmlSettings.LogMaxDataPointsUpdate = DefaultLogMaxDataPointsUpdate;
            WitsmlSettings.LogMaxDataPointsDelete = DefaultLogMaxDataPointsDelete;
            WitsmlSettings.LogMaxDataNodesGet = DefaultLogMaxDataNodesGet;
            WitsmlSettings.LogMaxDataNodesAdd = DefaultLogMaxDataNodesAdd;
            WitsmlSettings.LogMaxDataNodesUpdate = DefaultLogMaxDataNodesUpdate;
            WitsmlSettings.LogMaxDataNodesDelete = DefaultLogMaxDataNodesDelete;
            WitsmlSettings.TrajectoryMaxDataNodesGet = DefaultTrajectoryMaxDataNodesGet;
            WitsmlSettings.TrajectoryMaxDataNodesAdd = DefaultTrajectoryMaxDataNodesAdd;
            WitsmlSettings.TrajectoryMaxDataNodesUpdate = DefaultTrajectoryMaxDataNodesUpdate;
            WitsmlSettings.TrajectoryMaxDataNodesDelete = DefaultTrajectoryMaxDataNodesDelete;
            WitsmlSettings.MudLogMaxDataNodesGet = DefaultMudLogMaxDataNodesGet;
            WitsmlSettings.MudLogMaxDataNodesAdd = DefaultMudLogMaxDataNodesAdd;
            WitsmlSettings.MudLogMaxDataNodesUpdate = DefaultMudLogMaxDataNodesUpdate;
            WitsmlSettings.MudLogMaxDataNodesDelete = DefaultMudLogMaxDataNodesDelete;
            WitsmlSettings.MaxStationCount = DefaultMaxStationCount;
            WitsmlSettings.LogGrowingTimeoutPeriod = DefaultLogGrowingTimeoutPeriod;
            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = DefaultTrajectoryGrowingTimeoutPeriod;
            WitsmlSettings.MudLogGrowingTimeoutPeriod = DefaultMudLogGrowingTimeoutPeriod;
            WitsmlSettings.DefaultTimeZone = DefaultTimeZone;
            WitsmlSettings.MaxDataLength = DefaultMaxDataLength;
            WitsmlSettings.IsRequestCompressionEnabled = DefaultIsRequestCompressionEnabled;
        }

    public TList Query<TList>() where TList : IEnergisticsCollection
        {
            return WITSMLWebServiceConnection
                .BuildEmptyQuery<TList>()
                .SetVersion(DataSchemaVersion);
        }

        public TList Query<TList, TObject>(Action<TList, List<TObject>> setter, Action<TObject> filters) where TList : IEnergisticsCollection
        {
            var query = Query<TList>();
            setter(query, One(filters));
            return query;
        }

        public TList New<TList>(Action<TList> action) where TList : IEnergisticsCollection
        {
            var list = Activator.CreateInstance<TList>();
            action(list);
            return list;
        }

        public List<T> One<T>(Action<T> action)
        {
            var instance = Activator.CreateInstance<T>();
            action(instance);
            return List(instance);
        }

        public WitsmlQueryParser Parser<T>(T entity, string options = null)
        {
            var document = WitsmlParser.Parse(EnergisticsConverter.ObjectToXml(entity));
            return new WitsmlQueryParser(document.Root, ObjectTypes.GetObjectType<T>(), options);
        }

        public bool HasChildNodes(XmlElement element)
        {
            return element != null ? element.HasChildNodes : false;
        }

        /// <summary>
        /// Asserts the names of the specified data objects.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="result">The result.</param>
        /// <param name="entity">The entity.</param>
        public void AssertNames<TObject>(TObject result, TObject entity = null) where TObject : class, IWellboreObject
        {
            if (entity != null)
            {
                Assert.AreEqual(entity.Name, result.Name);
                Assert.AreEqual(entity.NameWell, result.NameWell);
                Assert.AreEqual(entity.NameWellbore, result.NameWellbore);
            }
            else
            {
                Assert.IsNull(result.Name);
                Assert.IsNull(result.NameWell);
                Assert.IsNull(result.NameWellbore);
            }
        }

        /// <summary>
        /// Asserts that only the requested elements have values on the data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="requested">The requested elements.</param>
        public void AssertRequestedElements(object dataObject, string[] requested)
        {
            var requestedProperties = new List<string>();
            requested.ForEach(x =>
            {
                requestedProperties.Add(x);
                requestedProperties.Add(x + "Specified");
            });

            var publicProperties = dataObject.GetType().GetProperties().Where(x => !requestedProperties.ContainsIgnoreCase(x.Name));
            publicProperties.ForEach(x =>
            {
                var result = x.GetValue(dataObject, null);
                if (result is bool)
                {
                    Assert.IsFalse((bool)result, x.Name);
                }
                else if (result is IList)
                {
                    Assert.AreEqual(0, ((IList)result).Count, x.Name);
                }
                else
                {
                    Assert.IsNull(result, x.Name);
                }
            });
        }

        /// <summary>
        /// Executes GetFromStore and tests the response.
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="example">The example data object.</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="queryByExample">if set to <c>true</c> query by example.</param>
        /// <param name="errorCode">The expected error code.</param>
        /// <returns>The data object instance if found; otherwise, null.</returns>
        public TObject GetAndAssert<TList, TObject>(TObject example, bool isNotNull = true, string optionsIn = null, bool queryByExample = false, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection where TObject : IDataObject
        {
            var query = queryByExample ? example : CreateQuery(example);
            return QueryAndAssert<TList, TObject>(query, isNotNull, optionsIn, errorCode);
        }

        /// <summary>
        /// Executes GetFromStore and tests the response.
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="isNotNull">if set to <c>true</c> the result should not be null.</param>
        /// <param name="optionsIn">The options in.</param>
        /// <param name="errorCode">The expected error code.</param>
        /// <returns>The data object instance if found; otherwise, null.</returns>
        public TObject QueryAndAssert<TList, TObject>(TObject query, bool isNotNull = true, string optionsIn = null, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection
        {
            short resultCode;
            var results = QueryWithErrorCode<TList, TObject>(query, out resultCode, ObjectTypes.GetObjectType<TList>(), null, optionsIn ?? OptionsIn.ReturnElements.All);

            // Assert that we get the expected Result (Error) Code
            Assert.AreEqual((short)errorCode, resultCode);

            Assert.AreEqual(isNotNull ? 1 : 0, results.Count);

            var result = results.FirstOrDefault();
            Assert.AreEqual(isNotNull, result != null);

            return result;
        }

        /// <summary>
        /// Adds a wellbore child object and test the return code
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The <see cref="WMLS_AddToStoreResponse"/> from the store.</returns>
        public WMLS_AddToStoreResponse AddAndAssert<TList, TObject>(TObject dataObject, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection
        {
            var response = Add<TList, TObject>(dataObject);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
            return response;
        }

        /// <summary>
        /// Updates the data object and test the return code
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        public void UpdateAndAssert<TList, TObject>(TObject dataObject, ErrorCodes errorCode = ErrorCodes.Success) where TList : IEnergisticsCollection
        {
            var response = Update<TList, TObject>(dataObject);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
        }

        public void UpdateAndAssert(string typeIn, string xmlIn, ErrorCodes errorCode = ErrorCodes.Success)
        {
            var response = UpdateInStore(typeIn, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
        }

        /// <summary>
        /// Deletes the data object and test the return code
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="partialDelete">if set to <c>true</c> is partial delete.</param>
        public void DeleteAndAssert<TList, TObject>(TObject dataObject, ErrorCodes errorCode = ErrorCodes.Success, bool partialDelete = false) where TList : IEnergisticsCollection where TObject : IDataObject
        {
            var query = partialDelete ? dataObject : CreateQuery(dataObject);
            var response = Delete<TList, TObject>(query, partialDelete: partialDelete);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
        }

        /// <summary>
        /// Deletes the data object and assert.
        /// </summary>
        /// <param name="wmlTypeIn">The Witsml type of the data object.</param>
        /// <param name="queryIn">The query XML.</param>
        /// <param name="errorCode">The error code to assert for the response.</param>
        public void DeleteAndAssert(string wmlTypeIn, string queryIn, ErrorCodes errorCode = ErrorCodes.Success)
        {
            var response = DeleteFromStore(wmlTypeIn, queryIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)errorCode, response.Result);
        }

        /// <summary>
        /// Ensures the data object and assert.
        /// </summary>
        /// <typeparam name="TList">The type of the container.</typeparam>
        /// <typeparam name="TObject">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        public void EnsureAndAssert<TList, TObject>(TObject dataObject) where TList : IEnergisticsCollection where TObject : IDataObject
        {
            var uri = dataObject.GetUri();
            var dataProvider = Container.Resolve<IEtpDataProvider<TObject>>();
            dataProvider.Ensure(uri);

            var exists = dataProvider.Exists(uri);
            Assert.IsTrue(exists);

            var current = dataProvider.Get(dataObject.GetUri());
            Assert.AreEqual(uri, current.GetUri());
        }

        /// <summary>
        /// Creates an id-only query from the specified data object.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="example">The example.</param>
        /// <returns></returns>
        public TObject CreateQuery<TObject>(TObject example) where TObject : IDataObject
        {
            var wellObject = example as IWellObject;
            var wellboreObject = example as IWellboreObject;
            var query = Activator.CreateInstance<TObject>();

            if (example.Uid != null)
            {
                query.Uid = example.Uid;
            }

            if (wellObject?.UidWell != null)
            {
                ((IWellObject)query).UidWell = wellObject.UidWell;
            }

            if (wellboreObject?.UidWellbore != null)
            {
                ((IWellboreObject)query).UidWellbore = wellboreObject.UidWellbore;
            }

            return query;
        }

        /// <summary>
        /// Creates an id-only query from the specified data object.
        /// </summary>
        /// <typeparam name="TList">The listType of the data object.</typeparam>
        /// <typeparam name="T">The type of the data object.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A new <see cref="XDocument"/> instance.</returns>
        public XDocument CreateQuery<TList, T>(T dataObject)
        {
            var objectTemplate = Template.Create<TList>();

            SetDocumentUids(dataObject, objectTemplate);
            Assert.IsNotNull(objectTemplate);
            Assert.IsNotNull(objectTemplate.Root);

            // Remove all child nodes
            Template.Remove(objectTemplate, $"//{ObjectTypes.GetObjectType<T>()}/*");

            return objectTemplate;
        }

        public WMLS_AddToStoreResponse Add<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            string typeIn, xmlIn;
            SetupParameters<TList, TObject>(List(entity), wmlTypeIn, out typeIn, out xmlIn);

            return AddToStore(typeIn, xmlIn, capClient, optionsIn);
        }

        public List<TObject> Query<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            short result;
            return QueryWithErrorCode<TList, TObject>(entity, out result, wmlTypeIn, capClient, optionsIn);
        }

        public List<TObject> Query<TList, TObject>(List<TObject> entities, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            short result;
            return QueryWithErrorCode<TList, TObject>(entities, out result, wmlTypeIn, capClient, optionsIn);
        }

        public List<TObject> Query<TList, TObject>(string wmlTypeIn, string queryIn, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            var response = GetFromStore(wmlTypeIn, queryIn, capClient, optionsIn);
            var results = EnergisticsConverter.XmlToObject<TList>(response.XMLout);
            return (List<TObject>)results.Items;
        }

        public List<TObject> QueryWithErrorCode<TList, TObject>(TObject entity, out short result, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            var response = Get<TList, TObject>(List(entity), wmlTypeIn, capClient, optionsIn);
            var results = EnergisticsConverter.XmlToObject<TList>(response.XMLout);
            result = response.Result;

            return (List<TObject>)results.Items;
        }

        public List<TObject> QueryWithErrorCode<TList, TObject>(List<TObject> entities, out short result, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            var response = Get<TList, TObject>(entities, wmlTypeIn, capClient, optionsIn);
            var results = EnergisticsConverter.XmlToObject<TList>(response.XMLout);
            result = response.Result;

            return (List<TObject>)results.Items;
        }

        public WMLS_GetFromStoreResponse Get<TList, TObject>(List<TObject> entityList, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            string typeIn, queryIn;
            SetupParameters<TList, TObject>(entityList, wmlTypeIn, out typeIn, out queryIn);

            var response = GetFromStore(typeIn, queryIn, capClient, optionsIn);
            return response;
        }

        public WMLS_AddToStoreResponse AddToStore(string wmlTypeIn, string xmlIn, string capClient, string optionsIn)
        {
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = wmlTypeIn, XMLin = xmlIn, CapabilitiesIn = capClient, OptionsIn = optionsIn };
            return Store.WMLS_AddToStore(request);
        }

        public WMLS_DeleteFromStoreResponse Delete<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null, bool partialDelete = false) where TList : IEnergisticsCollection
        {
            string typeIn, xmlIn;
            SetupParameters<TList, TObject>(List(entity), wmlTypeIn, out typeIn, out xmlIn);

            if (!partialDelete)
            {
                var element = WitsmlParser.Parse(xmlIn);
                WitsmlParser.RemoveEmptyElements(element.Root);
                xmlIn = element.ToString();
            }

            return DeleteFromStore(typeIn, xmlIn, capClient, optionsIn);
        }

        public WMLS_DeleteFromStoreResponse DeleteFromStore(string wmlTypeIn, string xmlIn, string capClient, string optionsIn)
        {
            var request = new WMLS_DeleteFromStoreRequest() { WMLtypeIn = wmlTypeIn, QueryIn = xmlIn, CapabilitiesIn = capClient, OptionsIn = optionsIn };
            return Store.WMLS_DeleteFromStore(request);
        }

        public WMLS_GetFromStoreResponse GetFromStore(string wmlTypeIn, string queryIn, string capClient, string optionsIn)
        {
            var request = new WMLS_GetFromStoreRequest { WMLtypeIn = wmlTypeIn, QueryIn = queryIn, CapabilitiesIn = capClient, OptionsIn = optionsIn };
            return Store.WMLS_GetFromStore(request);
        }

        public WMLS_UpdateInStoreResponse Update<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            string typeIn, xmlIn;
            SetupParameters<TList, TObject>(List(entity), wmlTypeIn, out typeIn, out xmlIn);

            return UpdateInStore(typeIn, xmlIn, capClient, optionsIn);
        }

        public WMLS_UpdateInStoreResponse UpdateInStore(string wmlTypeIn, string xmlIn, string capClient, string optionsIn)
        {
            var request = new WMLS_UpdateInStoreRequest { WMLtypeIn = wmlTypeIn, XMLin = xmlIn, CapabilitiesIn = capClient, OptionsIn = optionsIn };
            return Store.WMLS_UpdateInStore(request);
        }

        public void SetupParameters<TList, TObject>(List<TObject> entityList, string wmlTypeIn, out string typeIn, out string queryIn) where TList : IEnergisticsCollection
        {
            var objectType = ObjectTypes.GetObjectType<TList>();
            var version = ObjectTypes.GetVersion(typeof(TList));
            var family = ObjectTypes.GetFamily(typeof(TList));
            var property = ObjectTypes.GetObjectTypeListProperty(objectType, family, version);

            var info = typeof(TList).GetProperty(property);
            var list = New<TList>(x => info.SetValue(x, entityList));

            typeIn = wmlTypeIn ?? objectType;
            queryIn = EnergisticsConverter.ObjectToXml(list); // WitsmlParser.ToXml(list);
        }

        public void SetDocumentUids<T>(T dataObject, XDocument document)
        {
            if (dataObject is IDataObject)
            {
                Template.Set(document, "//@uid", ((IDataObject)dataObject).Uid);
            }
            if (dataObject is IWellObject)
            {
                Template.Set(document, "//@uidWell", ((IWellObject)dataObject).UidWell);
            }
            if (dataObject is IWellboreObject)
            {
                Template.Set(document, "//@uidWellbore", ((IWellboreObject)dataObject).UidWellbore);
            }
        }

        public IUniqueId GetLogCurveInfoByUid(IEnumerable<IUniqueId> logCurveInfos, string uid)
        {
            if (logCurveInfos == null || string.IsNullOrEmpty(uid))
                return null;

            return logCurveInfos.FirstOrDefault(c => c.Uid == uid);
        }

        public string RandomString(int size)
        {
            var random = new Random();
            const string input = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, size)
                                   .Select(x => input[random.Next(0, input.Length)]);
            return new string(chars.ToArray());
        }

        public void AssertDataRowWithQuotes(string row, string delimiter, int mnemonicCount = 0)
        {
            using (var sr = new StringReader(row))
            {
                using (var parser = new TextFieldParser(sr))
                {
                    parser.SetDelimiters(delimiter);
                    var values = parser.ReadFields();
                    Assert.IsNotNull(values);
                    Assert.AreEqual(mnemonicCount, values.Length);
                }
            }
        }
    }
}
