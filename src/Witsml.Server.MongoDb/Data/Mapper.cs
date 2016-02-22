using System.ComponentModel.Composition;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using MongoDB.Bson.Serialization;
using PDS.Witsml.Server.Models;
using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Mapper
    {
        public void Register()
        {
            // WITSML 1.3.1.1
            Register<Witsml131.Well>();
            Register<Witsml131.Wellbore>();
            Register<Witsml131.Log>();

            // WITSML 1.4.1.1
            Register<Witsml141.Well>();
            Register<Witsml141.Wellbore>();
            Register<Witsml141.Log>();

            // Custom
            if (!BsonClassMap.IsClassMapRegistered(typeof(LogDataValues)))
            {
                BsonClassMap.RegisterClassMap<LogDataValues>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.Uid).SetIdGenerator(UidGenerator.Instance);
                });
            }
        }

        private void Register<T>() where T : IDataObject
        {
            if (BsonClassMap.IsClassMapRegistered(typeof(T)))
                return;

            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Uid).SetIdGenerator(UidGenerator.Instance);
            });
        }
    }
}
