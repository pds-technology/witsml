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
using log4net;
using PDS.Framework;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Models
{
    /// <summary>
    /// Provides static helper methods that can be used to process <see cref="ChannelDataChunk"/> data.
    /// </summary>
    public static class ChunkExtensions
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ChunkExtensions));

        /// <summary>
        /// Gets a <see cref="ChannelDataReader"/> that can be used to process the <see cref="ChannelDataChunk"/> data.
        /// </summary>
        /// <param name="channelDataChunk">The channel data chunk.</param>
        /// <param name="reverse">if set to <c>true</c> the primary index should be reversed.</param>
        /// <returns></returns>
        public static ChannelDataReader GetReader(this ChannelDataChunk channelDataChunk, bool reverse = false)
        {
            _log.DebugFormat("Creating a ChannelDataReader for a ChannelDataChunk. Reverse: {0}", reverse);

            var mnemonics = ChannelDataReader.Split(channelDataChunk.MnemonicList);
            var units = ChannelDataReader.Split(channelDataChunk.UnitList);
            var nullValues = ChannelDataReader.Split(channelDataChunk.NullValueList);

            return new ChannelDataReader(channelDataChunk.Data, mnemonics, units, nullValues, channelDataChunk.Uri, channelDataChunk.Uid)
                .WithIndices(channelDataChunk.Indices, calculate: reverse, reverse: reverse);
        }

        /// <summary>
        /// Gets the records from each of the chunks.
        /// </summary>
        /// <param name="channelDataChunks">The channel data chunks.</param>
        /// <param name="range">The range.</param>
        /// <param name="ascending">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <param name="reverse">if set to <c>true</c> [reverse].</param>
        /// <returns></returns>
        public static IEnumerable<IChannelDataRecord> GetRecords(this IEnumerable<ChannelDataChunk> channelDataChunks, Range<double?>? range = null, bool ascending = true, bool reverse = false)
        {
            if (channelDataChunks == null) yield break;

            _log.DebugFormat("Getting IChannelDataRecords for all ChannelDataChunks; {0}", range);

            foreach (var chunk in channelDataChunks)
            {
                var records = chunk.GetReader(reverse: reverse).AsEnumerable();

                foreach (var record in records)
                {
                    if (range?.Start != null || range?.End != null)
                    {
                        var index = record.GetIndexValue();

                        if (range.Value.StartsAfter(index, ascending))
                            continue;

                        if (range.Value.EndsBefore(index, ascending))
                            yield break;
                    }

                    yield return record;
                }
            }
        }
    }
}
