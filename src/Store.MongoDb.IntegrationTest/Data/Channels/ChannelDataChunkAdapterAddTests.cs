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

using System.IO;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    [TestClass]
    public class ChannelDataChunkAdapterAddTests
    {
        private DevKit141Aspect DevKit;
        private Well Well;
        private Wellbore Wellbore;
        private Log Log;
        private string _testDataDir;
        private string _exceedFileFormat = "Test-exceed-max-doc-size-{0}-0001.xml";

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            _testDataDir = new DirectoryInfo(@".\TestData").FullName;

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            Well = new Well { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone };

            Wellbore = new Wellbore()
            {
                NameWell = Well.Name,
                Name = DevKit.Name("Wellbore 01")
            };

            Log = new Log()
            {
                NameWell = Well.Name,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.LogMaxDataPointsGet = DevKitAspect.DefaultLogMaxDataPointsGet;
            WitsmlSettings.LogMaxDataPointsUpdate = DevKitAspect.DefaultLogMaxDataPointsAdd;
            WitsmlSettings.LogMaxDataPointsAdd = DevKitAspect.DefaultLogMaxDataPointsUpdate;
            WitsmlSettings.LogMaxDataPointsDelete = DevKitAspect.DefaultLogMaxDataPointsDelete;
            WitsmlSettings.LogMaxDataNodesGet = DevKitAspect.DefaultLogMaxDataNodesGet;
            WitsmlSettings.LogMaxDataNodesAdd = DevKitAspect.DefaultLogMaxDataNodesAdd;
            WitsmlSettings.LogMaxDataNodesUpdate = DevKitAspect.DefaultLogMaxDataNodesUpdate;
            WitsmlSettings.LogMaxDataNodesDelete = DevKitAspect.DefaultLogMaxDataNodesDelete;
        }

        [TestMethod, Description("Test that a document larger than 16MB can be added to MongoDB")]
        public void ChannelDataChunkAdapter_AddToStore_Max_Document_Size_Exceeded_Successfully()
        {
            // Adjust Points and Nodes for large file
            WitsmlSettings.LogMaxDataPointsAdd = 5000000;
            WitsmlSettings.LogMaxDataNodesAdd = 15000;

            // Load Well from file and assert success response
            var response = DevKit.Add_Well_from_file(
                BuildDataFileName(string.Format(_exceedFileFormat, "well")));
            // There is no response if the Well already exists in the database
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            // Load Wellbore from file and assert success response
            response = DevKit.Add_Wellbore_from_file(
                BuildDataFileName(string.Format(_exceedFileFormat, "wellbore")));
            // There is no response if the Wellbore already exists in the database
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            // Load Log from file that is larger than 16MB and assert success response
            response = DevKit.Add_Log_from_file(
                BuildDataFileName(string.Format(_exceedFileFormat, "log")));
            // Log should always succeed 
            if (response != null)
            {
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        private string BuildDataFileName(string filename)
        {
            return Path.Combine(_testDataDir, filename);
        }
    }
}
