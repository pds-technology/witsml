//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using System;

namespace PDS.WITSMLstudio.Data.Channels
{
    /// <summary>
    /// Encapsulates the index information for unified log data chunk
    /// </summary>
    [Serializable]
    public class ChannelIndexInfo
    {
        /// <summary>
        /// Gets or sets the mnemonic.
        /// </summary>
        /// <value>
        /// The mnemonic.
        /// </value>
        public string Mnemonic { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>
        /// The unit.
        /// </value>
        public string Unit { get; set; }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        /// <value>
        /// The data type.
        /// </value>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets the null value.
        /// </summary>
        /// <value>
        /// The null value.
        /// </value>
        public string NullValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ChannelIndexInfo"/> is increasing.
        /// </summary>
        /// <value>
        ///   <c>true</c> if increasing; otherwise, <c>false</c>.
        /// </value>
        public bool Increasing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is time index.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is time index; otherwise, <c>false</c>.
        /// </value>
        public bool IsTimeIndex { get; set; }

        /// <summary>
        /// Gets or sets the start index value.
        /// </summary>
        /// <value>
        /// The start index value.
        /// </value>
        public double Start { get; set; }

        /// <summary>
        /// Gets or sets the end index value.
        /// </summary>
        /// <value>
        /// The end index value.
        /// </value>
        public double End { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public ChannelIndexInfo Clone()
        {
            return (ChannelIndexInfo)MemberwiseClone();
        }
    }
}
