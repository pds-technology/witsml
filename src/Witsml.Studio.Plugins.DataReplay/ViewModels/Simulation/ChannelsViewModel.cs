using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Energistics;
using Energistics.DataAccess;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Discovery;
using Microsoft.Win32;
using Newtonsoft.Json;
using PDS.Witsml.Studio.Connections;
using PDS.Witsml.Studio.Plugins.DataReplay.Providers;
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
        /// <value>
        /// The WITSML seb service proxy.
        /// </value>
        public WITSMLWebServiceConnection Proxy { get; private set; }

        /// <summary>
        /// Gets the witsml versions retrieved from the server.
        /// </summary>
        /// <value>
        /// The server's supported witsml versions.
        /// </value>
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

        private CancellationTokenSource _tokenSource;
        public CancellationTokenSource TokenSource
        {
            get { return _tokenSource; }
            set
            {
                if (!ReferenceEquals(_tokenSource, value))
                {
                    _tokenSource = value;
                    NotifyOfPropertyChange(() => TokenSource);
                    NotifyOfPropertyChange(() => CanStartChannelStreaming);
                    NotifyOfPropertyChange(() => CanStopChannelStreaming);
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

        public void StartWitsmlLogData()
        {
        }

        public void StartEtpLogData()
        {
        }

        public bool CanStartChannelStreaming
        {
            get { return TokenSource == null; }
        }

        public void StartChannelStreaming()
        {
            TokenSource = new CancellationTokenSource();
            var token = TokenSource.Token;

            Task.Run(async () =>
            {
                using (TokenSource)
                {
                    await InitChannelStreaming(token);
                    TokenSource = null;

                    Log("ETP Socket Server stopped.");
                }
            },
            token);
        }

        public bool CanStopChannelStreaming
        {
            get { return TokenSource != null; }
        }

        public void StopChannelStreaming()
        {
            if (TokenSource != null)
            {
                TokenSource.Cancel();
            }
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                StopChannelStreaming();
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
                var versions = Proxy.GetVersion();

                if (!string.IsNullOrEmpty(versions))
                {
                    _log.DebugFormat("Supported versions '{0}' found on WITSML server with uri '{1}'", versions, Model.WitsmlConnection.Uri);
                    WitsmlVersions.AddRange(versions.Split(','));
                    Model.WitsmlVersion = WitsmlVersions.Last();
                }
                else
                {
                    var msg = "The Witsml server does not support any versions.";
                    _log.Warn(msg);
                    Runtime.ShowError(msg);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("{0}{1}{1}{2}", "Error connecting to server.", Environment.NewLine, "Invalid URL");

                // Log the error
                _log.Error(errorMessage, ex);

                // Show the user the error in a dialog.
                Runtime.ShowError(errorMessage, ex);
            }
        }

        /// <summary>
        /// Creates a WITSMLWebServiceConnection for the current connection uri and witsml version.
        /// </summary>
        /// <returns></returns>
        private WITSMLWebServiceConnection CreateProxy()
        {
            _log.DebugFormat("A new Proxy is being created with {2}{2}uri: {0}{2}{2}WitsmlVersion: {1}{2}{2}", Model.WitsmlConnection.Uri, Model.WitsmlVersion, Environment.NewLine);
            var proxy = new WITSMLWebServiceConnection(Model.WitsmlConnection.Uri, GetWitsmlVersionEnum(Model.WitsmlVersion));

            if (!string.IsNullOrWhiteSpace(Model.WitsmlConnection.Username))
            {
                proxy.Username = Model.WitsmlConnection.Username;
                proxy.SetSecurePassword(Model.WitsmlConnection.SecurePassword);
            }

            return proxy;
        }

        /// <summary>
        /// Gets the witsml version enum.
        /// </summary>
        /// <returns>
        /// The WMLSVersion enum value based on the current value of Model.WitsmlVersion.
        /// If Model.WitsmlVersion has not been established the the default is WMLSVersion.WITSML141.
        /// </returns>
        private WMLSVersion GetWitsmlVersionEnum(string witsmlVersion)
        {
            return witsmlVersion != null && witsmlVersion.Equals(OptionsIn.DataVersion.Version131.Value)
                ? WMLSVersion.WITSML131
                : WMLSVersion.WITSML141;
        }

        private void Log(string message, params object[] values)
        {
            Output += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff - ") + string.Format(message, values) + Environment.NewLine;
        }
    }
}
