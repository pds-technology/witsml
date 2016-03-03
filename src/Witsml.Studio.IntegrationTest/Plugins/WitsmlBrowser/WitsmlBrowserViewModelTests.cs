using System;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser
{
    [TestClass]
    public class WitsmlMainViewModelTests
    {
        const string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";
        private static string _addWellTemplate =
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" >" + Environment.NewLine +
                "<well uid=\"{0}\">" + Environment.NewLine +
                "<name>Unit Test Well {1}</name>" + Environment.NewLine +
                "<timeZone>-06:00</timeZone>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

        private static string _getWellTemplate =
            "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
            "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" >" + Environment.NewLine +
            "<well uid=\"{0}\" />" + Environment.NewLine +
            "</wells>";

        private BootstrapperHarness _bootstrapper;
        private TestRuntimeService _runtime;

        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);
            _runtime.Shell = new ShellViewModel(_runtime);
        }

        [TestMethod]
        public void TestSubmitAddToStoreForWell()
        {
            // The expected result
            var expectedUid = Guid.NewGuid().ToString();

            // Create the view model and initialize data to add a well to the store
            var vm = new MainViewModel(_runtime);
            vm.Model.Connection = new Connections.Connection() { Uri = _validWitsmlUri };
            vm.Proxy.Url = vm.Model.Connection.Uri;

            vm.XmlQuery.Text = string.Format(
                _addWellTemplate,
                expectedUid,
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            vm.Model.ReturnElementType = OptionsIn.ReturnElements.All;

            // Submit the query
            string xmlOut = string.Empty;
            string suppMsgOut = string.Empty;
            vm.SubmitQuery(Functions.AddToStore, ref xmlOut, ref suppMsgOut);

            // The same uid should be returned as the results.
            Assert.AreEqual(expectedUid, suppMsgOut);
        }

        [TestMethod]
        public void TestSubmitGetFromStoreForWell()
        {
            // The expected result
            var expectedUid = Guid.NewGuid().ToString();

            // Create the view model and initialize data to add a well to the store
            var vm = new MainViewModel(_runtime);
            vm.Model.Connection = new Connections.Connection() { Uri = _validWitsmlUri };
            vm.Proxy.Url = vm.Model.Connection.Uri;
            vm.Model.ReturnElementType = OptionsIn.ReturnElements.All;

            // Add a well to the store
            vm.XmlQuery.Text = string.Format(
                _addWellTemplate,
                expectedUid,
                DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            vm.SubmitQuery(Functions.AddToStore);

            // Retrieve the same well from the store
            string xmlOut = string.Empty;
            string suppMsgOut = string.Empty;
            vm.XmlQuery.Text = string.Format(_getWellTemplate, expectedUid);
            vm.SubmitQuery(Functions.GetFromStore, ref xmlOut, ref suppMsgOut);

            // The same uid should be returned as the results.
            Assert.IsNotNull(xmlOut);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlOut);
            Assert.IsNotNull(wellList);
            Assert.AreEqual(1, wellList.Items.Count);
            Assert.AreEqual(expectedUid, (wellList.Items[0] as Well).Uid);
        }

        [TestMethod]
        public void TestMainViewModelGetCapabilities()
        {
            var vm = new MainViewModel(_runtime);
            vm.Model.Connection = new Connections.Connection() { Uri = _validWitsmlUri };
            vm.Proxy.Url = vm.Model.Connection.Uri;
            vm.Model.WitsmlVersion = OptionsIn.DataVersion.Version141.Value;

            // Test that Cap Servers can be fetched
            vm.GetCapabilities();
            var capServerList = EnergisticsConverter.XmlToObject<CapServers>(vm.QueryResults.Text);
            Assert.IsNotNull(capServerList);
            Assert.AreEqual(OptionsIn.DataVersion.Version141.Value, capServerList.CapServer.SchemaVersion);
        }

        [TestMethod]
        public void TestSettingsViewModelGetVersions()
        {
            WITSMLWebServiceConnection proxy = new WITSMLWebServiceConnection(_validWitsmlUri, WMLSVersion.WITSML141);

            var vm = new SettingsViewModel(_runtime);
            //var requestVm = new RequestViewModel(_runtime);
            //var mainVm = new MainViewModel(_runtime);

            //mainVm.Items.Add(requestVm);
            //requestVm.Items.Add(vm);

            var versions = vm.GetVersions(proxy, _validWitsmlUri);
            Assert.IsTrue(!string.IsNullOrEmpty(versions));
        }
    }
}
