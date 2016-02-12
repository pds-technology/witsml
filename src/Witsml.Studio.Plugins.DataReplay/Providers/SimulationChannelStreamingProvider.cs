using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Studio.Plugins.DataReplay.Providers
{
    public class SimulationChannelStreamingProvider : ChannelStreamingProducerHandler
    {
        public SimulationChannelStreamingProvider(Models.Simulation simulation)
        {
            Simulation = simulation;
        }

        public Models.Simulation Simulation { get; private set; }
    }
}
