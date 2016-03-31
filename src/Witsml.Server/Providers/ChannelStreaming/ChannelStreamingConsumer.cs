using System.ComponentModel.Composition;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export(typeof(IChannelStreamingConsumer))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreamingConsumer : ChannelStreamingConsumerHandler
    {
        protected override void HandleChannelMetadata(MessageHeader header, ChannelMetadata channelMetadata)
        {
            // Base implementation caches ChannelMetadataRecord items sent from the client
            base.HandleChannelMetadata(header, channelMetadata);

            var infos = new ChannelStreamingInfo[0];

            // Send ChannelStreamingStart message
            ChannelStreamingStart(infos);
        }
    }
}
