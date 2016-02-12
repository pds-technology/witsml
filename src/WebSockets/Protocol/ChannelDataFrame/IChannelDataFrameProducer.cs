using System.Collections.Generic;
using Energistics.Common;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelDataFrame
{
    public interface IChannelDataFrameProducer : IProtocolHandler
    {
        void ChannelMetadata(ChannelMetadata channelMetadata);

        void ChannelDataFrameSet(IList<long> channelIds, IList<DataFrame> dataFrames);

        event ProtocolEventHandler<RequestChannelData, ChannelMetadata> OnRequestChannelData;
    }
}
