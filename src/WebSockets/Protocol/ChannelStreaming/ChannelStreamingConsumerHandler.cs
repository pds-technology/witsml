using System.Collections.Generic;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace Energistics.Protocol.ChannelStreaming
{
    public class ChannelStreamingConsumerHandler : EtpProtocolHandler, IChannelStreamingConsumer
    {
        public ChannelStreamingConsumerHandler() : base(Protocols.ChannelStreaming, "consumer")
        {
            RequestedRole = "producer";
            ChannelMetadataRecords = new List<ChannelMetadataRecord>(0);
        }

        protected IList<ChannelMetadataRecord> ChannelMetadataRecords { get; private set; }

        public virtual void Start(int maxDataItems = 10000, int maxMessageRate = 1000)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.Start);

            var start = new Start()
            {
                MaxDataItems = maxDataItems,
                MaxMessageRate = maxMessageRate
            };

            ChannelMetadataRecords.Clear();
            Session.SendMessage(header, start);
        }

        public virtual void ChannelDescribe(IList<string> uris)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelDescribe);

            var channelDescribe = new ChannelDescribe()
            {
                Uris = uris
            };

            Session.SendMessage(header, channelDescribe);
        }

        public virtual void ChannelStreamingStart(IList<ChannelStreamingInfo> channelStreamingInfos)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelStreamingStart);

            var channelStreamingStart = new ChannelStreamingStart()
            {
                Channels = channelStreamingInfos
            };

            Session.SendMessage(header, channelStreamingStart);
        }

        public virtual void ChannelStreamingStop(IList<long> channelIds)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelStreamingStop);

            var channelStreamingStop = new ChannelStreamingStop()
            {
                Channels = channelIds
            };

            Session.SendMessage(header, channelStreamingStop);
        }

        public virtual void ChannelRangeRequest(IList<ChannelRangeInfo> channelRangeInfos)
        {
            var header = CreateMessageHeader(Protocols.ChannelStreaming, MessageTypes.ChannelStreaming.ChannelRangeRequest);

            var channelRangeRequest = new ChannelRangeRequest()
            {
                ChannelRanges = channelRangeInfos
            };

            Session.SendMessage(header, channelRangeRequest);
        }

        public event ProtocolEventHandler<ChannelMetadata> OnChannelMetadata;

        public event ProtocolEventHandler<ChannelData> OnChannelData;

        public event ProtocolEventHandler<ChannelDataChange> OnChannelDataChange;

        public event ProtocolEventHandler<ChannelStatusChange> OnChannelStatusChange;

        public event ProtocolEventHandler<ChannelDelete> OnChannelDelete;

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            switch (header.MessageType)
            {
                case (int)MessageTypes.ChannelStreaming.ChannelMetadata:
                    HandleChannelMetadata(header, decoder.Decode<ChannelMetadata>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelData:
                    HandleChannelData(header, decoder.Decode<ChannelData>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelDataChange:
                    HandleChannelDataChange(header, decoder.Decode<ChannelDataChange>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelStatusChange:
                    HandleChannelStatusChange(header, decoder.Decode<ChannelStatusChange>());
                    break;

                case (int)MessageTypes.ChannelStreaming.ChannelDelete:
                    HandleChannelDelete(header, decoder.Decode<ChannelDelete>());
                    break;

                default:
                    base.HandleMessage(header, decoder);
                    break;
            }
        }

        protected virtual void HandleChannelMetadata(MessageHeader header, ChannelMetadata channelMetadata)
        {
            foreach (var channel in channelMetadata.Channels)
                ChannelMetadataRecords.Add(channel);

            Notify(OnChannelMetadata, header, channelMetadata);
        }

        protected virtual void HandleChannelData(MessageHeader header, ChannelData channelData)
        {
            Notify(OnChannelData, header, channelData);
        }

        protected virtual void HandleChannelDataChange(MessageHeader header, ChannelDataChange channelDataChange)
        {
            Notify(OnChannelDataChange, header, channelDataChange);
        }

        protected virtual void HandleChannelStatusChange(MessageHeader header, ChannelStatusChange channelStatusChange)
        {
            Notify(OnChannelStatusChange, header, channelStatusChange);
        }

        protected virtual void HandleChannelDelete(MessageHeader header, ChannelDelete channelDelete)
        {
            Notify(OnChannelDelete, header, channelDelete);
        }
    }
}
