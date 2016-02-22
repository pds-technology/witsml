using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PDS.Witsml.Studio
{
    public class BootstrapperHarness : Bootstrapper
    {
        public BootstrapperHarness() : base(false)
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
}
