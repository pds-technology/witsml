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
    [ProtocolRole(Protocols.ChannelStreaming, "consumer", "producer")]
    public interface IChannelStreamingConsumer : IProtocolHandler
    {
        void Start(int maxDataItems = 10000, int maxMessageRate = 1000);

        void ChannelDescribe(IList<string> uris);

        void ChannelStreamingStart(IList<ChannelStreamingInfo> channelStreamingInfos);

        void ChannelStreamingStop(IList<long> channelIds);

        void ChannelRangeRequest(IList<ChannelRangeInfo> channelRangeInfos);

        event ProtocolEventHandler<ChannelMetadata> OnChannelMetadata;

        event ProtocolEventHandler<ChannelData> OnChannelData;

        event ProtocolEventHandler<ChannelDataChange> OnChannelDataChange;

        event ProtocolEventHandler<ChannelStatusChange> OnChannelStatusChange;

        event ProtocolEventHandler<ChannelDelete> OnChannelDelete;
    }
}
