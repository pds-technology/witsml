using System;
using System.Collections.Generic;
using Energistics.DataAccess;
using PDS.Framework;

namespace PDS.Witsml.Server
{
    public abstract class DevKitAspect
    {
        public readonly string TimestampFormat = "yyMMdd-HHmmss-fff";
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

        public string Uid()
        {
            return Guid.NewGuid().ToString();
        }

        public string Name(string prefix = null)
        {
            if (String.IsNullOrWhiteSpace(prefix))
                return DateTime.Now.ToString(TimestampFormat);

            return String.Format("{0} - {1}", prefix, DateTime.Now.ToString(TimestampFormat));
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

        public List<T> List<T>(params T[] instances)
        {
            return new List<T>(instances);
        }

        public List<T> One<T>(Action<T> action)
        {
            var instance = Activator.CreateInstance<T>();
            action(instance);
            return List(instance);
        }

        public WMLS_AddToStoreResponse Add<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            var info = typeof(TList).GetProperty(typeof(TObject).Name);
            var list = New<TList>(x => info.SetValue(x, List(entity)));
            var typeIn = wmlTypeIn ?? ObjectTypes.GetObjectType<TList>();
            var xmlIn = EnergisticsConverter.ObjectToXml(list);

            return AddToStore(typeIn, xmlIn, capClient, optionsIn);
        }

        public List<TObject> Query<TList, TObject>(TObject entity, string wmlTypeIn = null, string capClient = null, string optionsIn = null) where TList : IEnergisticsCollection
        {
            var info = typeof(TList).GetProperty(typeof(TObject).Name);
            var list = New<TList>(x => info.SetValue(x, List(entity)));
            var typeIn = wmlTypeIn ?? ObjectTypes.GetObjectType<TList>();
            var queryIn = EnergisticsConverter.ObjectToXml(list);

            var response = GetFromStore(typeIn, queryIn, capClient, optionsIn);
            var results = EnergisticsConverter.XmlToObject<TList>(response.XMLout);

            return (List<TObject>)results.Items;
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
    }
}
