using System;
using System.Collections.Generic;
using System.IO;
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
using PDS.Witsml.Studio.Plugins.DataReplay.Providers;
using PDS.Witsml.Studio.ViewModels;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Simulation
{
    public class ChannelsViewModel : Screen
    {
        public ChannelsViewModel()
        {
            DisplayName = "Channels";
        }

        public Models.Simulation Model
        {
            get { return ((SimulationViewModel)Parent).Model; }
        }

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

        public void ShowConnectionDialog()
        {
            var windowManager = Application.Current.Container().Resolve<IWindowManager>();
            var viewModel = new ConnectionViewModel()
            {
                Connection = Model.Connection
            };


            if (windowManager.ShowDialog(viewModel).GetValueOrDefault())
            {
                Model.Connection = viewModel.Connection;
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
                    Application.Current.ShowError("Error importing Channel Metadata", ex);
                }
            }
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

        private async Task InitChannelStreaming(CancellationToken token)
        {
            using (var server = new EtpSocketServer(Model.PortNumber, ((IScreen)Parent).DisplayName))
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

        private void Log(string message, params object[] values)
        {
            Output += DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff - ") + string.Format(message, values) + Environment.NewLine;
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                StopChannelStreaming();
            }

            base.OnDeactivate(close);
        }
    }
}
