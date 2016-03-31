using System.Threading;
using System.Threading.Tasks;
using Energistics;
using Energistics.Common;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public class Etp141ProxyViewModel : WitsmlProxyViewModel
    {
        public Etp141ProxyViewModel(IRuntimeService runtime, Connections.Connection connection) : base(connection, (WMLSVersion)2)
        {
            Runtime = runtime;
            Generator = new Log141Generator();
        }

        public IRuntimeService Runtime { get; private set; }

        public Log141Generator Generator { get; private set; }

        public Models.Simulation Model { get; private set; }

        public EtpClient Client { get; private set; }

        public override async Task Start(Models.Simulation model, CancellationToken token, int interval = 5000)
        {
            Model = model;

            var headers = EtpClient.Authorization(Model.EtpConnection.Username, Model.EtpConnection.Password);

            using (Client = new EtpClient(Model.EtpConnection.Uri, Model.Name, Model.Version, headers))
            {
                Client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                Client.Handler<IChannelStreamingProducer>().OnStart += OnStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStart += OnChannelStreamingStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStop += OnChannelStreamingStop;
                Client.Open();

                //Log("ETP Socket Server started, listening on port {0}.", Model.PortNumber);

                while (true)
                {
                    await Task.Delay(1000);

                    if (token.IsCancellationRequested || !Client.IsOpen)
                    {
                        break;
                    }

                    //var dataItems = model.Channels
                    //    .Select(ToChannelDataItem)
                    //    .ToList();

                    //client.Handler<IChannelStreamingProducer>()
                    //    .ChannelData(dataItems);
                }
            }
        }

        private void OnStart(object sender, ProtocolEventArgs<Start> e)
        {
            Client.Handler<IChannelStreamingProducer>()
                .ChannelMetadata(e.Header, Model.Channels);
        }

        private void OnChannelStreamingStart(object sender, ProtocolEventArgs<ChannelStreamingStart> e)
        {
        }

        private void OnChannelStreamingStop(object sender, ProtocolEventArgs<ChannelStreamingStop> e)
        {
            Client.Handler<ICoreClient>()
                .CloseSession("Streaming stopped.");
        }

        private DataItem ToChannelDataItem(ChannelMetadataRecord channel)
        {
            return new DataItem()
            {
                ChannelId = channel.ChannelId,
                Indexes = new long[0],
                ValueAttributes = new DataAttribute[0],
                Value = new DataValue()
                {
                    Item = 0
                }
            };
        }
    }
}
