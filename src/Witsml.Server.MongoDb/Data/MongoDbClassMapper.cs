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

using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using MongoDB.Bson;
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
            Register2<Witsml200.ChannelSet>();
            Register2<Witsml200.Channel>();

            Register3<Witsml200.ComponentSchemas.GeodeticWellLocation>();
            Register3<Witsml200.ComponentSchemas.GeodeticEpsgCrs>();
            Register3<Witsml200.ComponentSchemas.IndexRangeContext>();
            Register3<Witsml200.ComponentSchemas.DepthIndexValue>();
            Register3<Witsml200.ComponentSchemas.TimeIndexValue>();
            RegisterId<Witsml200.ComponentSchemas.ChannelIndex>("Mnemonic");

            // Custom
            Register3<ChannelDataChunk>();
            
            try
            {
                BsonSerializer.RegisterSerializer(new TimestampSerializer());
            }
            catch (BsonSerializationException)
            {
                // Ignoring exception because there is no clean way to check if a specific type of serializer is already registered. 
                // Calling BsonSerializer.LookupSerializer<Timestamp>() will create the wrong default serializer.
            }
        }

        private void Register<T>() where T : IDataObject
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    // update mappings to ignore _id field
                    cm.SetIgnoreExtraElements(true);
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

            Register3<T>();
        }

        private void Register3<T>()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                var cm = BsonClassMap.RegisterClassMap<T>();
                cm.SetIgnoreExtraElements(true);
            }
        }

        private void RegisterId<T>(string propertyName = "Uid")
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(propertyName).SetIdGenerator(UidGenerator.Instance);
                });
            }
        }
    }
}
