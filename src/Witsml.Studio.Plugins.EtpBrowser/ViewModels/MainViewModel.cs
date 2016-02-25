using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Avro.Specific;
using Caliburn.Micro;
using Energistics;
using Energistics.Common;
using Energistics.Protocol;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Properties;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.EtpBrowser.ViewModels
{
    /// <summary>
    /// Manages the behavior of the main user interface for the ETP Browser plug-in.
    /// </summary>
    /// <seealso cref="Caliburn.Micro.Conductor{Caliburn.Micro.IScreen}.Collection.OneActive" />
    /// <seealso cref="PDS.Witsml.Studio.ViewModels.IPluginViewModel" />
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IPluginViewModel
    {
        private const string RootUri = "/";
        private EtpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
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

        /// <summary>
        /// Gets the model.
        /// </summary>
        /// <value>The model.</value>
        public EtpSettings Model { get; private set; }

        /// <summary>
        /// Gets the resources to display in the tree view.
        /// </summary>
        /// <value>The collection of resources.</value>
        public BindableCollection<ResourceViewModel> Resources { get; private set; }

        private string _details;
        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        /// <value>The output.</value>
        public string Details
        {
            get { return _details; }
            set
            {
                if (!string.Equals(_details, value))
                {
                    _details = value;
                    NotifyOfPropertyChange(() => Details);
                }
            }
        }

        private string _messages;
        /// <summary>
        /// Gets or sets the messages.
        /// </summary>
        /// <value>The output.</value>
        public string Messages
        {
            get { return _messages; }
            set
            {
                if (!string.Equals(_messages, value))
                {
                    _messages = value;
                    NotifyOfPropertyChange(() => Messages);
                }
            }
        }

        /// <summary>
        /// Copies the details to the clipboard.
        /// </summary>
        public void CopyDetails()
        {
            Clipboard.SetText(Details);
        }

        /// <summary>
        /// Clears the details.
        /// </summary>
        public void ClearDetails()
        {
            Details = string.Empty;
        }

        /// <summary>
        /// Copies the messages to the clipboard.
        /// </summary>
        public void CopyMessages()
        {
            Clipboard.SetText(Messages);
        }

        /// <summary>
        /// Clears the messages.
        /// </summary>
        public void ClearMessages()
        {
            Messages = string.Empty;
        }

        /// <summary>
        /// Scrolls to the bottom of the current text content.
        /// </summary>
        /// <param name="control">The control.</param>
        public void ScrollToBottom(TextBox control)
        {
            control.ScrollToEnd();
        }

        /// <summary>
        /// Called when initializing.
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            ActivateItem(new SettingsViewModel());
        }

        /// <summary>
        /// Called when deactivating.
        /// </summary>
        /// <param name="close">Inidicates whether this instance will be closed.</param>
        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                CloseEtpClient();
            }

            base.OnDeactivate(close);
        }

        /// <summary>
        /// Called when the connection has changed.
        /// </summary>
        public void OnConnectionChanged()
        {
            CloseEtpClient();
            Resources.Clear();
            Messages = null;
            Details = null;

            while (Items.Count > 1)
            {
                this.CloseItem(Items[1]);
            }

            if (!string.IsNullOrWhiteSpace(Model.Connection.Uri))
            {
                ActivateItem(new HierarchyViewModel());
                InitEtpClient();
            }
        }

        /// <summary>
        /// Initializes the ETP client.
        /// </summary>
        private void InitEtpClient()
        {
            try
            {
                _client = new EtpClient(Model.Connection.Uri, "ETP Browser");
                _client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();

                _client.Handler<ICoreClient>().OnOpenSession += OnOpenSession;
                _client.Handler<IDiscoveryCustomer>().OnGetResourcesResponse += OnGetResourcesResponse;

                _client.Output = LogClientOutput;
                _client.Open();
            }
            catch (Exception ex)
            {
                App.Current.ShowError("Error connecting to server.", ex);
            }
        }

        /// <summary>
        /// Closes the ETP client.
        /// </summary>
        private void CloseEtpClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Gets the resources using the Discovery protocol.
        /// </summary>
        /// <param name="uri">The URI.</param>
        private void GetResources(string uri)
        {
            _client.Handler<IDiscoveryCustomer>().GetResources(uri);
        }

        /// <summary>
        /// Called when the ETP session is initialized.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{OpenSession}"/> instance containing the event data.</param>
        private void OnOpenSession(object sender, ProtocolEventArgs<OpenSession> e)
        {
            GetResources(RootUri);
        }

        /// <summary>
        /// Called when the GetResources response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{GetResourcesResponse, System.String}"/> instance containing the event data.</param>
        private void OnGetResourcesResponse(object sender, ProtocolEventArgs<GetResourcesResponse, string> e)
        {
            var viewModel = new ResourceViewModel(e.Message.Resource);
            viewModel.LoadChildren = GetResources;

            LogObjectDetails(e);

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

        /// <summary>
        /// Finds the resource associated with the specified URI.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="uri">The URI.</param>
        /// <returns>A <see cref="ResourceViewModel"/> instance.</returns>
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

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        private void LogObjectDetails<T>(ProtocolEventArgs<T> e) where T : ISpecificRecord
        {
            Details = string.Format(
                "Header:{3}{0}{3}{3}Body:{3}{1}{3}",
                _client.Serialize(e.Header, true),
                _client.Serialize(e.Message, true),
                Environment.NewLine);
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <typeparam name="V">The context type.</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T, V}"/> instance containing the event data.</param>
        private void LogObjectDetails<T, V>(ProtocolEventArgs<T, V> e) where T : ISpecificRecord
        {
            Details = string.Format(
                "Header:{3}{0}{3}{3}Body:{3}{1}{3}{3}Context:{3}{2}{3}",
                _client.Serialize(e.Header, true),
                _client.Serialize(e.Message, true),
                _client.Serialize(e.Context, true),
                Environment.NewLine);
        }

        /// <summary>
        /// Logs the client output.
        /// </summary>
        /// <param name="message">The message.</param>
        private void LogClientOutput(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Messages = string.Concat(Messages ?? string.Empty, message, Environment.NewLine);
        }
    }
}
