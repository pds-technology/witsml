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
            Model = new Models.Browser();
            Proxy = new WITSMLWebServiceConnection(Model.Connection.Uri, WMLSVersion.WITSML141);

            RequestControl = new RequestViewModel();
            ResultControl = new ResultViewModel();

            DisplayName = Settings.Default.PluginDisplayName;
            Model.PropertyChanged += Model_PropertyChanged;
        }

        public WITSMLWebServiceConnection Proxy { get; private set; }

        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        private Models.Browser _model;
        public Models.Browser Model
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

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                foreach (var item in Items)
                {
                    this.CloseItem(item);
                }
            }

            base.OnDeactivate(close);
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("WitsmlVersion"))
            {
                if (Model.HasWitsmlVersion)
                {
                    ((IShellViewModel)this.Parent).BreadcrumbText = string.Format("{0}/{1}", Settings.Default.PluginDisplayName, Model.WitsmlVersion);
                }
                else
                {
                    ((IShellViewModel)this.Parent).BreadcrumbText = Settings.Default.PluginDisplayName;
                }
            }
        }
    }
}
