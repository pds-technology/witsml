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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Logs;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.Common.Datatypes;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// MongoDbUtility tests.
    /// </summary>
    [TestClass]
    public class MongoDbUtilityTests : Log141TestBase
    {
        private IDatabaseProvider _provider;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            _provider = DevKit.Container.Resolve<IDatabaseProvider>();
        }

        [TestMethod]
        public void MongoDbUtility_GetEntityFilter_Can_Get_Id_Filter_From_Uri()
        {
            var collection = _provider.GetDatabase().GetCollection<Wellbore>(ObjectNames.Wellbore141);

            var wellbore141Uri = "eml://witsml14/well(well141)/wellbore(wellbore141)";
            var filters141 = MongoDbUtility.GetEntityFilter<Wellbore>(new EtpUri(wellbore141Uri));
            Assert.IsNotNull(filters141);
            var filters141Json = filters141.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
            Assert.IsNotNull(filters141Json);
            Assert.AreEqual(2, filters141Json.ElementCount);
            var filterElements = filters141Json.Elements.ToList();
            Assert.AreEqual(ObjectTypes.Uid, filterElements[0].Name);
            Assert.AreEqual("UidWell", filterElements[1].Name);


            var wellbore200Uri = "eml://witsml20/wellbore(wellbore200)";
            var filters200 = MongoDbUtility.GetEntityFilter<Wellbore>(new EtpUri(wellbore200Uri), ObjectTypes.Uuid);
            var filters200Json = filters200.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
            Assert.IsNotNull(filters200Json);
            Assert.AreEqual(1, filters200Json.ElementCount);
            Assert.AreEqual(ObjectTypes.Uuid, filters200Json.Elements.ToList()[0].Name);
        }

        [TestMethod]
        public void MongoDbUtility_CreateUpdateFields_Returns_Dictionary_Of_common_Objects_For_Update()
        {
            var commonPropertiesToUpdate = MongoDbUtility.CreateUpdateFields<Well>();
            Assert.AreEqual(1, commonPropertiesToUpdate.Count);
            Assert.IsNotNull(commonPropertiesToUpdate["CommonData.DateTimeLastChange"]);

            commonPropertiesToUpdate = MongoDbUtility.CreateUpdateFields<Witsml200.Well>();
            Assert.AreEqual(1, commonPropertiesToUpdate.Count);
            Assert.IsNotNull(commonPropertiesToUpdate["Citation.LastUpdate"]);

            commonPropertiesToUpdate = MongoDbUtility.CreateUpdateFields<DateTime>();
            Assert.AreEqual(0, commonPropertiesToUpdate.Count);
        }

        [TestMethod]
        public void MongoDbUtility_CreateIgnoreFields_Returns_List_Of_common_Objects_To_Ignore()
        {
            var commonPropertiesToIgnore = MongoDbUtility.CreateIgnoreFields<Well>(null);
            Assert.IsTrue(commonPropertiesToIgnore.Count > 0);

            commonPropertiesToIgnore = MongoDbUtility.CreateIgnoreFields<Witsml200.Well>(null);
            Assert.IsTrue(commonPropertiesToIgnore.Count > 0);

            var ignoredList = new List<string> { "dTimLicense" };
            commonPropertiesToIgnore = MongoDbUtility.CreateIgnoreFields<Well>(ignoredList);
            Assert.IsTrue(commonPropertiesToIgnore.Count > 1);
            Assert.IsTrue(commonPropertiesToIgnore.Contains("dTimLicense"));
        }

        [TestMethod]
        public void MongoDbUtility_BuildFilter_Returns_Filter_For_Specified_Field()
        {
            var collection = _provider.GetDatabase().GetCollection<Well>(ObjectNames.Well141);

            var filter = MongoDbUtility.BuildFilter<Well>(ObjectTypes.Uid, ObjectTypes.Uid);
            Assert.IsNotNull(filter);
            var filterJson = filter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
            Assert.IsNotNull(filterJson);
            Assert.AreEqual(1, filterJson.ElementCount);
            Assert.AreEqual(ObjectTypes.Uid, filterJson.Elements.ToList()[0].Name);

            filter = MongoDbUtility.BuildFilter<Well>(ObjectTypes.Uid, true);
            Assert.IsNotNull(filter);
            filterJson = filter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
            Assert.IsNotNull(filterJson);
            Assert.AreEqual(1, filterJson.ElementCount);
            Assert.AreEqual(ObjectTypes.Uid, filterJson.Elements.ToList()[0].Name);

            AddParents();
            
            filter = MongoDbUtility.BuildFilter<Well>(ObjectTypes.Uid, Well.Uid);
            Assert.IsNotNull(filter);
            var result = collection.Find(filter).ToList();
            Assert.IsTrue(result.Count == 1);
        }

        [TestMethod]
        public void MongoDbUtility_BuildFilter_Can_Create_Filter_For_Specified_Field()
        {
            var collection = _provider.GetDatabase().GetCollection<Well>(ObjectNames.Well141);

            AddParents();

            var filter = MongoDbUtility.BuildFilter<Well>(ObjectTypes.Uid, Well.Uid);
            Assert.IsNotNull(filter);
            var result = collection.Find(filter).ToList();
            Assert.IsTrue(result.Count == 1);
            Assert.AreEqual(Well.Name, result[0].Name);
        }

        [TestMethod]
        public void MongoDbUtility_BuildUpdate_Can_Create_UpdateDefinition_For_Specified_Field()
        {
            var collection = _provider.GetDatabase().GetCollection<Well>(ObjectNames.Well141);

            AddParents();

            var updateWellName = Well.Name + "Updated";
            var updateDef = MongoDbUtility.BuildUpdate<Well>(null, ObjectTypes.NameProperty, updateWellName);
            Assert.IsNotNull(updateDef);
            collection.UpdateOne(Builders<Well>.Filter.Eq(ObjectTypes.Uid, Well.Uid), updateDef);
            var updatedWell = DevKit.GetAndAssert(Well);
            Assert.AreEqual(updateWellName, updatedWell.Name);

            Assert.IsNull(updatedWell.DateTimeLicense);
            var updateDefCombine = MongoDbUtility.BuildUpdate(updateDef, "DateTimeLicense", DateTime.UtcNow.ToString("o"));
            Assert.IsNotNull(updateDefCombine);
            collection.UpdateOne(Builders<Well>.Filter.Eq(ObjectTypes.Uid, Well.Uid), updateDefCombine);
            updateDefCombine = MongoDbUtility.BuildUpdate(updateDef, "DateTimeLicenseSpecified", true);
            Assert.IsNotNull(updateDefCombine);
            collection.UpdateOne(Builders<Well>.Filter.Eq(ObjectTypes.Uid, Well.Uid), updateDefCombine);
            updatedWell = DevKit.GetAndAssert(Well);
            Assert.IsNotNull(updatedWell.DateTimeLicense);
            Assert.AreEqual(updateWellName, updatedWell.Name);
        }

        [TestMethod]
        public void MongoDbUtility_BuildPush_Can_Create_UpdateDefinition_For_Specified_Array_Field_With_Value()
        {
            var collection = _provider.GetDatabase().GetCollection<Log>(ObjectNames.Log141);

            AddParents();
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert<LogList, Log>(Log);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, addedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve1"));
            var updateDefinition = MongoDbUtility.BuildPush<Log>(null, ObjectTypes.LogCurveInfo.ToPascalCase(), DevKit.CreateDoubleLogCurveInfo("curve1", "unit1"));
            Assert.IsNotNull(updateDefinition);
            collection.UpdateOne(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid), updateDefinition);
            var updatedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, updatedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve1"));

            var updateDefinitionName = MongoDbUtility.BuildUpdate<Log>(null, ObjectTypes.NameProperty, Log.Name + "Updated");
            Assert.AreEqual(0, addedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve2"));
            var updateDefinitionCurve2 = MongoDbUtility.BuildPush(updateDefinitionName, ObjectTypes.LogCurveInfo.ToPascalCase(), DevKit.CreateDoubleLogCurveInfo("curve2", "unit2"));
            Assert.IsNotNull(updateDefinitionCurve2);
            collection.UpdateMany(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid), updateDefinitionCurve2);
            var updatedLogCurve2 = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, updatedLogCurve2.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve2"));
            Assert.AreEqual(Log.Name + "Updated", updatedLogCurve2.Name);
        }

        [TestMethod]
        public void MongoDbUtility_BuildPushEach_Can_Create_UpdateDefinition_For_Specified_Array_Field_With_Values()
        {
            var collection = _provider.GetDatabase().GetCollection<Log>(ObjectNames.Log141);

            AddParents();
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert<LogList, Log>(Log);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, addedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve1"));
            var curves = new List<LogCurveInfo>
            {
                DevKit.CreateDoubleLogCurveInfo("curve1", "unit1"),
                DevKit.CreateDoubleLogCurveInfo("curve2", "unit2")
            };
            var updateDefinition = MongoDbUtility.BuildPushEach<Log, LogCurveInfo>(null, ObjectTypes.LogCurveInfo.ToPascalCase(), curves);
            Assert.IsNotNull(updateDefinition);
            collection.UpdateMany(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid), updateDefinition);
            var updatedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, updatedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve1"));
            Assert.AreEqual(1, updatedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve2"));

            var updateDefinitionName = MongoDbUtility.BuildUpdate<Log>(null, ObjectTypes.NameProperty, Log.Name + "Updated");
            Assert.AreEqual(0, addedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve3"));
            curves.Clear();
            curves.AddRange(new List<LogCurveInfo>
            {
                DevKit.CreateDoubleLogCurveInfo("curve3", "unit3"),
                DevKit.CreateDoubleLogCurveInfo("curve4", "unit4")
            });
            var updateDefinitionCurveSet2 = MongoDbUtility.BuildPushEach(updateDefinitionName, ObjectTypes.LogCurveInfo.ToPascalCase(), curves);
            Assert.IsNotNull(updateDefinitionCurveSet2);
            collection.UpdateMany(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid), updateDefinitionCurveSet2);
            var updatedLogCurve2 = DevKit.GetAndAssert(Log);
            Assert.IsNotNull(updatedLogCurve2);
            Assert.AreEqual(1, updatedLogCurve2.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve3"));
            Assert.AreEqual(1, updatedLogCurve2.LogCurveInfo.Count(l => l.Mnemonic.Value == "curve4"));
            Assert.AreEqual(Log.Name + "Updated", updatedLogCurve2.Name);
        }

        [TestMethod]
        public void MongoDbUtility_LookUpIdField_Returns_IdField_For_Specified_Type()
        {
            var well141 = MongoDbUtility.LookUpIdField(typeof(Well));
            Assert.AreEqual(ObjectTypes.Uid, well141);
            var well200 = MongoDbUtility.LookUpIdField(typeof(Witsml200.Well), ObjectTypes.Uuid);
            Assert.AreEqual(ObjectTypes.Uuid, well200);
            var well200ChannelIndex = MongoDbUtility.LookUpIdField(typeof(Witsml200.ComponentSchemas.ChannelIndex));
            Assert.AreEqual("Mnemonic", well200ChannelIndex);
        }

        [TestMethod]
        public void MongoDbUtility_GetObjectUris_Returns_List_Of_Uri_By_ObjectType()
        {
            var listUri = new List<EtpUri>
            {
                new EtpUri("eml://witsml14/well(well141)"),
                new EtpUri("eml://witsml14/well(well141Another)"),
                new EtpUri("eml://witsml14/well(well141)/wellbore(wellbore141)"),
                new EtpUri("eml://witsml20/wellbore(wellbore200)")
            };

            var objUri = MongoDbUtility.GetObjectUris(listUri, ObjectTypes.Well);
            Assert.AreEqual(2, objUri.Count);
            var objUriWellbore = MongoDbUtility.GetObjectUris(listUri, ObjectTypes.Wellbore);
            Assert.AreEqual(2, objUriWellbore.Count);
        }

        [TestMethod]
        public void MongoDbUtility_GetDocumentId_Returns_Id_In_BsonDocument_Format()
        {
            AddParents();
            var bsonDoc = MongoDbUtility.GetDocumentId(Well);
            Assert.IsNotNull(bsonDoc);
            Assert.IsTrue(bsonDoc.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uid)).ToList().Count > 0);
            Assert.AreEqual(Well.Uid, bsonDoc.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uid)).ToList()[0].Value);

            var bsonDocWellbore = MongoDbUtility.GetDocumentId(Wellbore);
            Assert.IsNotNull(bsonDocWellbore);
            Assert.IsTrue(bsonDocWellbore.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uid)).ToList().Count > 0);
            Assert.AreEqual(Wellbore.Uid, bsonDocWellbore.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uid)).ToList()[0].Value);

            var bsonDocLog = MongoDbUtility.GetDocumentId(Log);
            Assert.IsNotNull(bsonDocLog);
            Assert.IsTrue(bsonDocLog.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uid)).ToList().Count > 0);
            Assert.AreEqual(Log.Uid, bsonDocLog.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uid)).ToList()[0].Value);

            var well200 = new Witsml200.Well { Uuid = DevKit.Uid(), Citation = new Witsml200.ComponentSchemas.Citation { Title = DevKit.Name("Well 01")}, TimeZone = DevKit.TimeZone };
            var bsonDoc200 = MongoDbUtility.GetDocumentId(well200);
            Assert.IsTrue(bsonDoc200.Elements.Where(f=>f.Name.EqualsIgnoreCase(ObjectTypes.Uuid)).ToList().Count > 0);
            Assert.AreEqual(well200.Uuid, bsonDoc200.Elements.Where(f => f.Name.EqualsIgnoreCase(ObjectTypes.Uuid)).ToList()[0].Value);
        }
    }
}
