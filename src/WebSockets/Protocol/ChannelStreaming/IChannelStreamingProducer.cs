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
    /// <summary>
    /// Defines the interface that must be implemented by the producer role of the ChannelStreaming protocol.
    /// </summary>
    /// <seealso cref="Energistics.Common.IProtocolHandler" />
    [ProtocolRole(Protocols.ChannelStreaming, "producer", "consumer")]
    public interface IChannelStreamingProducer : IProtocolHandler
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is a Simple Streamer.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is a Simple Streamer; otherwise, <c>false</c>.
        /// </value>
        bool IsSimpleStreamer { get; set; }

        /// <summary>
        /// Gets or sets the default describe URI.
        /// </summary>
        /// <value>The default describe URI.</value>
        string DefaultDescribeUri { get; set; }

        /// <summary>
        /// Sends a ChannelMetadata message to a consumer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="channelMetadataRecords">The list of <see cref="ChannelMetadataRecord" /> objects.</param>
        /// <param name="messageFlag">The message flag.</param>
        void ChannelMetadata(MessageHeader request, IList<ChannelMetadataRecord> channelMetadataRecords, MessageFlags messageFlag = MessageFlags.FinalPart);

        /// <summary>
        /// Sends a ChannelData message to a consumer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="dataItems">The list of <see cref="DataItem" /> objects.</param>
        /// <param name="messageFlag">The message flag.</param>
        void ChannelData(MessageHeader request, IList<DataItem> dataItems, MessageFlags messageFlag = MessageFlags.MultiPart);

        /// <summary>
        /// Sends a ChannelDataChange message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="dataItems">The data items.</param>
        void ChannelDataChange(long channelId, long startIndex, long endIndex, IList<DataItem> dataItems);

        /// <summary>
        /// Sends a ChannelStatusChange message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="status">The channel status.</param>
        void ChannelStatusChange(long channelId, ChannelStatuses status);

        /// <summary>
        /// Sends a ChannelDelete message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="reason">The reason.</param>
        void ChannelDelete(long channelId, string reason = null);

        /// <summary>
        /// Handles the Start event from a consumer.
        /// </summary>
        event ProtocolEventHandler<Start> OnStart;

        /// <summary>
        /// Handles the ChannelDescribe event from a consumer.
        /// </summary>
        event ProtocolEventHandler<ChannelDescribe, IList<ChannelMetadataRecord>> OnChannelDescribe;

        /// <summary>
        /// Handles the ChannelStreamingStart event from a consumer.
        /// </summary>
        event ProtocolEventHandler<ChannelStreamingStart> OnChannelStreamingStart;

        /// <summary>
        /// Handles the ChannelStreamingStop event from a consumer.
        /// </summary>
        event ProtocolEventHandler<ChannelStreamingStop> OnChannelStreamingStop;

        /// <summary>
        /// Handles the ChannelRangeRequest event from a consumer.
        /// </summary>
        event ProtocolEventHandler<ChannelRangeRequest> OnChannelRangeRequest;
    }
}
