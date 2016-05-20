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
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class DatabaseProviderTests
    {
        private IDatabaseProvider Provider;
        private DevKit141Aspect DevKit;

        private Well Well1;
        private Well Well2;

        [TestInitialize]
        public void TestSetUp()
        {
            var container = ContainerFactory.Create();
            Provider = new DatabaseProvider(container, new MongoDbClassMapper());
            DevKit = new DevKit141Aspect();

            Well1 = new Well() { Name = DevKit.Name("Mongo Well 01"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            Well2 = new Well() { Name = DevKit.Name("Mongo Well 02"), TimeZone = DevKit.TimeZone };
        }

        [TestMethod]
        public void Can_add_and_query_well()
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

    // TODO: remove this after adding NuGet package for PDS.Witsml.Server.IntegrationTest
    public class DevKit141Aspect
    {
        public string TimeZone => "-06:00";

        public string Name(string name)
        {
            return $"{ name }-{ DateTime.Now.ToString("yyyyMMdd-HHmmss-ffff") }";
        }

        public string Uid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
