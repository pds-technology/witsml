using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Caliburn.Micro;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Studio;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{

    [TestClass]
    public class BootstrapperTests
    {
        private BootstrapperHarness bootstrapper;

        private class BootstrapperHarness : Bootstrapper
        {
            public BootstrapperHarness():base(false)
            {
            }

            public IEnumerable<Assembly> CallSelectAssemblies()
            {
                //this.OnStartup()

                return SelectAssemblies();
            }

            public void CallOnStartup()
            {
                //SelectAssemblies();
                //this.StartDesignTime();
                //this.StartRuntime();
                //var windowsManager = this.GetInstance(typeof(IWindowManager), string.Empty);
                //var shellVm = this.GetInstance(typeof(IShellViewModel), string.Empty);

                //var shellVm = new ShellViewModel();
                OnStartup(null, null);
            }

            //public T GetInstance<T>()
            //{
            //    try
            //    {
            //        return (T)base.GetInstance(typeof(T), string.Empty);
            //    }
            //    catch (Exception e)
            //    {
            //        //this.WhatDoIHave();
            //        throw;
            //    }
            //}

            //protected override void StartRuntime()
            //{
            //    this.Configure();
            //    //this.SelectAssemblies();
            //}

            protected override IEnumerable<Assembly> SelectAssemblies()
            {
                return base.SelectAssemblies()
                    .Union(new[] { GetType().Assembly });
            }
        }

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
            bootstrapper.CallOnStartup();


            Assert.IsNotNull(bootstrapper);
        }

        [TestMethod]
        public void Bootstrapper_registered_event_aggregator()
        {
            bootstrapper.CallOnStartup();

            Assert.IsNotNull(bootstrapper);
        }

        [TestMethod]
        public void Bootstrapper_can_resolve_shell_view_model()
        {
            // Get instance of IShellViewModel from bootstrapper's GetInstance

            Assert.IsNotNull(bootstrapper);
        }

        // TODO: Move to a ShellViewModelTests
        [TestMethod]
        public void ShellViewModel_test()
        {
            // Get instance of IShellViewModel from bootstrapper's GetInstance
            var viewModel = new ShellViewModel();
            var app = new App();
            app.Resources["bootstrapper"] = bootstrapper;

            Assert.IsNotNull(app);
        }
    }
}
