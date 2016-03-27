using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Energistics;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Discovery;
using Microsoft.Win32;
using Newtonsoft.Json;
using PDS.Framework;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Plugins.DataReplay.Providers;
using PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies;
using PDS.Witsml.Studio.Runtime;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Simulation
{
    public class ChannelsViewModel : Screen
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ChannelsViewModel));

        public ChannelsViewModel(IRuntimeService runtime)
        {
            Runtime = runtime;
            DisplayName = "Channels";
            WitsmlVersions = new BindableCollection<string>();
        }

        public Models.Simulation Model
        {
            get { return ((SimulationViewModel)Parent).Model; }
        }

        public IRuntimeService Runtime { get; private set; }

        /// <summary>
        /// Gets the proxy for the WITSML web service.
        /// </summary>
        /// <value>The WITSML seb service proxy.</value>
        public WitsmlProxyViewModel Proxy { get; private set; }

        /// <summary>
        /// Gets the witsml versions retrieved from the server.
        /// </summary>
        /// <value>The server's supported witsml versions.</value>
        public BindableCollection<string> WitsmlVersions { get; }

        private string _output;
        public string Output
        {
            get { return _output; }
            set
            {
                if (!String.Equals(_output, value))
                {
                    _output = value;
                    NotifyOfPropertyChange(() => Output);
                }
            }
        }

        private CancellationTokenSource _witsmlClientTokenSource;
        public CancellationTokenSource WitsmlClientTokenSource
        {
            get { return _witsmlClientTokenSource; }
            set
            {
                if (!ReferenceEquals(_witsmlClientTokenSource, value))
                {
                    _witsmlClientTokenSource = value;
                    NotifyOfPropertyChange(() => WitsmlClientTokenSource);
                    NotifyOfPropertyChange(() => CanStartWitsmlClient);
                    NotifyOfPropertyChange(() => CanStopWitsmlClient);
                }
            }
        }

        private CancellationTokenSource _etpClientTokenSource;
        public CancellationTokenSource EtpClientTokenSource
        {
            get { return _etpClientTokenSource; }
            set
            {
                if (!ReferenceEquals(_etpClientTokenSource, value))
                {
                    _etpClientTokenSource = value;
                    NotifyOfPropertyChange(() => EtpClientTokenSource);
                    NotifyOfPropertyChange(() => CanStartEtpClient);
                    NotifyOfPropertyChange(() => CanStopEtpClient);
                }
            }
        }

        private CancellationTokenSource _etpServerTokenSource;
        public CancellationTokenSource EtpServerTokenSource
        {
            get { return _etpServerTokenSource; }
            set
            {
                if (!ReferenceEquals(_etpServerTokenSource, value))
                {
                    _etpServerTokenSource = value;
                    NotifyOfPropertyChange(() => EtpServerTokenSource);
                    NotifyOfPropertyChange(() => CanStartEtpServer);
                    NotifyOfPropertyChange(() => CanStopEtpServer);
                }
            }
        }

        public void Import()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Open Channel Metadata Configurtion File...",
                Filter = "JSON Files|*.json;*.js|All Files|*.*"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow).GetValueOrDefault())
            {
                var json = File.ReadAllText(dialog.FileName);

                try
                {
                    var channels = JsonConvert.DeserializeObject<List<ChannelMetadataRecord>>(json);
                    Model.Channels.AddRange(channels);
                }
                catch (Exception ex)
                {
                    Runtime.ShowError("Error importing Channel Metadata", ex);
                }
            }
        }

        public void ShowWitsmlConnectionDialog()
        {
            var viewModel = new ConnectionViewModel(Runtime, ConnectionTypes.Witsml)
            {
                DataItem = Model.WitsmlConnection
            };

            if (Runtime.ShowDialog(viewModel))
            {
                Model.WitsmlConnection = viewModel.DataItem;
                Proxy = CreateProxy();
                GetVersions();
            }
        }

        public void ShowEtpConnectionDialog()
        {
            var viewModel = new ConnectionViewModel(Runtime, ConnectionTypes.Etp)
            {
                DataItem = Model.EtpConnection
            };

            if (Runtime.ShowDialog(viewModel))
            {
                Model.EtpConnection = viewModel.DataItem;
            }
        }

        public void OnWitsmlVersionChanged()
        {
            Proxy = CreateProxy();
        }

        public bool CanStartWitsmlClient
        {
            get { return WitsmlClientTokenSource == null; }
        }

        public void StartWitsmlClient()
        {
            WitsmlClientTokenSource = new CancellationTokenSource();
            var token = WitsmlClientTokenSource.Token;

            Task.Run(async () =>
            {
                using (WitsmlClientTokenSource)
                {
                    try
                    {
                        Log("WITSML Client simulation starting. URL: {0}", Model.WitsmlConnection.Uri);
                        await Proxy.Start(Model, token);
                        Log("WITSML Client simulation stopped.");
                    }
                    catch (ContainerException)
                    {
                        Log("Data object not supported; Type: {0}; Version: {1};", ObjectTypes.Log, Model.WitsmlVersion);
                    }
                    catch (Exception ex)
                    {
                        Log("An error occurred: " + ex);
                    }
                    finally
                    {
                        WitsmlClientTokenSource = null;
                    }
                }
            },
            token);
        }

        public bool CanStopWitsmlClient
        {
            get { return WitsmlClientTokenSource != null; }
        }

        public void StopWitsmlClient()
        {
            if (WitsmlClientTokenSource != null)
            {
                WitsmlClientTokenSource.Cancel();
            }
        }

        public bool CanStartEtpClient
        {
            get { return EtpClientTokenSource == null; }
        }

        public void StartEtpClient()
        {
        }

        public bool CanStopEtpClient
        {
            get { return EtpClientTokenSource != null; }
        }

        public void StopEtpClient()
        {
            if (EtpClientTokenSource != null)
            {
                EtpClientTokenSource.Cancel();
            }
        }

        public bool CanStartEtpServer
        {
            get { return EtpServerTokenSource == null; }
        }

        public void StartEtpServer()
        {
            EtpServerTokenSource = new CancellationTokenSource();
            var token = EtpServerTokenSource.Token;

            Task.Run(async () =>
            {
                using (EtpServerTokenSource)
                {
                    try
                    {
                        await InitChannelStreaming(token);
                        Log("ETP Socket Server stopped.");
                    }
                    finally
                    {
                        EtpServerTokenSource = null;
                    }
                }
            },
            token);
        }

        public bool CanStopEtpServer
        {
            get { return EtpServerTokenSource != null; }
        }

        public void StopEtpServer()
        {
            if (EtpServerTokenSource != null)
            {
                EtpServerTokenSource.Cancel();
            }
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                StopEtpServer();
            }

            base.OnDeactivate(close);
        }

        private async Task InitChannelStreaming(CancellationToken token)
        {
            using (var server = new EtpSocketServer(Model.PortNumber, ((IScreen)Parent).DisplayName, Model.Version))
            {
                server.Register(InitChannelStreamingProvider);
                server.Register(InitDiscoveryProvider);
                server.Start();

                Log("ETP Socket Server started, listening on port {0}.", Model.PortNumber);

                while (true)
                {
                    await Task.Delay(250);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }

        private IChannelStreamingProducer InitChannelStreamingProvider()
        {
            return new SimulationChannelStreamingProvider(Model);
        }

        private IDiscoveryStore InitDiscoveryProvider()
        {
            return new SimulationDiscoveryProvider(Model);
        }

        /// <summary>
        /// Gets the supported versions from the server and initializes the UI element for version selection.
        /// </summary>
        private void GetVersions()
        {
            _log.Debug("Selecting supported versions from WITSML server.");

            try
            {
                WitsmlVersions.Clear();
                var versions = Proxy.Connection.GetVersion();

                if (!string.IsNullOrEmpty(versions))
                {
                    _log.DebugFormat("Supported versions '{0}' found on WITSML server with URI '{1}'", versions, Model.WitsmlConnection.Uri);
                    WitsmlVersions.AddRange(versions.Split(','));
                    Model.WitsmlVersion = WitsmlVersions.Last();
                }
                else
                {
                    var msg = "The WITSML server does not support any versions.";
                    _log.Warn(msg);
                    Runtime.ShowError(msg);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = "Error connecting to server: Invalid URL";

                // Log the error
                _log.Error(errorMessage, ex);

                // Show the user the error in a dialog.
                Runtime.ShowError(errorMessage, ex);
            }
        }

        private WitsmlProxyViewModel CreateProxy()
        {
            return OptionsIn.DataVersion.Version131.Equals(Model.WitsmlVersion)
                ? new Log131ProxyViewModel(Runtime, Model.WitsmlConnection) as WitsmlProxyViewModel
                : new Log141ProxyViewModel(Runtime, Model.WitsmlConnection) as WitsmlProxyViewModel;
        }

        private void Log(string message, params object[] values)
        {
            Output += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff - ") + string.Format(message, values) + Environment.NewLine;
        }
    }
}
