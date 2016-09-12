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
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using PDS.Framework;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Defines a method that can be used to retrieve channel data.
    /// </summary>
    public interface IChannelDataProvider
    {
        /// <summary>
        /// Gets the channels metadata.
        /// </summary>
        /// <param name="uris">The collection of URI to describe.</param>
        /// <returns>A collection of channel metadata.</returns>
        IList<ChannelMetadataRecord> GetChannelMetadata(params EtpUri[] uris);

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <returns>A collection of channel data.</returns>
        IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range);

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <param name="mnemonics">The mnemonics to return data for.</param>
        /// <param name="requestLatestValues">The total number of requested latest values.</param>
        /// <returns>A collection of channel data.</returns>
        List<List<List<object>>> GetChannelData(EtpUri uri, Range<double?> range, string[] mnemonics, int? requestLatestValues);

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        void UpdateChannelData(EtpUri uri, ChannelDataReader reader);
    }
}
