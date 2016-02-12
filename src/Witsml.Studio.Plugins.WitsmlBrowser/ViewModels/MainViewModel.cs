using System.Linq;
using System.Windows;
using Caliburn.Micro;
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
            DisplayName = Settings.Default.PluginDisplayName;
            Model = new Models.Browser();

            Model.PropertyChanged += Model_PropertyChanged;
        }

        private void Model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // TODO: Fix.  Not working!
            if (e.PropertyName.Equals("WitsmlVersion"))
            {
                if (Model.HasWitsmlVersion)
                {
                    ((Conductor<IScreen>.Collection.OneActive)this.Parent).DisplayName = string.Format("{0}/{1}", Settings.Default.PluginDisplayName, Model.WitsmlVersion);
                }
                else
                {
                    ((Conductor<IScreen>.Collection.OneActive)this.Parent).DisplayName = Settings.Default.PluginDisplayName;
                }
            }
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

        private RequestViewModel _requestControl;
        public RequestViewModel RequestControl
        {
            get
            {
                return _requestControl;
            }
            set
            {
                if (!ReferenceEquals(_requestControl, value))
                {
                    _requestControl = value;
                    NotifyOfPropertyChange(() => RequestControl);
                }
            }
        }

        private ResultViewModel _resultControl;
        public ResultViewModel ResultControl
        {
            get
            {
                return _resultControl;
            }
            set
            {
                if (!ReferenceEquals(_resultControl, value))
                {
                    _resultControl = value;
                    NotifyOfPropertyChange(() => ResultControl);
                }
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ActivateItem(new RequestViewModel());
            Items.Add(new ResultViewModel());

            RequestControl = (RequestViewModel)Items[0];
            ResultControl = (ResultViewModel)Items[1];
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                foreach (var child in Items.ToArray())
                {
                    this.CloseItem(child);
                }
            }

            base.OnDeactivate(close);
        }


        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }
    }
}
