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
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml;
using Energistics.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server
{
    public abstract class DevKitAspect : DataGenerator
    {
        public static readonly int DefaultXmlOutDebugSize = WitsmlSettings.TruncateXmlOutDebugSize;
        public static readonly long DefaultDepthChunkRange = WitsmlSettings.DepthRangeSize;
        public static readonly long DefaultTimeChunkRange = WitsmlSettings.TimeRangeSize;
        public static readonly int DefaultMaxDataPoints = WitsmlSettings.MaxDataPoints;
        public static readonly int DefaultMaxDataNodes = WitsmlSettings.MaxDataNodes;
        public static readonly int DefaultMaxStationCount = WitsmlSettings.MaxStationCount;

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
        }

        public IContainer Container { get; }

        [ImportMany]
        public List<ITestContextProvider> ContextProviders { get; set; }

        public WITSMLWebServiceConnection Proxy { get; }

        public WitsmlStore Store { get; }

        public abstract string DataSchemaVersion { get; }

        public string ConnectionUrl { get; set; }

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

        public List<TObject> QueryWithErrorCode<TList, TObject>(TObject entity, out short result, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            var response = Get<TList, TObject>(List(entity), wmlTypeIn, capClient, optionsIn);
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

        public WMLS_DeleteFromStoreResponse Delete<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            string typeIn, xmlIn;
            SetupParameters<TList, TObject>(List(entity), wmlTypeIn, out typeIn, out xmlIn);

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

        private void SetupParameters<TList, TObject>(List<TObject> entityList, string wmlTypeIn, out string typeIn, out string queryIn) where TList : IEnergisticsCollection
        {
            var info = typeof(TList).GetProperty(typeof(TObject).Name);
            var list = New<TList>(x => info.SetValue(x, entityList));
            typeIn = wmlTypeIn ?? ObjectTypes.GetObjectType<TList>();
            queryIn = EnergisticsConverter.ObjectToXml(list); // WitsmlParser.ToXml(list);
        }

        public IUniqueId GetLogCurveInfoByUid(IEnumerable<IUniqueId> logCurveInfos, string uid)
        {
            if (logCurveInfos == null || string.IsNullOrEmpty(uid))
                return null;

            return logCurveInfos.FirstOrDefault(c => c.Uid == uid);
        }
    }
}
