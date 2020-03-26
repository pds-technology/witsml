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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Logs;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// MongoDbExtensions tests.
    /// </summary>
    [TestClass]
    public class MongoDbExtensionsTests : Log141TestBase
    {
        private IDatabaseProvider _provider;
        
        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            _provider = DevKit.Container.Resolve<IDatabaseProvider>();
        }

        [TestMethod]
        public void MongoDbExtensions_EqIgnoreCase_Return_Null_Filter_For_Invalid_Property()
        {
            var builder = new FilterDefinitionBuilder<Well>();
            Assert.IsNull(builder.EqIgnoreCase(string.Empty, "uid"));
            Assert.IsNull(builder.EqIgnoreCase(ObjectTypes.Uid, null));
            Assert.IsNull(builder.EqIgnoreCase(null, null));
        }

        [TestMethod]
        public void MongoDbExtensions_EqIgnoreCase_Can_Create_RegEx_Filter_For_Case_Insensitive_Search()
        {
            var collection = _provider.GetDatabase().GetCollection<Well>(ObjectNames.Well141);

            AddParents();

            var filter = Builders<Well>.Filter.Eq(ObjectTypes.Uid, Well.Uid.ToUpper());
            var result = collection.Find(filter).ToList();
            Assert.IsTrue(result.Count == 0);

            var regexFilter = Builders<Well>.Filter.EqIgnoreCase(ObjectTypes.Uid, Well.Uid.ToUpper());
            result = collection.Find(regexFilter).ToList();
            Assert.IsTrue(result.Count == 1);
            Assert.AreEqual(Well.Name, result[0].Name);

            var emptyFilter = Builders<Well>.Filter.EqIgnoreCase(ObjectTypes.Uid, string.Empty);
            result = collection.Find(emptyFilter).ToList();
            Assert.IsTrue(result.Count == 0);
        }       

        [TestMethod]
        public void MongoDbExtensions_EqualsIgnoreCase_Can_Create_RegEx_Filter_For_Case_Insensitive_Search()
        {
            var collection = _provider.GetDatabase().GetCollection<Well>(ObjectNames.Well141);

            AddParents();

            var filter = Builders<Well>.Filter.Eq(ObjectTypes.Uid, Well.Uid.ToUpper());
            var result = collection.Find(filter).ToList();
            Assert.IsTrue(result.Count == 0);

            var regexFilter = MongoDbExtensions.EqualsIgnoreCase(Well.GetType(), ObjectTypes.Uid, Well.Uid.ToUpper()) as FilterDefinition<Well>;
            result = collection.Find(regexFilter).ToList();
            Assert.IsTrue(result.Count == 1);
            Assert.AreEqual(Well.Name, result[0].Name);

            var regexFilterGeneric = MongoDbExtensions<Well>.EqualsIgnoreCase(ObjectTypes.Uid, Well.Uid.ToUpper());
            result = collection.Find(regexFilterGeneric).ToList();
            Assert.IsTrue(result.Count == 1);
            Assert.AreEqual(Well.Name, result[0].Name);

            var emptyFilter = MongoDbExtensions.EqualsIgnoreCase(Well.GetType(), ObjectTypes.Uid, string.Empty) as FilterDefinition<Well>;
            result = collection.Find(emptyFilter).ToList();
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public void MongoDbExtensions_PullFilter_Can_Create_Pull_Filter_Update_Definition_Expression()
        {
            var collection = _provider.GetDatabase().GetCollection<Log>(ObjectNames.Log141);

            AddParents();
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.AddAndAssert<LogList, Log>(Log);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(1, addedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "ROP"));
            var childFilter = Builders<LogCurveInfo>.Filter.Eq("Mnemonic.Value", "ROP");
            var updateDefinition = MongoDbExtensions.PullFilter(typeof(Log), typeof(LogCurveInfo), ObjectTypes.LogCurveInfo.ToPascalCase(), childFilter) as UpdateDefinition<Log>;
            var log = collection.Find(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid));
            Assert.IsNotNull(log);
            collection.UpdateOne(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid), updateDefinition);
            var updatedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, updatedLog.LogCurveInfo.Count(l => l.Mnemonic.Value == "ROP"));

            Assert.AreEqual(1, updatedLog.LogCurveInfo.Count(l => l.Uid == "GR"));
            var childFilterGr = Builders<LogCurveInfo>.Filter.Eq(ObjectTypes.Uid, "GR");
            var updateDefinitionGeneric = MongoDbExtensions<Log, LogCurveInfo>.PullFilter(ObjectTypes.LogCurveInfo.ToPascalCase(), childFilterGr);
            collection.UpdateOne(Builders<Log>.Filter.Eq(ObjectTypes.Uid, Log.Uid), updateDefinitionGeneric);
            updatedLog = DevKit.GetAndAssert(Log);
            Assert.AreEqual(0, updatedLog.LogCurveInfo.Count(l => l.Uid == "GR"));
        }
    }
}
