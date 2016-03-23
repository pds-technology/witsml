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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Discovery;
using Microsoft.Win32;
using Newtonsoft.Json;
using PDS.Framework;
using PDS.Witsml.Data.Logs;
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
                        await InitWitsmlClient(token);
                        Log("WITSML Client simulation stopped.");
                    }
                    catch (ContainerException)
                    {
                        Log("Data object not supported; Type: {0}; Version: {1};", ObjectTypes.Log, Model.WitsmlVersion);
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

        private async Task InitWitsmlClient(CancellationToken token)
        {
            //var generator = Runtime.Container.Resolve<IWitsmlDataGenerator>(new ObjectName(ObjectTypes.Log, Model.WitsmlVersion));
            var generator = new Log141Generator();
            var index = 0d;

            Log("WITSML Client simulation started. URL: {0}", Proxy.Url);

            var logList = new Log()
            {
                UidWell = Model.WellUid,
                NameWell = Model.WellName,
                UidWellbore = Model.WellboreUid,
                NameWellbore = Model.WellboreName,
                Uid = Model.LogUid,
                Name = Model.LogName,
                IndexType = Model.LogIndexType
            }
            .AsList();

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var result = Proxy.Read(new LogList() { Log = logList }, OptionsIn.ReturnElements.HeaderOnly);

                if (!result.Log.Any())
                {
                    Runtime.Invoke(() => Runtime.ShowError("Log not found."));
                    break;
                }

                var log = result.Log[0];
                log.Direction = LogIndexDirection.increasing;
                log.IndexCurve = Model.Channels.Select(x => x.Mnemonic).FirstOrDefault();
                log.LogCurveInfo = Model.Channels.Select(ToLogCurveInfo).ToList();

                index = generator.GenerateLogData(log, startIndex: index);

                result.Log[0].LogData[0].MnemonicList = generator.Mnemonics(result.Log[0].LogCurveInfo);
                result.Log[0].LogData[0].UnitList = generator.Units(result.Log[0].LogCurveInfo);

                Proxy.Update(result);

                await Task.Delay(5000);
            }
        }

        private LogCurveInfo ToLogCurveInfo(ChannelMetadataRecord channel)
        {
            return new LogCurveInfo()
            {
                Mnemonic = new ShortNameStruct(channel.Mnemonic),
                Unit = channel.Uom,
                CurveDescription = channel.Description,
                TypeLogData = LogDataType.@double,
            };
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
            return OptionsIn.DataVersion.Version131.Equals(witsmlVersion)
                ? WMLSVersion.WITSML131
                : WMLSVersion.WITSML141;
        }

        private void Log(string message, params object[] values)
        {
            Output += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff - ") + string.Format(message, values) + Environment.NewLine;
        }
    }
}
