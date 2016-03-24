using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Channels
{
    [Export]
    public class ChannelDataChunkAdapter : MongoDbDataAdapter<ChannelDataChunk>
    {
        [ImportingConstructor]
        public ChannelDataChunkAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelDataChunk, ObjectTypes.Id)
        {

        }

        public void Add(ChannelDataReader reader)
        {

            if (reader.RecordsAffected > 0)
            {
                var collection = GetCollection();

                collection.BulkWrite(ToChunks(reader)
                    .Select(dc =>
                    {
                        dc.Id = NewUid();
                        dc.Uid = reader.Uid;
                        dc.MnemonicList = string.Join(",", reader.Mnemonics);
                        dc.UnitList = string.Join(",", reader.Units);
                        return new InsertOneModel<ChannelDataChunk>(dc);
                    }));
            }

        }

        private IEnumerable<ChannelDataChunk> ToChunks(ChannelDataReader reader)
        {
            ChannelIndexInfo indexChannel = reader.Indices.First();
            Range<int>? plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;
            var data = new List<string>();
            string uid = string.Empty;
            var increasing = indexChannel.Increasing;

            while (reader.Read())
            {
                var index = indexChannel.IsTimeIndex ? reader.GetUnixTimeSeconds(0) : reader.GetDouble(0);

                if (!plannedRange.HasValue)
                {
                    plannedRange = ComputeRange(index, ChannelDataReader.RangeSize, increasing);
                    uid = reader.Uid;
                    startIndex = index;
                }

                if (WithinRange(index, plannedRange.Value.End, increasing, false))
                {
                    uid = string.IsNullOrEmpty(uid) ? reader.Uid : uid;
                    data.Add(reader.GetJson());
                    endIndex = index;
                }
                else
                {
                    var newIndex = indexChannel.Clone();
                    newIndex.Start = startIndex;
                    newIndex.End = endIndex;

                    yield return new ChannelDataChunk()
                    {
                        Data = "[" + String.Join(",", data) + "]",
                        Indices = new List<ChannelIndexInfo> { newIndex }
                    };

                    plannedRange = ComputeRange(index, ChannelDataReader.RangeSize, increasing);
                    data = new List<string>();
                    data.Add(reader.GetJson());
                    startIndex = index;
                    endIndex = index;
                    uid = reader.Uid;
                }
            }

            if (data.Count > 0)
            {
                var newIndex = indexChannel.Clone();
                newIndex.Start = startIndex;
                newIndex.End = endIndex;

                yield return new ChannelDataChunk()
                {
                    Data = "[" + String.Join(",", data) + "]",
                    Indices = new List<ChannelIndexInfo> { newIndex }
                };
            }
        }

        /// <summary>
        /// Computes the range.
        /// </summary>
        /// <param name="index">The start index.</param>
        /// <param name="rangeSize">Size of the range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The range.</returns>
        private Range<int> ComputeRange(double index, int rangeSize, bool increasing = true)
        {
            var rangeIndex = increasing ? (int)(Math.Floor(index / rangeSize)) : (int)(Math.Ceiling(index / rangeSize));
            return new Range<int>(rangeIndex * rangeSize, rangeIndex * rangeSize + (increasing ? rangeSize : -rangeSize));
        }

        /// <summary>
        /// Check if the iteration is within the range: includes end point for entire data; excludes end point for chunking.
        /// </summary>
        /// <param name="current">The current index.</param>
        /// <param name="end">The end index.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="closeRange">if set to <c>true</c> [close range].</param>
        /// <returns>True if within range; false if not.</returns>
        private bool WithinRange(double current, double end, bool increasing, bool closeRange = true)
        {
            if (closeRange)
                return increasing ? current <= end : current >= end;
            else
                return increasing ? current < end : current > end;
        }
    }
}
