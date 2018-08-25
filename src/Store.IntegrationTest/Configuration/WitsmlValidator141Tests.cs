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
using System.Linq;

namespace PDS.WITSMLstudio.Store.Configuration
{
    [TestClass]
    public class WitsmlValidator141Tests
    {
        private DevKit141Aspect DevKit;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_ReturnElement_HeaderOnly_For_Growing_Object()
        {
            var query = new Log { Uid = "", Name = "" };
            var response = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, optionsIn: OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void WitsmlValidator_GetFromStore_ReturnElement_StationLocationOnly_For_Trajectory()
        {
            var query = new Trajectory { Uid = "", Name = "" };
            var response = DevKit.Get<TrajectoryList, Trajectory>(DevKit.List(query), ObjectTypes.Trajectory, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void WitsmlValidator_GetFromStore_ReturnElement_LatestChangeOnly_For_ChangeLog()
        {
            var query = new ChangeLog { Uid = "", NameWell = ""};
            var response = DevKit.Get<ChangeLogList, ChangeLog>(DevKit.List(query), ObjectTypes.ChangeLog, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
