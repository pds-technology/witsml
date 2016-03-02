using System;
using Caliburn.Micro;
using Energistics.DataAccess;
using ICSharpCode.AvalonEdit.Document;
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
            XmlQuery = new TextDocument();
            QueryResults = new TextDocument();

            // TODO: Remove after testing
            XmlQuery.Text =
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" />";

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

        private TextDocument _xmlQuery;
        public TextDocument XmlQuery
        {
            get { return _xmlQuery; }
            set
            {
                if (!string.Equals(_xmlQuery, value))
                {
                    _xmlQuery = value;
                    NotifyOfPropertyChange(() => XmlQuery);
                }
            }
        }

        private TextDocument _queryResults;
        public TextDocument QueryResults
        {
            get { return _queryResults; }
            set
            {
                if (!string.Equals(_queryResults, value))
                {
                    _queryResults = value;
                    NotifyOfPropertyChange(() => QueryResults);
                }
            }
        }

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
                    : Settings.Default.PluginDisplayName;

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
            return Model.WitsmlVersion != null && Model.WitsmlVersion.Equals(OptionsIn.DataVersion.Version131.Value) 
                ? WMLSVersion.WITSML131 
                : WMLSVersion.WITSML141;
        }
    }
}
