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

using PDS.WITSMLstudio.Data.Channels;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Encapsulates data for responses to WITSML Store API methods.
    /// </summary>
    public class ResponseContext : IQueryContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseContext"/> class.
        /// </summary>
        public ResponseContext()
        {
            ObjectType = string.Empty;
        }

        /// <summary>
        /// Gets or sets the type of the object for the response.
        /// </summary>
        /// <value>
        /// The type of the object.
        /// </value>
        public string ObjectType { get; set; }

        /// <summary>
        /// Gets the maximum return nodes OptionsIn from the parser.
        /// </summary>
        /// <value>
        /// The maximum return nodes.
        /// </value>
        public int? MaxReturnNodes { get; set; }

        /// <summary>
        /// Gets or sets the request latest values OptionsIn from the parser.
        /// </summary>
        /// <value>
        /// The request latest values.
        /// </value>
        public int? RequestLatestValues { get; set; }

        /// <summary>
        /// Gets the current query maximum data nodes and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The query maximum data nodes.
        /// </value>
        public int MaxDataNodes { get; set; }

        /// <summary>
        /// Gets the current query maximum data points and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The query maximum data points.
        /// </value>
        public int MaxDataPoints { get; set; }

        /// <summary>
        /// Gets the response maximum data nodes.
        /// </summary>
        /// <value>
        /// The response maximum data nodes.
        /// </value>
        public int TotalMaxDataNodes { get; set; }

        /// <summary>
        /// Gets the current response data node total and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The response data node total.
        /// </value>
        public int TotalDataNodes { get; set; }

        /// <summary>
        /// Gets the current response data point total and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The response data point total.
        /// </value>
        public int TotalDataPoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether data has been truncated.
        /// </summary>
        /// <value>
        ///   <c>true</c> if data has been truncated; otherwise, <c>false</c>.
        /// </value>
        public bool DataTruncated { get; set; }

        /// <summary>
        /// Gets a value indicating whether all requested values have been found.
        /// </summary>
        /// <value>
        /// <c>true</c> if all requested values have been found; otherwise, <c>false</c>.
        /// </value>
        public bool HasAllRequestedValues { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance</returns>
        public IQueryContext Clone()
        {
            return MemberwiseClone() as ResponseContext;
        }
    }
}
