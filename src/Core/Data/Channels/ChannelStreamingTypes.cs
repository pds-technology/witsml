//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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

namespace PDS.WITSMLstudio.Data.Channels
{
    /// <summary>
    /// An enumeration of Channel Streaming Types
    /// </summary>
    public enum ChannelStreamingTypes
    {
        /// <summary>
        /// Channel streaming start - latest value
        /// </summary>
        LatestValue,

        /// <summary>
        /// Channel streaming start - index count values before 
        /// and including the latest value.
        /// </summary>
        IndexCount,

        /// <summary>
        /// Channel streaming start - Stream from index value
        /// </summary>
        IndexValue,

        /// <summary>
        /// Channel range request
        /// </summary>
        RangeRequest,

        /// <summary>
        /// Channel Streaming directly to Real-Time for ChannelDataLoad processing
        /// </summary>
        ChannelDataLoad
    }
}
