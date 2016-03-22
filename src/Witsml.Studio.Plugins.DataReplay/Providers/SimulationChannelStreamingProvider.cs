using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Studio.Plugins.DataReplay.Providers
{
    public class SimulationChannelStreamingProvider : ChannelStreamingProducerHandler
    {
        private CancellationTokenSource _tokenSource;

        public SimulationChannelStreamingProvider(Models.Simulation simulation)
        {
            Simulation = simulation;
            IsSimpleStreamer = true;
        }

        public Models.Simulation Simulation { get; private set; }

        protected override void HandleStart(MessageHeader header, Start start)
        {
            base.HandleStart(header, start);
            ChannelMetadata(header, Simulation.Channels);
            StartSendingChannelData(header);
        }

        protected override void HandleChannelStreamingStart(MessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
            base.HandleChannelStreamingStart(header, channelStreamingStart);
            StartSendingChannelData(header);
        }

        protected override void HandleChannelStreamingStop(MessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            base.HandleChannelStreamingStop(header, channelStreamingStop);

            if (_tokenSource != null)
                _tokenSource.Cancel();
        }

        private void StartSendingChannelData(MessageHeader request)
        {
            if (_tokenSource != null)
                _tokenSource.Cancel();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Task.Run(async () =>
            {
                using (_tokenSource)
                {
                    await SendChannelData(request, token);
                    _tokenSource = null;
                }
            },
            token);
        }

        private async Task SendChannelData(MessageHeader request, CancellationToken token)
        {
            while (true)
            {
                await Task.Delay(MaxMessageRate);

                if (token.IsCancellationRequested)
                {
                    break;
                }

                ChannelData(request, Simulation.Channels
                    .Select(x =>
                        new DataItem
                        {
                            ChannelId = x.ChannelId,
                            Indexes = new long[0],
                            ValueAttributes = new DataAttribute[0],
                            Value = new DataValue()
                            {
                               Item = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                            }
                        })
                    .ToList());
            }
        }
    }
}
