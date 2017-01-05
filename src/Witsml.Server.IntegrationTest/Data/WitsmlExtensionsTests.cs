using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
