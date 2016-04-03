using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelStreaming
{
    public interface IChannelStreamingProducer : IProtocolHandler
    {
        void ChannelMetadata(MessageHeader request, IList<ChannelMetadataRecord> channelMetadataRecords);

        void ChannelData(MessageHeader request, IList<DataItem> dataItems);

        void ChannelDataChange(long channelId, long startIndex, long endIndex, IList<DataItem> dataItems);

        void ChannelStatusChange(long channelId, ChannelStatuses status);

        void ChannelDelete(long channelId, string reason = null);

        event ProtocolEventHandler<Start> OnStart;

        event ProtocolEventHandler<ChannelDescribe, IList<ChannelMetadataRecord>> OnChannelDescribe;

        event ProtocolEventHandler<ChannelStreamingStart> OnChannelStreamingStart;

        event ProtocolEventHandler<ChannelStreamingStop> OnChannelStreamingStop;

        event ProtocolEventHandler<ChannelRangeRequest> OnChannelRangeRequest;
    }
}
