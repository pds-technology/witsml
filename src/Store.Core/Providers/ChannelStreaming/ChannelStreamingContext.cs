//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.1
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Threading;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    /// <summary>
    /// Defines properties used to configure channel streaming.
    /// </summary>
    public class ChannelStreamingContext
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        public long ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the channel metadata.
        /// </summary>
        public ChannelMetadataRecord ChannelMetadata { get; set; }

        /// <summary>
        /// Gets or sets the parent URI.
        /// </summary>
        public EtpUri ParentUri { get; set; }

        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        public long? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the end index.
        /// </summary>
        public long? EndIndex { get; set; }

        /// <summary>
        /// Gets or sets the last index value processed.
        /// </summary>
        public long? LastIndex { get; set; }

        /// <summary>
        /// Gets or sets the index count before and including the latest index value.
        /// </summary>
        public int IndexCount { get; set; }

        /// <summary>
        /// Gets or sets the real time status of the channel.
        /// </summary>
        public bool IsRealTime { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel streaming.
        /// </summary>
        public ChannelStreamingTypes ChannelStreamingType { get; set; }

        /// <summary>
        /// Gets or sets the range request message header.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the query context.
        /// </summary>
        public string QueryContext { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource TokenSource { get; set; }
    }
}
