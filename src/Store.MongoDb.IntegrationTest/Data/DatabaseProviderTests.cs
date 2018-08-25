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

using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PDS.WITSMLstudio.Store.Data
{
    [TestClass]
    public class DatabaseProviderTests
    {
        private IDatabaseProvider Provider;
        private DevKit141Aspect DevKit;

        private Well Well1;
        private Well Well2;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);
            Provider = DevKit.Container.Resolve<IDatabaseProvider>();

            Well1 = new Well() { Name = DevKit.Name("Mongo Well 01"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            Well2 = new Well() { Name = DevKit.Name("Mongo Well 02"), TimeZone = DevKit.TimeZone };
        }

        [TestMethod]
        public void DatabaseProvider_Can_Add_And_Query_Well()
        {
            var database = Provider.GetDatabase();
            var collection = database.GetCollection<Well>(ObjectNames.Well141);

            collection.InsertMany(new[] { Well1, Well2 });

            var exclude = Builders<Well>.Projection.Exclude("_id");
            var filter = Builders<Well>.Filter.Regex("Uid", new BsonRegularExpression("/^" + Well1.Uid + "$/i"));
            var result = collection.Find(filter).Project<Well>(exclude).ToList();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(Well1.Name, result[0].Name);
        }
    }
}
