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

using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using Shouldly;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class DatabaseProviderTests
    {
        private SqlSchemaMapper _schemaMapper;

        [TestInitialize]
        public void TestSetUp()
        {
            var container = ContainerFactory.Create();
            _schemaMapper = new SqlSchemaMapper(container);
        }

        [TestMethod]
        public void DatabaseProvider_GetDatabase_Initializes_Successfully()
        {
            var provider = new DatabaseProvider(_schemaMapper);

            Assert.IsNotNull(provider.SchemaMapper);
            Assert.IsNotNull(provider.SchemaMapper.Schema);

            using (var database = provider.GetDatabase())
            {
                Assert.IsNotNull(database);
            }
        }

        [TestMethod]
        public void DatabaseProvider_GetDatabase_Connection_Error()
        {
            var expected = "Data Source=(local)\\SQLEXPRESS;Initial Catalog=Invalid_Connection_String;";
            var provider = new DatabaseProvider(_schemaMapper, expected);

            Assert.IsNotNull(provider.SchemaMapper);
            Assert.IsNotNull(provider.SchemaMapper.Schema);
            Assert.IsNotNull(provider.SchemaMapper.Schema.Database);
            Assert.AreEqual(expected, provider.SchemaMapper.Schema.Database.ConnectionString);

            Should.Throw<SqlException>(() =>
            {
                using (var database = provider.GetDatabase())
                {
                    database.ExecuteScalar<int>("select null");
                }
            });
        }
    }
}
