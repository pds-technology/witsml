using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using PDS.Framework;
using PDS.Witsml.Studio.Properties;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio
{
    /// <summary>
    /// Configures the composition container to be used for dependecy injection.
    /// </summary>
    public class Bootstrapper : BootstrapperBase
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(Bootstrapper));
        private static readonly string PluginsFolderName = Settings.Default.PluginsFolderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class.
        /// </summary>
        public Bootstrapper() : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class.
        /// </summary>
        /// <param name="useApplication">Set this to false when hosting Caliburn.Micro inside and Office or WinForms application. The default is true.</param>
        public Bootstrapper(bool useApplication = true) : base(useApplication)
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
            object instance = string.IsNullOrWhiteSpace(key)
                ? Container.Resolve(service)
                : Container.Resolve(service, key);

            return instance;
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
            var path = Path.Combine(Environment.CurrentDirectory, PluginsFolderName);
            _log.DebugFormat("Bootstrapper Assembly Path: {0}", path);

            // Ensure that the plugins folder exists so we don't get an error
            Directory.CreateDirectory(path);

            IEnumerable<Assembly> assemblies = new[] { typeof(Bootstrapper).Assembly }
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
