using System;
using System.Collections.Generic;
using Energistics.DataAccess;

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
        }

        public WITSMLWebServiceConnection Proxy { get; private set; }

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
    }
}
