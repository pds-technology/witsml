using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;

namespace Energistics.Protocol.ChannelDataFrame
{
    public class ChannelDataFrameConsumerHandler : EtpProtocolHandler, IChannelDataFrameConsumer
    {
        public ChannelDataFrameConsumerHandler() : base(Protocols.ChannelDataFrame, "consumer")
        {
            RequestedRole = "producer";
        }

        public virtual void RequestChannelData(string uri, long? fromIndex = null, long? toIndex = null)
        {
            var header = CreateMessageHeader(Protocols.ChannelDataFrame, MessageTypes.ChannelDataFrame.RequestChannelData);

            var requestChannelData = new RequestChannelData()
            {
                Uri = uri,
                FromIndex = fromIndex,
                ToIndex = toIndex
            };

            Session.SendMessage(header, requestChannelData);
        }

        public event ProtocolEventHandler<ChannelMetadata> OnChannelMetadata;

        public event ProtocolEventHandler<ChannelDataFrameSet> OnChannelDataFrameSet;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.ChannelDataFrame.ChannelMetadata:
                    HandleChannelMetadata(header, decoder.Decode<ChannelMetadata>());
                    break;

                case (int)MessageTypes.ChannelDataFrame.ChannelDataFrameSet:
                    HandleChannelDataFrameSet(header, decoder.Decode<ChannelDataFrameSet>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected virtual void HandleChannelMetadata(MessageHeader header, ChannelMetadata channelMetadata)
        {
            Notify(OnChannelMetadata, header, channelMetadata);
        }

        protected virtual void HandleChannelDataFrameSet(MessageHeader header, ChannelDataFrameSet channelDataFrameSet)
        {
            Notify(OnChannelDataFrameSet, header, channelDataFrameSet);
        }
    }
}
