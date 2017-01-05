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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    [TestClass]
    public class WitsmlExtensionsTests
    {
        [TestMethod]
        public void WitsmlExtensions_GetLogMaxNodes_GetFromStore_Returns_Value()
        {
            // Change the setting for LogMaxDataNodesGet to something we can check for
            WitsmlSettings.LogMaxDataNodesGet = 20;

            // Check that GetLogMaxNodes returns what is expected for GetFromStore
            Assert.AreEqual(WitsmlSettings.LogMaxDataNodesGet, Functions.GetFromStore.GetLogMaxNodes());
        }

        [TestMethod]
        public void WitsmlExtensions_GetLogMaxNodes_DeleteFromStore_Returns_Value()
        {
            // Change the setting for LogMaxDataNodesDelete to something we can check for
            WitsmlSettings.LogMaxDataNodesDelete = 20;

            // Check that GetLogMaxNodes returns what is expected for DeleteFromStore
            Assert.AreEqual(WitsmlSettings.LogMaxDataNodesDelete, Functions.DeleteFromStore.GetLogMaxNodes());
        }
    }
}
