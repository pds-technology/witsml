//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace PDS.Witsml.Server.Providers.ChannelStreaming
{
    /// <summary>
    /// Defines properties used to configure channel streaming.
    /// </summary>
    public class ChannelStreamingContext
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>
        /// The channel identifier.
        /// </value>
        public long ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the channel metadata.
        /// </summary>
        /// <value>
        /// The channel metadata.
        /// </value>
        public ChannelMetadataRecord ChannelMetadata { get; set; }

        /// <summary>
        /// Gets or sets the parent URI.
        /// </summary>
        /// <value>
        /// The parent URI.
        /// </value>
        public EtpUri ParentUri { get; set; }

        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>
        /// The start index.
        /// </value>
        public long? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the end index.
        /// </summary>
        /// <value>
        /// The end index.
        /// </value>
        public long? EndIndex { get; set; }

        /// <summary>
        /// Gets or sets the index count.
        /// </summary>
        /// <value>
        /// The index count before and including the latest index value.
        /// </value>
        public int IndexCount { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel streaming.
        /// </summary>
        /// <value>
        /// The type of the channel streaming.
        /// </value>
        public ChannelStreamingTypes ChannelStreamingType { get; set; }

        /// <summary>
        /// Gets or sets the range request message header.
        /// </summary>
        /// <value>
        /// The range request message header.
        /// </value>
        public MessageHeader RangeRequestHeader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether change notification 
        /// should be received by the requestor.
        /// </summary>
        /// <value>
        /// <c>true</c> if change notification should be received by the requestor; 
        /// otherwise, <c>false</c>.
        /// </value>
        public bool ReceiveChangeNotification { get; set; }
    }
}
