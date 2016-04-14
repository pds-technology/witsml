//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.Linq;
using PDS.Framework;
using PDS.Witsml.Properties;

namespace PDS.Witsml.Data.Channels
{
    public class ChannelDataBlock
    {
        public static readonly int BatchSize = Settings.Default.ChannelDataBlockBatchSize;

        private readonly List<List<List<object>>> _records;
        private readonly Dictionary<double, List<List<object>>> _recordsByIndex;

        public ChannelDataBlock(string uri)
        {
            _records = new List<List<List<object>>>();
            _recordsByIndex = new Dictionary<double, List<List<object>>>();
            Indices = new List<ChannelIndexInfo>();
            ChannelIds = new List<long>();
            Mnemonics = new List<string>();
            Units = new List<string>();
            Uri = uri;
        }

        public string Uri { get; private set; }

        public List<ChannelIndexInfo> Indices { get; private set; }

        public List<long> ChannelIds { get; private set; }

        public List<string> Mnemonics { get; private set; }

        public List<string> Units { get; private set; }

        public void AddIndex(string mnemonic, string unit, bool increasing, bool isTimeIndex)
        {
            if (Indices.Any(x => x.Mnemonic.EqualsIgnoreCase(mnemonic)))
                return;

            Indices.Add(new ChannelIndexInfo()
            {
                Mnemonic = mnemonic,
                Increasing = increasing,
                IsTimeIndex = isTimeIndex,
                Unit = unit
            });
        }

        public void AddChannel(long channelId, string mnemonic, string unit)
        {
            if (Mnemonics.Any(x => x.EqualsIgnoreCase(mnemonic)))
                return;

            ChannelIds.Add(channelId);
            Mnemonics.Add(mnemonic);
            Units.Add(unit);
        }

        public void Append(long channelId, IList<double> indexes, object value)
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

            // Secondary indexes
            if (indexes.Count > 1)
            {
                channelIndexes.AddRange(indexes.Cast<object>());
            }

            // Ensure available channel value slots
            for (int i = channelValues.Count; i < ChannelIds.Count; i++)
                channelValues.Add(null);

            // Channel value
            channelValues[position] = value;
        }

        public int Count()
        {
            return _records.Count;
        }

        public ChannelDataReader GetReader()
        {
            var records = new List<List<List<object>>>(_records);

            return new ChannelDataReader(records, Mnemonics.ToArray(), Units.ToArray(), Uri)
                .WithIndices(Indices, true);
        }

        public void Clear()
        {
            _records.Clear();
            _recordsByIndex.Clear();
        }
    }
}
