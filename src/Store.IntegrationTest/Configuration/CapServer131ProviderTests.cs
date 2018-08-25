//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Energistics.DataAccess.WITSML131;

namespace PDS.WITSMLstudio.Store.Configuration
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
        }

        [TestMethod]
        public void CapServer131Provider_ToXml_Can_Get_Server_Capabilities_As_Xml()
        {
            var capServerXml = _capServer131Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<CapServers>(capServerXml).CapServer;

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
            var capServerObject = GetCapServerObject();

            var addToStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.AddToStore.ToString())).ToArray();
            Assert.IsNotNull(addToStore);

            addToStore.FirstOrDefault()?.DataObject.ForEach(
                    dataObject =>
                    {
                        Assert.IsTrue(_capServer131Provider.IsSupported(Functions.AddToStore, dataObject), dataObject);
                    });

            Assert.IsFalse(_capServer131Provider.IsSupported(Functions.AddToStore, ObjectTypes.Unknown), ObjectTypes.Unknown);
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_GetFromStore_Can_Check_Supported_Object_Type()
        {
            var capServerObject = GetCapServerObject();

            var getFromStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.GetFromStore.ToString())).ToArray();
            Assert.IsNotNull(getFromStore);

            getFromStore.FirstOrDefault()?.DataObject.ForEach(
                    dataObject =>
                    {
                        Assert.IsTrue(_capServer131Provider.IsSupported(Functions.GetFromStore, dataObject), dataObject);
                    });

            Assert.IsFalse(_capServer131Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Unknown), ObjectTypes.Unknown);
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_UpdateInStore_Can_Check_Supported_Object_Type()
        {
            var capServerObject = GetCapServerObject();

            var updateInStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.UpdateInStore.ToString())).ToArray();
            Assert.IsNotNull(updateInStore);

            updateInStore.FirstOrDefault()?.DataObject.ForEach(
                    dataObject =>
                    {
                        Assert.IsTrue(_capServer131Provider.IsSupported(Functions.UpdateInStore, dataObject), dataObject);
                    });

            Assert.IsFalse(_capServer131Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Unknown), ObjectTypes.Unknown);
        }

        [TestMethod]
        public void CapServer131Provider_IsSupported_DeleteFromStore_Can_Check_Supported_Object_Type()
        {
            var capServerObject = GetCapServerObject();

            var deleteFromStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.DeleteFromStore.ToString())).ToArray();
            Assert.IsNotNull(deleteFromStore);

            deleteFromStore.FirstOrDefault()?.DataObject.ForEach(
                    dataObject =>
                    {
                        Assert.IsTrue(_capServer131Provider.IsSupported(Functions.DeleteFromStore, dataObject), dataObject);
                    });

            Assert.IsFalse(_capServer131Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Unknown), ObjectTypes.Unknown);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            _devKit = null;
        }

        private CapServer GetCapServerObject()
        {
            var capServerXml = _capServer131Provider.ToXml();

            return Energistics.DataAccess.EnergisticsConverter.XmlToObject<CapServers>(capServerXml).CapServer;
        }

    }
}
