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

using Energistics.Etp.Common.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Container class to hold an Index and Channel value DataItem
    /// </summary>
    public struct IndexedDataItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexedDataItem"/> struct.
        /// </summary>
        /// <param name="indexDataItem">The index data item.</param>
        /// <param name="valueDataItem">The value data item.</param>
        public IndexedDataItem(IDataItem indexDataItem, IDataItem valueDataItem)
        {
            IndexDataItem = indexDataItem;
            ValueDataItem = valueDataItem;
        }

        /// <summary>
        /// Gets the index data item.
        /// </summary>
        /// <value>
        /// The index data item.
        /// </value>
        public IDataItem IndexDataItem { get; }

        /// <summary>
        /// Gets the channel value data item.
        /// </summary>
        /// <value>
        /// The value data item.
        /// </value>
        public IDataItem ValueDataItem { get; }
    }
}
