using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.ViewModels
{
    /// <summary>
    /// Unit tests for the Witsml Studio application shell.
    /// </summary>
    [TestClass]
    public class ShellViewModelTests
    {
        private BootstrapperHarness _bootstrapper;
        private TestRuntimeService _runtime;
        private ShellViewModel _viewModel;

        [TestInitialize]
        public void TestSetUp()
        {
            _bootstrapper = new BootstrapperHarness();
            _runtime = new TestRuntimeService(_bootstrapper.Container);
            _viewModel = new ShellViewModel(_runtime);
        }

        /// <summary>
        /// Tests that all of the expected IPluginViewModels were loaded.
        /// </summary>
        [TestMethod]
        public void Can_load_shellViewModel_plugins()
        {
            _viewModel.LoadPlugins();

            Assert.AreEqual(3, _viewModel.Items.Count);
        }

        /// <summary>
        /// Tests that all of the IPluginViewModels were loaded in the correct display order.
        /// </summary>
        [TestMethod]
        public void ShellViewModel_plugins_are_displayed_in_ascending_order()
        {
            _viewModel.LoadPlugins();

            var actual = _viewModel.Items.ToArray();

            var expected = _viewModel.Items.Cast<IPluginViewModel>()
                .OrderBy(x => x.DisplayOrder)
                .ToArray();

            for (int i=0; i<actual.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ShellViewModel_status_is_set()
        {
            _viewModel.LoadPlugins();

            Assert.AreEqual("Ready.", _viewModel.StatusBarText);
        }

        [TestMethod]
        public void ShellViewModel_breadcrumb_is_first_plugin_displayName()
        {
            _viewModel.LoadPlugins();
           
            // Test that the Shell breadcrumb is the same as the first plugin
            Assert.AreEqual(_viewModel.Items[0].DisplayName, _viewModel.BreadcrumbText);
        }
    }
}
