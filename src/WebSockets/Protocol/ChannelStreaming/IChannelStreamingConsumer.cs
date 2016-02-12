using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelStreaming
{
    public interface IChannelStreamingConsumer : IProtocolHandler
    {
        void Start(int maxDataItems = 10000, int maxMessageRate = 1000);

        void ChannelDescribe(IList<string> uris);

        void ChannelStreamingStart(IList<ChannelStreamingInfo> channelStreamingInfos);

        void ChannelStreamingStop(IList<int> channelIds);

        void ChannelRangeRequest(IList<ChannelRangeInfo> channelRangeInfos);

        event ProtocolEventHandler<ChannelMetadata> OnChannelMetadata;

        event ProtocolEventHandler<ChannelData> OnChannelData;

        event ProtocolEventHandler<ChannelDataChange> OnChannelDataChange;

        event ProtocolEventHandler<ChannelStatusChange> OnChannelStatusChange;

        event ProtocolEventHandler<ChannelDelete> OnChannelDelete;
    }
}
