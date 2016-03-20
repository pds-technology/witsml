using System.ComponentModel.Composition;
using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export(typeof(IChannelStreamingConsumer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelStreamingConsumer : ChannelStreamingConsumerHandler
    {
    }
}
