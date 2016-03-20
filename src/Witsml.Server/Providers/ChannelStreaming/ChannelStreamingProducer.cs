using System.ComponentModel.Composition;
using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export(typeof(IChannelStreamingProducer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelStreamingProducer : ChannelStreamingProducerHandler
    {
    }
}
