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
using System.Linq;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// CapServer131Provider tests.
    /// </summary>
    [TestClass]
    public class CapServer131ProviderTests
    {
        private DevKit131Aspect _devKit;
        private CapServer131Provider _capServer131Provider;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit131Aspect(TestContext);
            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            _capServer131Provider = _devKit.Store.CapServerProviders.FirstOrDefault() as CapServer131Provider;
        }

        [TestMethod]
        public void CapServer131Provider_DataSchemaVersion_Can_Get_DataVersion()
        {
            Assert.IsNotNull(_capServer131Provider);
            Assert.AreEqual("1.3.1.1", _capServer131Provider.DataSchemaVersion);
        }

        [TestMethod]
        public void CapServer131Provider_Providers_Can_Get_Providers()
        {
            Assert.IsNotNull(_capServer131Provider.Providers);
            Assert.AreEqual(7, _capServer131Provider.Providers.Count());
        }

        [TestMethod]
        public void CapServer131Provider_ToXml_Can_Get_Server_Capabilities_As_Xml()
        {
            var capServerXml = _capServer131Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML131.CapServers>(capServerXml).CapServer;

            Assert.AreEqual("1.3.1", capServerObject.ApiVers);
            Assert.AreEqual(Properties.Settings.Default.DefaultServerName, capServerObject.Name, "Server Name");
            Assert.AreEqual(Properties.Settings.Default.DefaultVendorName, capServerObject.Vendor, "Vendor");
            Assert.AreEqual(Properties.Settings.Default.DefaultServerDescription, capServerObject.Description, "Server Description");
            Assert.AreEqual("1.3.1.1", capServerObject.SchemaVersion, "Schema Version");
            Assert.AreEqual(Properties.Settings.Default.DefaultContactName, capServerObject.Contact.Name, "Contact Name");
            Assert.AreEqual(Properties.Settings.Default.DefaultContactEmail, capServerObject.Contact.Email, "Contact Email");
            Assert.AreEqual(Properties.Settings.Default.DefaultContactPhone, capServerObject.Contact.Phone, "Contact Phone");

            Assert.AreEqual(4, capServerObject.Function.Count, "Server Functions");
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_AddToStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_GetFromStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_UpdateInStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_DeleteFromStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            _devKit = null;
        }
    }
}
