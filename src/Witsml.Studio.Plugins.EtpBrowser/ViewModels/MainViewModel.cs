using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Avro.Specific;
using Caliburn.Micro;
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using Energistics.Protocol.Core;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using PDS.Framework;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Models;
using PDS.Witsml.Studio.Plugins.EtpBrowser.Properties;
using PDS.Witsml.Studio.Runtime;
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
        private EtpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        /// <param name="runtime">The runtime service.</param>
        [ImportingConstructor]
        public MainViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = Settings.Default.PluginDisplayName;
            Resources = new BindableCollection<ResourceViewModel>();
            Model = new EtpSettings();

            Details = new TextEditorViewModel(runtime, "JavaScript", true);
            Messages = new TextEditorViewModel(runtime, "JavaScript", true)
            {
                IsScrollingEnabled = true
            };
        }

        /// <summary>
        /// Gets the display order of the plug-in when loaded by the main application shell
        /// </summary>
        public int DisplayOrder
        {
            get { return Settings.Default.PluginDisplayOrder; }
        }

        /// <summary>
        /// Gets the runtime service.
        /// </summary>
        /// <value>The runtime.</value>
        public IRuntimeService Runtime { get; private set; }

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

        private TextEditorViewModel _details;

        /// <summary>
        /// Gets or sets the details editor.
        /// </summary>
        /// <value>The details editor.</value>
        public TextEditorViewModel Details
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

        private TextEditorViewModel _messages;

        /// <summary>
        /// Gets or sets the messages editor.
        /// </summary>
        /// <value>The messages editor.</value>
        public TextEditorViewModel Messages
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
        /// Gets the selected resource's details using the Store protocol.
        /// </summary>
        public void GetObject()
        {
            var resource = FindResource(Resources, x => x.IsSelected);
            if (resource != null)
            {
                SendGetObject(resource.Resource.Uri);
            }
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.GetObject"/> message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendGetObject(string uri)
        {
            _client.Handler<IStoreCustomer>()
                .GetObject(uri);
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.PutObject"/> message with the supplied XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        public void SendPutObject(string xml)
        {
            var uri = new EtpUri(Model.Store.Uri);

            var dataObject = new DataObject()
            {
                ContentEncoding = string.Empty,
                Data = Encoding.UTF8.GetBytes(xml),
                Resource = new Resource()
                {
                    Uri = uri,
                    Uuid = Model.Store.Uuid,
                    Name = Model.Store.Name,
                    HasChildren = -1,
                    ContentType = uri.ContentType,
                    ResourceType = ResourceTypes.DataObject.ToString(),
                    CustomData = new Dictionary<string, string>(),
                    LastChanged = new Energistics.Datatypes.DateTime()
                    {
                        Offset = 0,
                        Time = 0
                    }
                }
            };

            _client.Handler<IStoreCustomer>()
                .PutObject(dataObject);
        }

        /// <summary>
        /// Sends the <see cref="Energistics.Protocol.Store.DeleteObject"/> message with the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public void SendDeleteObject(string uri)
        {
            _client.Handler<IStoreCustomer>()
                .DeleteObject(new List<string>() { uri });
        }

        /// <summary>
        /// Called when initializing.
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            ActivateItem(new SettingsViewModel(Runtime));
            Items.Add(new HierarchyViewModel());
            Items.Add(new StoreViewModel(Runtime));
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
        /// Called by a subclass when an activation needs processing.
        /// </summary>
        /// <param name="item">The item on which activation was attempted.</param>
        /// <param name="success">if set to <c>true</c> activation was successful.</param>
        protected override void OnActivationProcessed(IScreen item, bool success)
        {
            base.OnActivationProcessed(item, success);

            Runtime.Invoke(() =>
            {
                Runtime.Shell.SetBreadcrumb(DisplayName, item.DisplayName);
            });
        }

        /// <summary>
        /// Called when the connection has changed.
        /// </summary>
        public void OnConnectionChanged()
        {
            CloseEtpClient();
            Resources.Clear();
            Messages.Clear();
            Details.Clear();

            if (!string.IsNullOrWhiteSpace(Model.Connection.Uri))
            {
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
                Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connecting...");

                _client = new EtpClient(Model.Connection.Uri, "ETP Browser");
                _client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                _client.Register<IStoreCustomer, StoreCustomerHandler>();

                _client.Handler<ICoreClient>().OnOpenSession += OnOpenSession;
                _client.Handler<IDiscoveryCustomer>().OnGetResourcesResponse += OnGetResourcesResponse;
                _client.Handler<IStoreCustomer>().OnObject += OnObject;

                _client.Output = LogClientOutput;
                _client.Open();
            }
            catch (Exception ex)
            {
                Runtime.ShowError("Error connecting to server.", ex);
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
            Runtime.Invoke(() => Runtime.Shell.StatusBarText = "Connected");

            if (e.Message.SupportedProtocols.Any(x => x.Protocol == (int)Protocols.Discovery))
            {
                ActivateItem(Items[1]);
                GetResources(EtpUri.RootUri);
            }
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

            if (EtpUri.IsRoot(e.Context))
            {
                Resources.ForEach(x => x.IsSelected = false);
                viewModel.IsSelected = true;
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
            return FindResource(resources, x => x.Resource.Uri == uri);
        }

        /// <summary>
        /// Finds a resource by evaluating the specified predicate on each item in the collection.
        /// </summary>
        /// <param name="resources">The resources.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>A <see cref="ResourceViewModel" /> instance.</returns>
        private ResourceViewModel FindResource(IEnumerable<ResourceViewModel> resources, Func<ResourceViewModel, bool> predicate)
        {
            foreach (var resource in resources)
            {
                if (predicate(resource))
                    return resource;

                var found = FindResource(resource.Children, predicate);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Called when the GetObject response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ProtocolEventArgs{Energistics.Protocol.Store.Object}"/> instance containing the event data.</param>
        private void OnObject(object sender, ProtocolEventArgs<Energistics.Protocol.Store.Object> e)
        {
            Details.SetText(string.Format(
                "// Header:{3}{0}{3}{3}// Body:{3}{1}{3}{3}/* Data:{3}{2}{3}*/{3}",
                _client.Serialize(e.Header, true),
                _client.Serialize(e.Message, true),
                Encoding.UTF8.GetString(e.Message.DataObject.Data),
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T}"/> instance containing the event data.</param>
        private void LogObjectDetails<T>(ProtocolEventArgs<T> e) where T : ISpecificRecord
        {
            Details.SetText(string.Format(
                "// Header:{2}{0}{2}{2}// Body:{2}{1}{2}",
                _client.Serialize(e.Header, true),
                _client.Serialize(e.Message, true),
                Environment.NewLine));
        }

        /// <summary>
        /// Logs the object details.
        /// </summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <typeparam name="V">The context type.</typeparam>
        /// <param name="e">The <see cref="ProtocolEventArgs{T, V}"/> instance containing the event data.</param>
        private void LogObjectDetails<T, V>(ProtocolEventArgs<T, V> e) where T : ISpecificRecord
        {
            Details.SetText(string.Format(
                "// Header:{3}{0}{3}{3}// Body:{3}{1}{3}{3}// Context:{3}{2}{3}",
                _client.Serialize(e.Header, true),
                _client.Serialize(e.Message, true),
                _client.Serialize(e.Context, true),
                Environment.NewLine));
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

            Messages.Append(string.Concat(
                message.StartsWith("{") ? string.Empty : "// ",
                message,
                Environment.NewLine));
        }
    }
}
