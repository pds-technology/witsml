using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    [TestClass]
    public class ShellViewModelTests
    {
        private BootstrapperHarness bootstrapper;

        [TestInitialize]
        public void TestSetUp()
        {
            bootstrapper = new BootstrapperHarness();
        }

        [TestMethod]
        public void ShellViewModel_test()
        {
            // Get instance of IShellViewModel from bootstrapper's GetInstance
            //var viewModel = new ShellViewModel();
            //var app = new App();
            //app.Resources["bootstrapper"] = bootstrapper;

            //Assert.IsNotNull(app);
        }
    }
}
