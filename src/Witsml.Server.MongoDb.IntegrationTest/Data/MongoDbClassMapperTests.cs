//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class MongoDbClassMapperTests
    {
        private MongoDbClassMapper Mapper;

        [TestInitialize]
        public void TestSetUp()
        {
            Mapper = new MongoDbClassMapper();
            Mapper.Register();
        }

        [TestMethod]
        public void MongoDbClassMapper_Can_Provide_Id_Mapping_Details()
        {
            var classMap = BsonClassMap.LookupClassMap(typeof(ChannelIndex));

            Assert.IsNotNull(classMap.IdMemberMap);
            Assert.AreEqual("Mnemonic", classMap.IdMemberMap.MemberName);
        }
    }
}
