//----------------------------------------------------------------------- 
// ETP DevKit, 1.2
//
// Copyright 2018 Energistics
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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    /// <summary>
    /// Defines a common interface for an ETP channel streaming producer implementation.
    /// </summary>
    public interface IStreamingProducer : IProtocolHandler
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is a Simple Streamer.
        /// </summary>
        bool IsSimpleStreamer { get; set; }

        /// <summary>
        /// Gets the maximum data items.
        /// </summary>
        int MaxDataItems { get; }

        /// <summary>
        /// Gets the minimum message interval.
        /// </summary>
        int MinMessageInterval { get; }

        /// <summary>
        /// Gets or sets the simple streamer uris.
        /// </summary>
        IList<string> SimpleStreamerUris { get; set; }

        /// <summary>
        /// Initializes the channel streaming producer as a simple streamer.
        /// </summary>
        /// <param name="uris">The uris to stream.</param>
        void InitializeSimpleStreamer(IList<string> uris);

        /// <summary>
        /// Sends a ChannelMetadata message to a consumer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="channelMetadataRecords">The list of <see cref="IChannelMetadataRecord" /> objects.</param>
        /// <param name="messageFlag">The message flag.</param>
        /// <returns>The message identifier.</returns>
        long ChannelMetadata(IMessageHeader request, IList<IChannelMetadataRecord> channelMetadataRecords, MessageFlags messageFlag = MessageFlags.MultiPartAndFinalPart);

        /// <summary>
        /// Sends a ChannelData message to a consumer.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="dataItems">The list of <see cref="IDataItem" /> objects.</param>
        /// <param name="messageFlag">The message flag.</param>
        /// <returns>The message identifier.</returns>
        long ChannelData(IMessageHeader request, IList<IDataItem> dataItems, MessageFlags messageFlag = MessageFlags.MultiPart);

        /// <summary>
        /// Sends a ChannelDataChange message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="dataItems">The data items.</param>
        /// <returns>The message identifier.</returns>
        long ChannelDataChange(long channelId, long startIndex, long endIndex, IList<IDataItem> dataItems);

        /// <summary>
        /// Sends a ChannelStatusChange message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="isActive">if set to <c>true</c> the channel is active.</param>
        /// <returns>The message identifier.</returns>
        long ChannelStatusChange(long channelId, bool isActive);

        /// <summary>
        /// Sends a ChannelRemove message to a consumer.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="reason">The reason.</param>
        /// <returns>The message identifier.</returns>
        long ChannelRemove(long channelId, string reason = null);
    }
}