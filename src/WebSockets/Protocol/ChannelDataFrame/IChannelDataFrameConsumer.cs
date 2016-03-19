using Energistics.Common;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelDataFrame
{
    public interface IChannelDataFrameConsumer : IProtocolHandler
    {
        void RequestChannelData(string uri, long? fromIndex = null, long? toIndex = null);

        event ProtocolEventHandler<ChannelMetadata> OnChannelMetadata;

        event ProtocolEventHandler<ChannelDataFrameSet> OnChannelDataFrameSet;
    }
}
