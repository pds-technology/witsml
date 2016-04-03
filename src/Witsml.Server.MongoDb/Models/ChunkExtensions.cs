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
        public static ChannelDataReader GetReader(this ChannelDataValues channelDataValues)
        {
            var mnemonics = ChannelDataReader.Split(channelDataValues.MnemonicList);
            var units = ChannelDataReader.Split(channelDataValues.UnitList);

            return new ChannelDataReader(channelDataValues.Data, mnemonics, units, channelDataValues.Uid, channelDataValues.Id)
                .WithIndices(channelDataValues.Indices);
        }

        public static ChannelDataReader GetReader(this ChannelDataChunk channelDataChunk)
        {
            var mnemonics = ChannelDataReader.Split(channelDataChunk.MnemonicList);
            var units = ChannelDataReader.Split(channelDataChunk.UnitList);

            return new ChannelDataReader(channelDataChunk.Data, mnemonics, units, channelDataChunk.Uri, channelDataChunk.Id)
                .WithIndices(channelDataChunk.Indices);
        }

        public static IEnumerable<IChannelDataRecord> GetRecords(this IEnumerable<ChannelDataChunk> channelDataChunks, Range<double?>? range = null, bool increasing = true)
        {
            if (channelDataChunks == null)
                yield break;

            foreach (var chunk in channelDataChunks)
            {
                var records = chunk.GetReader().AsEnumerable();

                foreach (var record in records)
                {
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
