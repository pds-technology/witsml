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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class SqlSchemaMapperTests
    {
        private SqlSchemaMapper _mapper;

        private const string Json = @"{
          ""version"": ""1.4.1.1"",
          ""database"": {
            ""connectionString"": ""Data Source=(local)\\SQLEXPRESS;Initial Catalog=WitsmlStore;Integrated Security=True;"",
            ""providerName"": ""System.Data.SqlClient""
          },
          ""mappings"": {
            ""well"": {
              ""table"": ""Well"",
              ""alias"": ""w"",
              ""filter"": ""w.ParentWellId = 0"",
              ""columns"": [
                { ""column"": ""w.WellId"", ""alias"": ""Uid"", ""selection"":  true, ""isUid"": true },
                { ""column"": ""w.WellName"", ""alias"": ""Name"", ""selection"":  true, ""isName"": true }
              ]
            }
          }
        }";

        [TestInitialize]
        public void TestSetUp()
        {
            _mapper = new SqlSchemaMapper();
        }

        [TestMethod]
        public void SqlSchemaMapper_Configure_Handles_Null_Configuration()
        {
            var schema = _mapper.Configure(null);

            Assert.IsNotNull(schema);
            Assert.IsNotNull(schema.Database);
        }

        [TestMethod]
        public void SqlSchemaMapper_Configure_Deserializes_Basic_Configuration()
        {
            var schema = _mapper.Configure("{}");

            Assert.IsNotNull(schema);
            Assert.IsNotNull(schema.Database);
            Assert.IsNull(schema.Database.ConnectionString);
        }

        [TestMethod]
        public void SqlSchemaMapper_Configure_Deserializes_Object_Mapping()
        {
            var schema = _mapper.Configure(Json);

            Assert.IsNotNull(schema);
            Assert.IsNotNull(schema.Database);
            Assert.IsNotNull(schema.Database.ConnectionString);

            Assert.IsNotNull(schema.Mappings);
            Assert.AreEqual(1, schema.Mappings.Count);
            Assert.IsTrue(schema.Mappings.ContainsKey(ObjectTypes.Well));
        }
    }
}
