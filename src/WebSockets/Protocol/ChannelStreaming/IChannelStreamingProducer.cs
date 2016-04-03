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
