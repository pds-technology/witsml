using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    /// <summary>
    /// Unit tests for the Witsml Studio application shell.
    /// </summary>
    [TestClass]
    public class ShellViewModelTests
    {
        private BootstrapperHarness bootstrapper;

        /// <summary>
        /// Initialization before each test.
        /// </summary>
        [TestInitialize]
        public void TestSetUp()
        {
            bootstrapper = new BootstrapperHarness();
        }

        /// <summary>
        /// Tests that the ShellViewModel was created.
        /// </summary>
        [TestMethod]
        public void ShellViewModel_test_shell_created()
        {
            var app = new App();
            app.Resources["bootstrapper"] = bootstrapper;

            bootstrapper.CallOnStartup();

            // Test if Shell exists?
            var shell = app.Shell() as ShellViewModel;
            Assert.IsNotNull(shell);
        }

        /// <summary>
        /// Tests that all of the expected IPluginViewModels were loaded.
        /// </summary>
        [TestMethod]
        public void ShellViewModel_test_all_view_models_loaded()
        {
            var totalScreens = 3;

            var app = new App();
            app.Resources["bootstrapper"] = bootstrapper;

            bootstrapper.CallOnStartup();

            // Test that all unit test IPluginViewModels are loaded
            var shell = app.Shell() as ShellViewModel;
            var screenCount = shell.Items.Count;
            Assert.AreEqual(totalScreens, screenCount);
        }

        /// <summary>
        /// Tests that all of the IPluginViewModels were loaded in the correct display order.
        /// </summary>
        [TestMethod]
        public void ShellViewModel_test_view_model_order()
        {
            var app = new App();
            app.Resources["bootstrapper"] = bootstrapper;

            bootstrapper.CallOnStartup();

            // Verify that the view models are in the expected order.
            var shell = app.Shell() as ShellViewModel;
            var screenCount = shell.Items.Count;
            for (var i = 0; i < screenCount; i++)
            {
                Assert.AreEqual(((i + 1) * 100).ToString(), shell.Items[i].DisplayName);
            }
        }
    }
}
