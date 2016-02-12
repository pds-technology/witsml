using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelDataFrame
{
    public class ChannelDataFrameProducerHandler : EtpProtocolHandler, IChannelDataFrameProducer
    {
        public ChannelDataFrameProducerHandler() : base(Protocols.ChannelDataFrame, "producer")
        {
        }

        public virtual void ChannelMetadata(ChannelMetadata channelMetadata)
        {
            var header = CreateMessageHeader(Protocols.ChannelDataFrame, MessageTypes.ChannelDataFrame.ChannelMetadata);

            Session.SendMessage(header, channelMetadata);
        }

        public virtual void ChannelDataFrameSet(IList<long> channelIds, IList<DataFrame> dataFrames)
        {
            var header = CreateMessageHeader(Protocols.ChannelDataFrame, MessageTypes.ChannelDataFrame.ChannelDataFrameSet);

            var channelDataFrameSet = new ChannelDataFrameSet()
            {
                Channels = channelIds,
                Data = dataFrames
            };

            Session.SendMessage(header, channelDataFrameSet);
        }

        public event ProtocolEventHandler<RequestChannelData, ChannelMetadata> OnRequestChannelData;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.ChannelDataFrame.RequestChannelData:
                    HandleRequestChannelData(header, decoder.Decode<RequestChannelData>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected virtual void HandleRequestChannelData(MessageHeader header, RequestChannelData requestChannelData)
        {
            var args = Notify(OnRequestChannelData, header, requestChannelData, new ChannelMetadata());
            HandleRequestChannelData(args);

            ChannelMetadata(args.Context);
        }

        protected virtual void HandleRequestChannelData(ProtocolEventArgs<RequestChannelData, ChannelMetadata> args)
        {
        }
    }
}
