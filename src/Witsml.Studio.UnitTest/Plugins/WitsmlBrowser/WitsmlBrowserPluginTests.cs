using Energistics.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser
{
    [TestClass]
    public class WitsmlBrowserPluginTests
    {
        private MainViewModel _mainViewModel;

        [TestInitialize]
        public void TestSetup()
        {
            _mainViewModel = new MainViewModel();
        }

        [TestMethod]
        public void TestMainViewModelScreensLoaded()
        {
            _mainViewModel.LoadScreens();
            
            Assert.AreEqual(2, _mainViewModel.Items.Count);
        }

        [TestMethod]
        public void TestGetWitsmlVersionEnum()
        {
            // Test version 131
            Assert.AreEqual(WMLSVersion.WITSML131, _mainViewModel.GetWitsmlVersionEnum(OptionsIn.DataVersion.Version131.Value));

            // Test version 141
            Assert.AreEqual(WMLSVersion.WITSML141, _mainViewModel.GetWitsmlVersionEnum(OptionsIn.DataVersion.Version141.Value));
        }

        [TestMethod]
        public void TestCreateProxy()
        {
            Assert.IsNotNull(_mainViewModel.CreateProxy());
        }

        [TestMethod]
        public void TestRequestViewModelScreensLoaded()
        {
            var requestViewModel = new RequestViewModel();

            requestViewModel.LoadScreens();

            Assert.AreEqual(2, requestViewModel.Items.Count);
        }

        [TestMethod]
        public void TestQueryViewModelFunctionTextToEnum()
        {
            var queryViewModel = new QueryViewModel();

            Assert.AreEqual(Functions.AddToStore, queryViewModel.FunctionTextToEnum("Add"));
            Assert.AreEqual(Functions.UpdateInStore, queryViewModel.FunctionTextToEnum("Update"));
            Assert.AreEqual(Functions.DeleteFromStore, queryViewModel.FunctionTextToEnum("Delete"));
            Assert.AreEqual(Functions.GetFromStore, queryViewModel.FunctionTextToEnum("Get"));
        }
    }
}
