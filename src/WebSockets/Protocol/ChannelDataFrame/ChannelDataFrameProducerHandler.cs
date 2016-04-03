//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
