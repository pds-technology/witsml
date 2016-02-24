using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelStreaming
{
    public class ChannelStreamingProducerHandler : EtpProtocolHandler, IChannelStreamingProducer
    {
        public ChannelStreamingProducerHandler() : base(Protocols.ChannelStreaming, "producer")
        {
        }

        public int MaxDataItems { get; private set; }

        public int MaxMessageRate { get; private set; }

        public virtual void ChannelMetadata(MessageHeader request, IList<ChannelMetadataRecord> channelMetadataRecords)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelMetadata, request.MessageId, MessageFlags.FinalPart);

            var channelMetadata = new ChannelMetadata()
            {
                Channels = channelMetadataRecords
            };

            Session.SendMessage(header, channelMetadata);
        }

        public virtual void ChannelData(MessageHeader request, IList<DataItem> dataItems)
        {
            // NOTE: CorrelationId is only specified when responding to a ChannelRangeRequest message
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelData, request == null ? 0 : request.MessageId, MessageFlags.MultiPart);

            var channelData = new ChannelData()
            {
                Data = dataItems
            };

            Session.SendMessage(header, channelData);
        }

        public virtual void ChannelDataChange(long channelId, IndexValue startIndex, IndexValue endIndex, IList<DataItem> dataItems)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelDataChange);

            var channelDataChange = new ChannelDataChange()
            {
                ChannelId = channelId,
                StartIndex = startIndex,
                EndIndex = endIndex,
                Data = dataItems
            };

            Session.SendMessage(header, channelDataChange);
        }

        public virtual void ChannelStatusChange(long channelId, ChannelStatuses status)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelStatusChange);

            var channelStatusChange = new ChannelStatusChange()
            {
                ChannelId = channelId,
                Status = status
            };

            Session.SendMessage(header, channelStatusChange);
        }

        public virtual void ChannelDelete(long channelId, string reason = null)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelDelete);

            var channelDelete = new ChannelDelete()
            {
                ChannelId = channelId,
                DeleteReason = reason
            };

            Session.SendMessage(header, channelDelete);
        }

        public event ProtocolEventHandler<Start> OnStart;

        public event ProtocolEventHandler<ChannelDescribe, IList<ChannelMetadataRecord>> OnChannelDescribe;

        public event ProtocolEventHandler<ChannelStreamingStart> OnChannelStreamingStart;

        public event ProtocolEventHandler<ChannelStreamingStop> OnChannelStreamingStop;

        public event ProtocolEventHandler<ChannelRangeRequest, IList<DataItem>> OnChannelRangeRequest;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.ChannelStreaming.Start:
                    HandleStart(header, decoder.Decode<Start>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelDescribe:
                    HandleChannelDescribe(header, decoder.Decode<ChannelDescribe>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelStreamingStart:
                    HandleChannelStreamingStart(header, decoder.Decode<ChannelStreamingStart>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelStreamingStop:
                    HandleChannelStreamingStop(header, decoder.Decode<ChannelStreamingStop>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelRangeRequest:
                    HandleChannelRangeRequest(header, decoder.Decode<ChannelRangeRequest>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected virtual void HandleStart(MessageHeader header, Start start)
        {
            MaxDataItems = start.MaxDataItems;
            MaxMessageRate = start.MaxMessageRate;
            Notify(OnStart, header, start);
        }

        protected virtual void HandleChannelDescribe(MessageHeader header, ChannelDescribe channelDescribe)
        {
            var args = Notify(OnChannelDescribe, header, channelDescribe, new List<ChannelMetadataRecord>());
            HandleChannelDescribe(args);

            ChannelMetadata(header, args.Context);
        }

        protected virtual void HandleChannelDescribe(ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> args)
        {
        }

        protected virtual void HandleChannelStreamingStart(MessageHeader header, ChannelStreamingStart channelStreamingStart)
        {
            Notify(OnChannelStreamingStart, header, channelStreamingStart);
        }

        protected virtual void HandleChannelStreamingStop(MessageHeader header, ChannelStreamingStop channelStreamingStop)
        {
            Notify(OnChannelStreamingStop, header, channelStreamingStop);
        }

        protected virtual void HandleChannelRangeRequest(MessageHeader header, ChannelRangeRequest channelRangeRequest)
        {
            var args = Notify(OnChannelRangeRequest, header, channelRangeRequest, new List<DataItem>());
            HandleChannelRangeRequest(args);

            ChannelData(header, args.Context);
        }

        protected virtual void HandleChannelRangeRequest(ProtocolEventArgs<ChannelRangeRequest, IList<DataItem>> args)
        {
        }
    }
}
