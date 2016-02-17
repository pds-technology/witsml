using System.Collections.Generic;
using Caliburn.Micro;
using Energistics;
using Energistics.Common;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Properties;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel
    {
        private const string RootUri = "/";
        private EtpClient _client;

        public MainViewModel()
        {
            DisplayName = Settings.Default.PluginDisplayName;
            Resources = new BindableCollection<ResourceViewModel>();
            Model = new EtpSettings();
        }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        public EtpSettings Model { get; private set; }

        public BindableCollection<ResourceViewModel> Resources { get; private set; }

        public string Output { get; set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ActivateItem(new SettingsViewModel());
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                CloseEtpClient();
            }

            base.OnDeactivate(close);
        }

        public void OnConnectionChanged()
        {
            CloseEtpClient();
            Resources.Clear();

            while (Items.Count > 1)
            {
                this.CloseItem(Items[1]);
            }

            ActivateItem(new HierarchyViewModel());
            InitEtpClient();
        }

        private void InitEtpClient()
        {
            _client = new EtpClient(Model.Connection.Uri, "ETP Browser");
            _client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();

            _client.Handler<ICoreClient>().OnOpenSession += OnOpenSession;
            _client.Handler<IDiscoveryCustomer>().OnGetResourcesResponse += OnGetResourcesResponse;

            _client.Open();
        }

        private void CloseEtpClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        private void GetResources(string uri)
        {
            _client.Handler<IDiscoveryCustomer>().GetResources(uri);
        }

        private void OnOpenSession(object sender, ProtocolEventArgs<OpenSession> e)
        {
            GetResources(RootUri);
        }

        private void OnGetResourcesResponse(object sender, ProtocolEventArgs<GetResourcesResponse, string> e)
        {
            var viewModel = new ResourceViewModel(e.Message.Resource);
            viewModel.LoadChildren = GetResources;

            if (e.Context == RootUri)
            {
                Resources.Add(viewModel);
                return;
            }

            var parent = FindResource(Resources, e.Context);

            if (parent != null)
            {
                parent.Children.Add(viewModel);
            }
        }

        private ResourceViewModel FindResource(IEnumerable<ResourceViewModel> resources, string uri)
        {
            foreach (var resource in resources)
            {
                if (resource.Resource.Uri == uri)
                    return resource;

                var found = FindResource(resource.Children, uri);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
