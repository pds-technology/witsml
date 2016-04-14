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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141ValidatorTests
    {
        private DevKit141Aspect DevKit;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DevKit = null;
        }

        [TestMethod]
        public void Test_error_code_438_recurring_elements_inconsistent_selection()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var crs1 = DevKit.WellCRS("geog1", null);
            var crs2 = DevKit.WellCRS(null, "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void Test_error_code_439_recurring_elements_empty_value()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var crs1 = DevKit.WellCRS("geog1", string.Empty);
            var crs2 = DevKit.WellCRS("proj1", "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }
    }
}
