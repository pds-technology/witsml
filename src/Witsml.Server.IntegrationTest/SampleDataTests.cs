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

using System.IO;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server
{
    [TestClass]
    public class SampleDataTests
    {
        private DevKit141Aspect DevKit;
        private string DataDir;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            DataDir =  new DirectoryInfo(@".\TestData").FullName;
        }

        /// <summary>
        /// Add a <see cref="Well"/>, <see cref="Wellbore"/> and <see cref="Log"/> to the store
        /// </summary>
        [TestMethod]
        public void Add_data()
        {
            Add_parents();
            Add_logs();
        }

        #region Helper Methods

        private void Add_parents()
        {
            string[] wellFiles = Directory.GetFiles(DataDir, "*_Well.xml");

            foreach (string xmlfile in wellFiles)
            {
                var response = DevKit.Add_Well_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }

            string[] wellboreFiles = Directory.GetFiles(DataDir, "*_Wellbore.xml");
            foreach (string xmlfile in wellboreFiles)
            {
                var response = DevKit.Add_Wellbore_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }
        }

        private void Add_logs()
        {
            string[] logFiles = Directory.GetFiles(DataDir, "*_Log.xml");

            foreach (string xmlfile in logFiles)
            {
                var response = DevKit.Add_Log_from_file(xmlfile);
                if (response != null)
                {
                    Assert.AreEqual((short)ErrorCodes.Success, response.Result);
                }
            }
        }
        #endregion Helper Methods
    }
}
