using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using PDS.Framework;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    /// <summary>
    /// Application Bootstrapper class derived from the Caliburn Micro BootstrapperBase
    /// </summary>
    public class Bootstrapper : BootstrapperBase
    {
        // TODO: Figure out why log4net is not logging for a Bootstrapper class.
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Bootstrapper));

        /// <summary>
        /// Initializes a new instance of the application Bootstrapper
        /// </summary>
        public Bootstrapper()
        {
            Initialize();
        }

        /// <summary>
        /// A reference to the composition container for dependency injection
        /// </summary>
        public IContainer Container { get; private set; }

        protected override void Configure()
        {
            var catalog = new AggregateCatalog
            (
                AssemblySource.Instance
                    .Select(x => new AssemblyCatalog(x))
            );

            Container = ContainerFactory.Create(catalog);

            Container.Register<IWindowManager>(new WindowManager());
            Container.Register<IEventAggregator>(new EventAggregator());
        }

        protected override object GetInstance(Type service, string key)
        {
            return string.IsNullOrWhiteSpace(key)
                ? Container.Resolve(service)
                : Container.Resolve(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return Container.ResolveAll(service);
        }

        protected override void BuildUp(object instance)
        {
            Container.BuildUp(instance);
        }

        /// <summary>
        /// Selects assemblies from the applications Plugins folder 
        /// where the application plugins are loaded from.
        /// </summary>
        /// <returns>An IEnumerable of the Assemblies found in the Plugins folder</returns>
        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Plugins");
            _log.DebugFormat("Bootstrapper Assembly Path: {0}", path);

            IEnumerable<Assembly> assemblies = new[] { GetType().Assembly }
                .Union(Directory.GetFiles(path, "*.dll")
                .Select(x => Assembly.LoadFrom(x)));

            if (_log.IsDebugEnabled)
            {
                _log.DebugFormat("{0}{1}{2}", "Selected Assemblies:", Environment.NewLine, string.Join(Environment.NewLine, assemblies.Select(x => x.FullName)));
            }

            return assemblies;
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<IShellViewModel>();
        }
    }
}
