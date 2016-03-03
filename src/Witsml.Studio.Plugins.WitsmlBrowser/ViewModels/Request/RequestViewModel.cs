using System;
using System.Linq;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request
{
    public class RequestViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RequestViewModel));

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestViewModel"/> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        public RequestViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
        }

        public new MainViewModel Parent
        {
            get { return (MainViewModel)base.Parent; }
        }

        public Models.WitsmlSettings Model
        {
            get { return Parent.Model; }
        }

        public WITSMLWebServiceConnection Proxy
        {
            get { return Parent.Proxy; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

        internal void LoadScreens()
        {
            Items.Add(new SettingsViewModel(Runtime));
            //Items.Add(new TreeViewViewModel());
            //Items.Add(new TemplatesViewModel());
            Items.Add(new QueryViewModel(Runtime));

            ActivateItem(Items.FirstOrDefault());
        }

        protected override void OnInitialize()
        {
            _log.Debug("Loading Request View Models");

            base.OnInitialize();

            LoadScreens();
        }
    }
}
