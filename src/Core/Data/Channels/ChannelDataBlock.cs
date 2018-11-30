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

using System.Collections.Generic;
using System.Linq;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Properties;

namespace PDS.WITSMLstudio.Data.Channels
{
    /// <summary>
    /// Encapsulates a block of Channel Data
    /// </summary>
    public class ChannelDataBlock
    {
        /// <summary>
        /// The default batch size
        /// </summary>
        public static readonly int BatchSize = Settings.Default.ChannelDataBlockBatchSize;

        /// <summary>
        /// The block flush rate in milliseconds
        /// </summary>
        public static readonly int BlockFlushRateInMilliseconds = Settings.Default.ChannelDataBlockFlushRateInMilliseconds;

        private readonly List<List<List<object>>> _records;
        private readonly Dictionary<object, List<List<object>>> _recordsByIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataBlock"/> class.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public ChannelDataBlock(string uri)
        {
            _records = new List<List<List<object>>>();
            _recordsByIndex = new Dictionary<object, List<List<object>>>();
            Indices = new List<ChannelIndexInfo>();
            ChannelIds = new List<long>();
            Mnemonics = new List<string>();
            Units = new List<string>();
            DataTypes = new List<string>();
            NullValues = new List<string>();
            Uri = uri;
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        public string Uri { get; }

        /// <summary>
        /// Gets the indices.
        /// </summary>
        public List<ChannelIndexInfo> Indices { get; }

        /// <summary>
        /// Gets the channel ids.
        /// </summary>
        public List<long> ChannelIds { get; }

        /// <summary>
        /// Gets the mnemonics.
        /// </summary>
        public List<string> Mnemonics { get; }

        /// <summary>
        /// Gets the units.
        /// </summary>
        public List<string> Units { get; }

        /// <summary>
        /// Gets the data types.
        /// </summary>
        public List<string> DataTypes { get; }

        /// <summary>
        /// Gets the null values.
        /// </summary>
        public List<string> NullValues { get; }

        /// <summary>
        /// Adds the index.
        /// </summary>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="dataType">The data type.</param>
        /// <param name="increasing">if set to <c>true</c> if data is incresting, false otherwise.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> if index is time, false otherwise.</param>
        /// <param name="nullValue">The null value.</param>
        public void AddIndex(string mnemonic, string unit, string dataType, bool increasing, bool isTimeIndex, string nullValue = null)
        {
            if (Indices.Any(x => x.Mnemonic.EqualsIgnoreCase(mnemonic)))
                return;

            Indices.Add(new ChannelIndexInfo()
            {
                Mnemonic = mnemonic,
                Increasing = increasing,
                IsTimeIndex = isTimeIndex,
                Unit = unit,
                DataType = dataType,
                NullValue = nullValue
            });
        }

        /// <summary>
        /// Adds the channel.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <param name="unit">The unit.</param>
        /// <param name="dataType">The data type.</param>        
        /// <param name="nullValue">The null value.</param>
        public void AddChannel(long channelId, string mnemonic, string unit, string dataType, string nullValue = null)
        {
            if (Mnemonics.Any(x => x.EqualsIgnoreCase(mnemonic)))
                return;

            ChannelIds.Add(channelId);
            Mnemonics.Add(mnemonic);
            Units.Add(unit);
            DataTypes.Add(dataType);
            NullValues.Add(nullValue);
        }

        /// <summary>
        /// Appends the index and data values for the specified channel identifier.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="indexes">The index values.</param>
        /// <param name="value">The data value.</param>
        public void Append(long channelId, IList<object> indexes, object value)
        {
            var primaryIndex = indexes.First();
            List<List<object>> record;

            // Check if primary index has been added before
            if (!_recordsByIndex.TryGetValue(primaryIndex, out record))
            {
                record = new List<List<object>>()
                {
                    new List<object>() { primaryIndex },
                    new List<object>()
                };

                _records.Add(record);
                _recordsByIndex[primaryIndex] = record;
            }

            var position = ChannelIds.IndexOf(channelId);
            var channelIndexes = record[0];
            var channelValues = record[1];

            // Add the secondary index if not already present
            if (indexes.Count > 1 && channelIndexes.Count < 2)
            {
                channelIndexes.AddRange(indexes.Skip(1));  // Skip 1 for the primary index that was already added.
            }

            // Ensure available channel value slots
            for (int i = channelValues.Count; i < ChannelIds.Count; i++)
                channelValues.Add(null);

            // Channel value
            channelValues[position] = value;
        }

        /// <summary>
        /// The number of records of Channel Data
        /// </summary>
        /// <returns>The record count of channels.</returns>
        public int Count()
        {
            return _records.Count;
        }

        /// <summary>
        /// Gets the reader.
        /// </summary>
        /// <returns>A ChannelDataReader with indices</returns>
        public ChannelDataReader GetReader()
        {
            var records = new List<List<List<object>>>(_records);

            return new ChannelDataReader(records, Mnemonics.ToArray(), Units.ToArray(), DataTypes.ToArray(), NullValues.ToArray(), Uri)
                .WithIndices(Indices, true);
        }

        /// <summary>
        /// Clears the channel data for this instance.
        /// </summary>
        public void Clear()
        {
            _records.Clear();
            _recordsByIndex.Clear();
        }
    }
}
