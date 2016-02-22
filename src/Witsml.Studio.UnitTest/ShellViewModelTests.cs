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
        public void ShellViewModel_test_view_model_order()
        {
            var firstViewText = "100";

            var app = new App();
            app.Resources["bootstrapper"] = bootstrapper;

            bootstrapper.CallOnStartup();

            var shell = app.Shell();
            Assert.IsNotNull(shell);

            var breadcrumbText = shell.BreadcrumbText;
            Assert.AreEqual(firstViewText, breadcrumbText);
        }
    }
}
