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
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.ChangeLogs;
using PDS.WITSMLstudio.Store.Models;
using PDS.WITSMLstudio.Store.Data.Transactions;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Initializes MongoDb class and member mappings.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IMongoDbClassMapper" />
    [Export(typeof(IMongoDbClassMapper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MongoDbClassMapper : IMongoDbClassMapper
    {
        /// <summary>
        /// Registers all supported class and member mappings.
        /// </summary>
        public void Register()
        {
            RegisterDataTypes();
            RegisterDerivedTypes();

            Register3<Witsml141.ChangeLog>();

            RegisterId<Witsml200.ComponentSchemas.ChannelIndex>("Mnemonic");
            RegisterId<Witsml141.ComponentSchemas.LithostratigraphyStruct>("Kind");
            RegisterId<Witsml141.ComponentSchemas.ChronostratigraphyStruct>("Kind");

            // Custom
            Register3<ChannelDataChunk>();
            Register3<DbTransaction>();
            Register3<DbGrowingObject>();
            Register3<DbAuditHistory>();

            Register(new TimestampSerializer());
            Register(new XmlElementSerializer());
            Register(new ExtensibleEnumSerializer<Witsml200.ReferenceData.UnitOfMeasure>());
            Register(new ExtensibleEnumSerializer<Witsml200.ReferenceData.LithologyKind>());
            Register(new ExtensibleEnumSerializer<Witsml200.ReferenceData.LithologyQualifierKind>());
            Register(new ExtensibleEnumSerializer<Witsml200.ReferenceData.EquipmentType>());
            Register(new ExtensibleEnumSerializer<Witsml200.ReferenceData.QuantityClassKind>());
        }

        /// <summary>
        /// Registers the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void Register(Type type)
        {
            var baseType = type.BaseType;

            while (baseType != null && baseType != typeof(object))
            {
                if (baseType.GetProperty(ObjectTypes.Uuid, BindingFlags.Public | BindingFlags.DeclaredOnly) != null)
                    RegisterId(baseType, ObjectTypes.Uuid);
                else
                    Register3(baseType, true);

                baseType = baseType.BaseType;
            }

            Register3(type, true);
        }

        private void RegisterDerivedTypes()
        {
            var type = typeof(Witsml200.ComponentSchemas.ProjectedWellLocation);

            type.Assembly
                .GetTypes()
                .Where(t => t.Namespace == type.Namespace && !t.IsAbstract && t.GetCustomAttributes<XmlIncludeAttribute>().Any())
                .SelectMany(t => t.GetCustomAttributes<XmlIncludeAttribute>())
                .ForEach(a => Register2(a.Type, true));
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

        private void Register2<T>() where T : Witsml200.AbstractObject
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T).BaseType))
            {
                BsonClassMap.RegisterClassMap<Witsml200.AbstractObject>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                });
            }

            Register3<T>();
        }

        private void Register2(Type type, bool autoMap = false)
        {
            Register3(type.BaseType, true);
            Register3(type, autoMap);
        }

        private void Register3<T>()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                var cm = BsonClassMap.RegisterClassMap<T>();
                cm.SetIgnoreExtraElements(true);
            }
        }

        private void Register3(Type type, bool autoMap = false)
        {
            if (!BsonClassMap.IsClassMapRegistered(type))
            {
                var cm = new BsonClassMap(type);

                if (autoMap)
                    cm.AutoMap();

                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);

                BsonClassMap.RegisterClassMap(cm);
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

        private void RegisterId(Type type, string propertyName = "Uid")
        {
            if (!BsonClassMap.IsClassMapRegistered(type))
            {
                var cm = new BsonClassMap(type);
            
                cm.AutoMap();
                cm.MapIdProperty(propertyName).SetIdGenerator(UidGenerator.Instance);
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);

                BsonClassMap.RegisterClassMap(cm);
            }
        }

        private void Register<T>(IBsonSerializer<T> serializer)
        {
            try
            {
                BsonSerializer.RegisterSerializer(serializer);
            }
            catch (BsonSerializationException)
            {
                // Ignoring exception because there is no clean way to check if a specific type of serializer is already registered. 
                // Calling BsonSerializer.LookupSerializer<Timestamp>() will create the wrong default serializer.
            }
        }
    }
}
