using Energistics.Datatypes;
using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Studio.Plugins.DataReplay.Providers
{
    public class SimulationChannelStreamingProvider : ChannelStreamingProducerHandler
    {
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
        }

        protected override void HandleChannelStreamingStart(MessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
            base.HandleChannelStreamingStart(header, channelStreamingStart);
        }

        protected override void HandleChannelStreamingStop(MessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            base.HandleChannelStreamingStop(header, channelStreamingStop);
        }
    }
}
