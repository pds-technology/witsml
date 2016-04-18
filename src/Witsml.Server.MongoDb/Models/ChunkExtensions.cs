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
using PDS.Framework;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Models
{
    public static class ChunkExtensions
    {
        public static ChannelDataReader GetReader(this ChannelDataChunk channelDataChunk, bool reverseSort = false)
        {
            var mnemonics = ChannelDataReader.Split(channelDataChunk.MnemonicList);
            var units = ChannelDataReader.Split(channelDataChunk.UnitList);

            return new ChannelDataReader(channelDataChunk.Data, mnemonics, units, channelDataChunk.Uri, channelDataChunk.Id)
                .WithIndices(channelDataChunk.Indices, calculate: reverseSort, reverseSort: reverseSort);
        }

        public static IEnumerable<IChannelDataRecord> GetRecords(this IEnumerable<ChannelDataChunk> channelDataChunks, Range<double?>? range = null, bool increasing = true, int? requestLatestValues = null)
        {
            if (channelDataChunks == null)
                yield break;

            var breakValue = requestLatestValues.HasValue ? requestLatestValues.Value : 0;

            foreach (var chunk in channelDataChunks)
            {
                if (requestLatestValues.HasValue && breakValue <= 0)
                    yield break;

                var records = chunk.GetReader(reverseSort: requestLatestValues.HasValue).AsEnumerable();

                foreach (var record in records)
                {
                    if (requestLatestValues.HasValue && breakValue <= 0)
                        yield break;

                    if (requestLatestValues.HasValue)
                        breakValue =- 1;

                        if (range.HasValue)
                    {
                        var index = record.GetIndexValue();

                        if (range.Value.StartsAfter(index, increasing))
                            continue;

                        if (range.Value.EndsBefore(index, increasing))
                            yield break;
                    }

                    yield return record;
                }
            }
        }
    }
}
