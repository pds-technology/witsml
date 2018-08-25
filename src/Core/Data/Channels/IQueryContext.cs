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
    /// Defines properties and methods used by the ChannelDataReader to control how data is returned.  
    /// </summary>
    public interface IQueryContext
    {
        /// <summary>
        /// Gets or sets the request latest values OptionsIn.
        /// </summary>
        /// <value>
        /// The request latest values.
        /// </value>
        int? RequestLatestValues { get; set; }

        /// <summary>
        /// Gets the current query maximum data nodes .
        /// </summary>
        /// <value>
        /// The query maximum data nodes.
        /// </value>
        int MaxDataNodes { get; set; }

        /// <summary>
        /// Gets the current query maximum data points.
        /// </summary>
        /// <value>
        /// The query maximum data points.
        /// </value>
        int MaxDataPoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether data has been truncated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if data has been truncated; otherwise, <c>false</c>.
        /// </value>
        bool DataTruncated { get; set; }

        /// <summary>
        /// Gets a value indicating whether all requested values have been found.
        /// </summary>
        /// <value>
        /// <c>true</c> if all requested values have been found; otherwise, <c>false</c>.
        /// </value>
        bool HasAllRequestedValues { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance</returns>
        IQueryContext Clone();
    }
}
