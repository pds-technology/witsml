using System.Linq;
using Caliburn.Micro;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{

    [TestClass]
    public class BootstrapperTests
    {
        private BootstrapperHarness bootstrapper;

        [TestInitialize]
        public void TestSetUp()
        {
            bootstrapper = new BootstrapperHarness();
        }

        [TestMethod]
        public void Bootstrapper_can_load_assemblies()
        {
            var thisAssembly = bootstrapper.CallSelectAssemblies()
                .FirstOrDefault(a => a == GetType().Assembly);
            
            Assert.IsNotNull(thisAssembly);
        }

        [TestMethod]
        public void Bootstrapper_registered_window_manager()
        {
            // Get instance of IWindowManager from bootstrapper's GetInstance
            var windownManager = bootstrapper.CallGetInstance(typeof(IWindowManager));

            Assert.IsNotNull(windownManager);
        }

        [TestMethod]
        public void Bootstrapper_registered_event_aggregator()
        {
            // Get instance of IEventAggregator from bootstrapper's GetInstance
            var eventAggregator = bootstrapper.CallGetInstance(typeof(IEventAggregator));

            Assert.IsNotNull(eventAggregator);
        }

        [TestMethod]
        public void Bootstrapper_can_resolve_shell_view_model()
        {
            // Get instance of IShellViewModel from bootstrapper's GetInstance
            var eventAggregator = bootstrapper.CallGetInstance(typeof(IShellViewModel));

            Assert.IsNotNull(bootstrapper);
        }
    }
}
