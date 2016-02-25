using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using MongoDB.Bson.Serialization;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Initializes MongoDb class and member mappings.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class MongoDbClassMapper
    {
        /// <summary>
        /// Registers all supported class and member mappings.
        /// </summary>
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

            // WITSML 2.0
            Register2<Witsml200.Well>();
            Register2<Witsml200.Wellbore>();
            Register2<Witsml200.Log>();

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
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.Uid).SetIdGenerator(UidGenerator.Instance);
                });
            }
        }

        private void Register2<T>() where T : Witsml200.ComponentSchemas.AbstractObject
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T).BaseType))
            {
                BsonClassMap.RegisterClassMap<Witsml200.ComponentSchemas.AbstractObject>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(x => x.Uuid).SetIdGenerator(UidGenerator.Instance);
                });
            }

            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>();
            }
        }
    }
}
