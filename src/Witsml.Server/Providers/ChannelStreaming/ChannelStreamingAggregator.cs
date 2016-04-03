//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Avro.IO;
using Energistics.Common;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Protocol;
using Energistics.Protocol.ChannelStreaming;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ChannelStreamingAggregator : EtpProtocolHandler, IChannelStreamingProducer, IChannelStreamingConsumer
    {
        private readonly IChannelStreamingProducer _producer;
        private readonly IChannelStreamingConsumer _consumer;

        [ImportingConstructor]
        public ChannelStreamingAggregator(IChannelStreamingProducer producer, IChannelStreamingConsumer consumer) : base(Protocols.ChannelStreaming, "producer,consumer")
        {
            _producer = producer;
            _consumer = consumer;
        }

        public override IEtpSession Session
        {
            set
            {
                base.Session = value;
                _producer.Session = value;
                _consumer.Session = value;
            }
        }

        #region IChannelStreamingProducer

        public static readonly MessageTypes.ChannelStreaming[] ProducerMessageTypes =
        {
            MessageTypes.ChannelStreaming.ChannelMetadata,
            MessageTypes.ChannelStreaming.ChannelData,
            MessageTypes.ChannelStreaming.ChannelDataChange,
            MessageTypes.ChannelStreaming.ChannelStatusChange,
            MessageTypes.ChannelStreaming.ChannelDelete
        };

        public event ProtocolEventHandler<Start> OnStart
        {
            add { _producer.OnStart += value; }
            remove { _producer.OnStart -= value; }
        }

        public event ProtocolEventHandler<ChannelDescribe, IList<ChannelMetadataRecord>> OnChannelDescribe
        {
            add { _producer.OnChannelDescribe += value; }
            remove { _producer.OnChannelDescribe -= value; }
        }

        public event ProtocolEventHandler<ChannelStreamingStart> OnChannelStreamingStart
        {
            add { _producer.OnChannelStreamingStart += value; }
            remove { _producer.OnChannelStreamingStart -= value; }
        }

        public event ProtocolEventHandler<ChannelStreamingStop> OnChannelStreamingStop
        {
            add { _producer.OnChannelStreamingStop += value; }
            remove { _producer.OnChannelStreamingStop -= value; }
        }

        public event ProtocolEventHandler<ChannelRangeRequest> OnChannelRangeRequest
        {
            add { _producer.OnChannelRangeRequest += value; }
            remove { _producer.OnChannelRangeRequest -= value; }
        }

        public void ChannelMetadata(MessageHeader request, IList<ChannelMetadataRecord> channelMetadataRecords)
        {
            _producer.ChannelMetadata(request, channelMetadataRecords);
        }

        public void ChannelData(MessageHeader request, IList<DataItem> dataItems)
        {
            _producer.ChannelData(request, dataItems);
        }

        public void ChannelDataChange(long channelId, long startIndex, long endIndex, IList<DataItem> dataItems)
        {
            _producer.ChannelDataChange(channelId, startIndex, endIndex, dataItems);
        }

        public void ChannelStatusChange(long channelId, ChannelStatuses status)
        {
            _producer.ChannelStatusChange(channelId, status);
        }

        public void ChannelDelete(long channelId, string reason = null)
        {
            _producer.ChannelDelete(channelId, reason);
        }

        #endregion IChannelStreamingProducer

        #region IChannelStreamingConsumer

        public static readonly MessageTypes.ChannelStreaming[] ConsumerMessageTypes =
        {
            MessageTypes.ChannelStreaming.Start,
            MessageTypes.ChannelStreaming.ChannelDescribe,
            MessageTypes.ChannelStreaming.ChannelStreamingStart,
            MessageTypes.ChannelStreaming.ChannelStreamingStop,
            MessageTypes.ChannelStreaming.ChannelRangeRequest
        };

        public event ProtocolEventHandler<ChannelMetadata> OnChannelMetadata
        {
            add { _consumer.OnChannelMetadata += value; }
            remove { _consumer.OnChannelMetadata -= value; }
        }

        public event ProtocolEventHandler<ChannelData> OnChannelData
        {
            add { _consumer.OnChannelData += value; }
            remove { _consumer.OnChannelData -= value; }
        }

        public event ProtocolEventHandler<ChannelDataChange> OnChannelDataChange
        {
            add { _consumer.OnChannelDataChange += value; }
            remove { _consumer.OnChannelDataChange -= value; }
        }

        public event ProtocolEventHandler<ChannelStatusChange> OnChannelStatusChange
        {
            add { _consumer.OnChannelStatusChange += value; }
            remove { _consumer.OnChannelStatusChange -= value; }
        }

        public event ProtocolEventHandler<ChannelDelete> OnChannelDelete
        {
            add { _consumer.OnChannelDelete += value; }
            remove { _consumer.OnChannelDelete -= value; }
        }

        public void Start(int maxDataItems = 10000, int maxMessageRate = 1000)
        {
            _consumer.Start(maxDataItems, maxMessageRate);
        }

        public void ChannelDescribe(IList<string> uris)
        {
            _consumer.ChannelDescribe(uris);
        }

        public void ChannelStreamingStart(IList<ChannelStreamingInfo> channelStreamingInfos)
        {
            _consumer.ChannelStreamingStart(channelStreamingInfos);
        }

        public void ChannelStreamingStop(IList<long> channelIds)
        {
            _consumer.ChannelStreamingStop(channelIds);
        }

        public void ChannelRangeRequest(IList<ChannelRangeInfo> channelRangeInfos)
        {
            _consumer.ChannelRangeRequest(channelRangeInfos);
        }

        #endregion IChannelStreamingConsumer

        protected override void HandleMessage(MessageHeader header, Decoder decoder)
        {
            var messageType = (MessageTypes.ChannelStreaming)header.MessageType;

            if (ProducerMessageTypes.Contains(messageType))
            {
                _consumer.HandleMessage(header, decoder);
            }
            else
            {
                _producer.HandleMessage(header, decoder);
            }
        }
    }
}
