//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
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

using Energistics.Datatypes.ChannelData;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
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
        public IndexedDataItem(DataItem indexDataItem, DataItem valueDataItem)
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
        public DataItem IndexDataItem { get; private set; }

        /// <summary>
        /// Gets the channel value data item.
        /// </summary>
        /// <value>
        /// The value data item.
        /// </value>
        public DataItem ValueDataItem { get; private set; }
    }
}
