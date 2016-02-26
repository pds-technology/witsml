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
            Proxy = new WITSMLWebServiceConnection(Model.Connection.Uri, WMLSVersion.WITSML141);

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
            }
        }
    }
}
