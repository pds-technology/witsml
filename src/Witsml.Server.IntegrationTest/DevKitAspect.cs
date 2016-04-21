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
using System.Xml;
using Energistics.DataAccess;
using PDS.Framework;
using PDS.Witsml.Data;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data;

namespace PDS.Witsml.Server
{
    public abstract class DevKitAspect : DataGenerator
    {
        public readonly string TimeZone = "-06:00";

        public DevKitAspect(string url, WMLSVersion version)
        {
            Proxy = new WITSMLWebServiceConnection(url, version);
            Proxy.Timeout *= 5;
            Store = new WitsmlStore();
            Store.Container = ContainerFactory.Create();
            Store.Container.BuildUp(Store);
        }

        public WITSMLWebServiceConnection Proxy { get; private set; }

        public WitsmlStore Store { get; private set; }

        public abstract string DataSchemaVersion { get; }

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

        public WitsmlQueryParser Parser<T>(Functions function, T entity, string options = null, string capabilities = null)
        {
            var context = new RequestContext(function, ObjectTypes.GetObjectType<T>(),
                WitsmlParser.ToXml(entity), options, capabilities);

            return new WitsmlQueryParser(context);
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
            var response = Get<TList, TObject>(List(entity), wmlTypeIn, capClient, optionsIn);
            var results = EnergisticsConverter.XmlToObject<TList>(response.XMLout);

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
