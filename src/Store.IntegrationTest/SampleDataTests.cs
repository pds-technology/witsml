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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store
{
    [TestClass]
    public class SampleDataTests
    {
        private DevKit141Aspect _devKit;
        private string _dataDir;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _dataDir =  new DirectoryInfo(@".\TestData").FullName;
        }

        /// <summary>
        /// Tests logs can be added from XML file
        /// </summary>
        [TestMethod]
        public void WitsmlDataProvider_AddToStore_Can_Add_Wells_Wellbores_Logs_From_File()
        {
            AddParents();
            AddLogs();
            AddMudLogs();
        }

        #region Helper Methods

        internal void AddSampleData(TestContext context)
        {
            TestContext = context;
            TestSetUp();
            AddParents();
            AddLogs();
            AddMudLogs();
        }

        private void AddParents()
        {
            var wellFiles = Directory.GetFiles(_dataDir, "*_Well.xml");

            foreach (var xmlfile in wellFiles)
            {
                var response = _devKit.Add_Well_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }

            var wellboreFiles = Directory.GetFiles(_dataDir, "*_Wellbore.xml");
            foreach (var xmlfile in wellboreFiles)
            {
                var response = _devKit.Add_Wellbore_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }
        }

        private void AddLogs()
        {
            var logFiles = Directory.GetFiles(_dataDir, "*_Log.xml");

            foreach (var xmlfile in logFiles)
            {
                var response = _devKit.Add_Log_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }
        }

        private void AddMudLogs()
        {
            var logFiles = Directory.GetFiles(_dataDir, "*_MudLog.xml");

            foreach (var xmlfile in logFiles)
            {
                var response = _devKit.Add_MudLog_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }
        }

        #endregion Helper Methods
    }
}
