using System;
using System.Threading;
using System.Threading.Tasks;
using Energistics;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Core;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Studio.Runtime;

namespace PDS.Witsml.Studio.Plugins.DataReplay.ViewModels.Proxies
{
    public class Etp141ProxyViewModel : EtpProxyViewModel
    {
        private Log141Generator _generator;
        private long _interval;

        public Etp141ProxyViewModel(IRuntimeService runtime, Action<string> log) : base(runtime, log)
        {
            _generator = new Log141Generator();
        }

        public override async Task Start(Models.Simulation model, CancellationToken token, int interval = 5000)
        {
            Model = model;
            _interval = interval;

            var headers = EtpClient.Authorization(Model.EtpConnection.Username, Model.EtpConnection.Password);

            using (Client = new EtpClient(Model.EtpConnection.Uri, Model.Name, Model.Version, headers))
            {
                Client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();
                Client.Handler<IChannelStreamingProducer>().OnStart += OnStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStart += OnChannelStreamingStart;
                Client.Handler<IChannelStreamingProducer>().OnChannelStreamingStop += OnChannelStreamingStop;
                Client.Output = Log;
                Client.Open();

                while (true)
                {
                    var delay = (int)Interlocked.Read(ref _interval);
                    await Task.Delay(delay);

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

                Client.Handler<ICoreClient>()
                    .CloseSession("Streaming stopped.");
            }
        }

        private void OnStart(object sender, ProtocolEventArgs<Start> e)
        {
            Interlocked.Exchange(ref _interval, e.Message.MaxMessageRate);

            Client.Handler<IChannelStreamingProducer>()
                .ChannelMetadata(e.Header, Model.Channels);
        }

        private void OnChannelStreamingStart(object sender, ProtocolEventArgs<ChannelStreamingStart> e)
        {
        }

        private void OnChannelStreamingStop(object sender, ProtocolEventArgs<ChannelStreamingStop> e)
        {
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
