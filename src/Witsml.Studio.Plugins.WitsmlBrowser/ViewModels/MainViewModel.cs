using System;
using System.Linq;
using System.Windows;
using Caliburn.Micro;
using Energistics.DataAccess;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.Properties;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Request;
using PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels.Result;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.WitsmlBrowser.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.AllActive, IPluginViewModel
    {
        public MainViewModel()
        {
            Model = new Models.WitsmlSettings();
            Proxy = CreateProxy();

            RequestControl = new RequestViewModel();
            ResultControl = new ResultViewModel();

            DisplayName = Settings.Default.PluginDisplayName;
            Model.PropertyChanged += Model_PropertyChanged;
        }

        public WITSMLWebServiceConnection Proxy { get; private set; }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        private Models.WitsmlSettings _model;
        public Models.WitsmlSettings Model
        {
            get { return _model; }
            set
            {
                if (!ReferenceEquals(_model, value))
                {
                    _model = value;
                    NotifyOfPropertyChange(() => Model);
                }
            }
        }

        public RequestViewModel RequestControl { get; set; }

        public ResultViewModel ResultControl { get; set; }

        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and witsml version.
        /// </summary>
        /// <returns></returns>
        internal WITSMLWebServiceConnection CreateProxy()
        {
            return new WITSMLWebServiceConnection(Model.Connection.Uri, GetWitsmlVersionEnum());
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            Items.Add(RequestControl);
            Items.Add(ResultControl);
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("WitsmlVersion"))
            {
                App.Current.Shell().BreadcrumbText = !string.IsNullOrEmpty(Model.WitsmlVersion)
                    ? string.Format("{0}/{1}", Settings.Default.PluginDisplayName, Model.WitsmlVersion)
                    : App.Current.Shell().BreadcrumbText = Settings.Default.PluginDisplayName;

                // Reset the Proxy when the version changes
                Proxy = CreateProxy();
            }
        }

        /// <summary>
        /// Gets the witsml version enum.
        /// </summary>
        /// <returns>
        /// The WMLSVersion enum value based on the current value of Model.WitsmlVersion.
        /// If Model.WitsmlVersion has not been established the the default is WMLSVersion.WITSML141.
        /// </returns>
        private WMLSVersion GetWitsmlVersionEnum()
        {
            return Model.WitsmlVersion != null && Model.WitsmlVersion.Equals("1.3.1.1") 
                ? WMLSVersion.WITSML131 
                : WMLSVersion.WITSML141;
        }
    }
}
