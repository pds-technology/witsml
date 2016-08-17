//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Well141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public class Well141DataAdapterDeleteTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Delete_Full_Well()
        {
            _well = _devKit.CreateFullWell();
            _well.Uid = _devKit.Uid();

            // Add well
            AddWell(_well);

            // Assert well is added
            GetWell(_well);

            // Delete well
            var delete = new Well {Uid = _well.Uid};
            DeleteWell(delete);

            // Assert the well has been deleted
            var query = new Well { Uid = _well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Delete_Full_Well_With_Case_Insensitive_Uid()
        {
            var uid = _devKit.Uid();
            _well = _devKit.CreateFullWell();
            _well.Uid = "w" + uid;

            // Add well
            AddWell(_well);

            // Assert well is added
            GetWell(_well);

            // Delete well
            var delete = new Well { Uid = "W" + uid };
            DeleteWell(delete);

            // Assert the well has been deleted
            var query = new Well { Uid = _well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            _well.Country = "USA";
            _well.DateTimeSpud = DateTimeOffset.UtcNow;

            // Add well
            AddWell(_well);

            // Partial delete well
            const string delete = "<country /><dTimSpud />";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the well elements has been deleted
            var result = GetWell(_well);
            Assert.IsNull(result.Country);
            Assert.IsNull(result.DateTimeSpud);
        }

        private void AddWell(Well well)
        {
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private void DeleteWell(Well well)
        {
            var response = _devKit.Delete<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Well GetWell(Well well)
        {
            var query = new Well { Uid = well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }
    }
}
