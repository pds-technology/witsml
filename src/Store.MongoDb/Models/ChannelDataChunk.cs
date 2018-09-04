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

using System;
using System.Collections.Generic;
using PDS.WITSMLstudio.Data.Channels;

namespace PDS.WITSMLstudio.Store.Models
{
    /// <summary>
    /// Model class for unified log data
    /// </summary>
    [Serializable]
    public class ChannelDataChunk
    {
        /// <summary>
        /// Gets or sets the URI of the parent data object.
        /// </summary>
        /// <value>The parent data object URI.</value>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="ChannelIndexInfo"/>.
        /// </summary>
        /// <value>The indices to handle multiple indices in a channel set for 2.0 log.</value>
        public List<ChannelIndexInfo> Indices { get; set; }

        /// <summary>
        /// Gets or sets the channel data values.
        /// </summary>
        /// <value>The data in JSON format.</value>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the comma separated mnemonic list.
        /// </summary>
        /// <value>The mnemonic list.</value>
        public string MnemonicList { get; set; }

        /// <summary>
        /// Gets or sets the comma separated unit list.
        /// </summary>
        /// <value>The unit list.</value>
        public string UnitList { get; set; }

        /// <summary>
        /// Gets or sets the null value list.
        /// </summary>
        /// <value>
        /// The null value list.
        /// </value>
        public string NullValueList { get; set; }

        /// <summary>
        /// Gets or sets the record count.
        /// </summary>
        /// <value>
        /// The record count.
        /// </value>
        public int RecordCount { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>
        /// The uid.
        /// </value>
        public string Uid { get; set; }
    }
}
