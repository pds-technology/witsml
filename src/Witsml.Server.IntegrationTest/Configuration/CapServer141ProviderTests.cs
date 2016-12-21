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
    /// CapServer141Provider tests.
    /// </summary>
    [TestClass]
    public class CapServer141ProviderTests
    {
        private DevKit141Aspect _devKit;
        private CapServer141Provider _capServer141Provider;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);
            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _capServer141Provider = _devKit.Store.CapServerProviders.FirstOrDefault() as CapServer141Provider;

        }

        [TestMethod]
        public void CapServer141Provider_DataSchemaVersion_Can_Get_DataVersion()
        {
            Assert.IsNotNull(_capServer141Provider);
            Assert.AreEqual("1.4.1.1", _capServer141Provider.DataSchemaVersion);
        }

        [TestMethod]
        public void CapServer141Provider_Providers_Can_Get_Providers()
        {
            Assert.IsNotNull(_capServer141Provider.Providers);
            Assert.AreEqual(8, _capServer141Provider.Providers.Count());
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_As_Xml()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            Assert.AreEqual("1.4.1", capServerObject.ApiVers);
            Assert.AreEqual(Properties.Settings.Default.DefaultServerName, capServerObject.Name, "Server Name");
            Assert.AreEqual(Properties.Settings.Default.DefaultVendorName, capServerObject.Vendor, "Vendor");
            Assert.AreEqual(Properties.Settings.Default.DefaultServerDescription, capServerObject.Description, "Server Description");
            Assert.AreEqual("1.4.1.1", capServerObject.SchemaVersion, "Schema Version");
            Assert.AreEqual(Properties.Settings.Default.DefaultContactName, capServerObject.Contact.Name, "Contact Name");
            Assert.AreEqual(Properties.Settings.Default.DefaultContactEmail, capServerObject.Contact.Email, "Contact Email");
            Assert.AreEqual(Properties.Settings.Default.DefaultContactPhone, capServerObject.Contact.Phone, "Contact Phone");

            Assert.IsNotNull(capServerObject.MaxRequestLatestValues);
            Assert.AreEqual(Properties.Settings.Default.MaxRequestLatestValues, capServerObject.MaxRequestLatestValues.Value, "maxRequestLatestValue");
            Assert.IsNotNull(capServerObject.SupportUomConversion);
            Assert.IsTrue(!string.IsNullOrEmpty(capServerObject.CompressionMethod));

            Assert.AreEqual(4, capServerObject.Function.Count, "Server Functions");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_AddToStore_With_Object_Contraints_For_Log()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var addToStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.AddToStore.ToString())).ToArray();
            Assert.IsNotNull(addToStore);
            var logDataObject = addToStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Log).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataPointsAdd, logDataObject.FirstOrDefault()?.MaxDataPoints, "MaxDataPoints");
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataNodesAdd, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_AddToStore_With_Object_Contraints_For_Trajectory()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var addToStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.AddToStore.ToString())).ToArray();
            Assert.IsNotNull(addToStore);
            var logDataObject = addToStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Trajectory).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.TrajectoryMaxDataNodesAdd, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_UpdateInStore_With_Object_Contraints_For_Log()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var updateInStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.UpdateInStore.ToString())).ToArray();
            Assert.IsNotNull(updateInStore);
            var logDataObject = updateInStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Log).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataPointsUpdate, logDataObject.FirstOrDefault()?.MaxDataPoints, "MaxDataPoints");
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataNodesUpdate, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_UpdateInStore_With_Object_Contraints_For_Trajectory()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var updateInStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.UpdateInStore.ToString())).ToArray();
            Assert.IsNotNull(updateInStore);
            var logDataObject = updateInStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Trajectory).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.TrajectoryMaxDataNodesUpdate, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_DeleteFromStore_With_Object_Contraints_For_Log()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var deleteFromStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.DeleteFromStore.ToString())).ToArray();
            Assert.IsNotNull(deleteFromStore);
            var logDataObject = deleteFromStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Log).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataPointsDelete, logDataObject.FirstOrDefault()?.MaxDataPoints, "MaxDataPoints");
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataNodesDelete, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_DeleteFromStore_With_Object_Contraints_For_Trajectory()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var deleteFromStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.DeleteFromStore.ToString())).ToArray();
            Assert.IsNotNull(deleteFromStore);
            var logDataObject = deleteFromStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Trajectory).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.TrajectoryMaxDataNodesDelete, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_GetFromStore_With_Object_Contraints_For_Log()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var getFromStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.GetFromStore.ToString())).ToArray();
            Assert.IsNotNull(getFromStore);
            var logDataObject = getFromStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Log).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataPointsGet, logDataObject.FirstOrDefault()?.MaxDataPoints, "MaxDataPoints");
            Assert.AreEqual(Properties.Settings.Default.LogMaxDataNodesGet, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_ToXml_Can_Get_Server_Capabilities_For_GetFromStore_With_Object_Contraints_For_Trajectory()
        {
            var capServerXml = _capServer141Provider.ToXml();

            Assert.IsTrue(capServerXml != string.Empty);

            var capServerObject = Energistics.DataAccess.EnergisticsConverter.XmlToObject<Energistics.DataAccess.WITSML141.CapServers>(capServerXml).CapServer;

            var getFromStore = capServerObject.Function.Where(n => n.Name.EndsWith(Functions.GetFromStore.ToString())).ToArray();
            Assert.IsNotNull(getFromStore);
            var logDataObject = getFromStore.FirstOrDefault()?.DataObject.Where(d => d.Value == ObjectTypes.Trajectory).ToArray();
            Assert.IsNotNull(logDataObject);
            Assert.AreEqual(Properties.Settings.Default.TrajectoryMaxDataNodesGet, logDataObject.FirstOrDefault()?.MaxDataNodes, "MaxDataNodes");
        }

        [TestMethod]
        public void CapServer141Provider_IsSupported_AddToStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.AddToStore, ObjectTypes.Attachment), ObjectTypes.Attachment);
        }

        [TestMethod]
        public void CapServer141Provider_IsSupported_GetFromStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.GetFromStore, ObjectTypes.Attachment), ObjectTypes.Attachment);
        }

        [TestMethod]
        public void CapServer141Provider_IsSupported_UpdateInStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.UpdateInStore, ObjectTypes.Attachment), ObjectTypes.Attachment);
        }

        [TestMethod]
        public void CapServer141Provider_IsSupported_DeleteFromStore_Can_Check_Supported_Object_Type()
        {
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Well), ObjectTypes.Well);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Wellbore), ObjectTypes.Wellbore);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Message), ObjectTypes.Message);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Log), ObjectTypes.Log);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Rig), ObjectTypes.Rig);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Trajectory), ObjectTypes.Trajectory);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.WbGeometry), ObjectTypes.WbGeometry);
            Assert.IsTrue(_capServer141Provider.IsSupported(Functions.DeleteFromStore, ObjectTypes.Attachment), ObjectTypes.Attachment);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            _devKit = null;
        }
    }
}
