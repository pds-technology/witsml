using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
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
                BulkWriteChunks(ToChunks(reader.AsEnumerable()), reader.Uid, reader.Mnemonics, reader.Units);
            }
        }

        private void BulkWriteChunks(IEnumerable<ChannelDataChunk> chunks, string uid, string[] mnemonics, string[] units)
        {
            var collection = GetCollection();

            collection.BulkWrite(chunks
                .Select(dc =>
                {
                    dc.Id = NewUid();
                    dc.Uid = uid;
                    dc.MnemonicList = string.Join(",", mnemonics);
                    dc.UnitList = string.Join(",", units);
                    return new InsertOneModel<ChannelDataChunk>(dc);
                })
                .ToList());
        }

        private IEnumerable<ChannelDataChunk> ToChunks(IEnumerable<IChannelDataRecord> records)
        {
            var data = new List<string>();
            var uid = string.Empty;
            ChannelIndexInfo indexChannel = null;
            Range<int>? plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;

            foreach (var record in records)
            {
                indexChannel = record.Indices.First();
                var increasing = indexChannel.Increasing;

                double index = GetIndexValue(record);

                if (!plannedRange.HasValue)
                {
                    plannedRange = ComputeRange(index, ChannelDataReader.RangeSize, increasing);
                    uid = record.Uid;
                    startIndex = index;
                }

                if (WithinRange(index, plannedRange.Value.End, increasing, false))
                {
                    uid = string.IsNullOrEmpty(uid) ? record.Uid : uid;
                    data.Add(record.GetJson());
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
                    data.Add(record.GetJson());
                    startIndex = index;
                    endIndex = index;
                    uid = record.Uid;
                }
            }

            if (data.Count > 0 && indexChannel != null)
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

        private static double GetIndexValue(IChannelDataRecord channelDataRecord)
        {
            var indexChannel = channelDataRecord.Indices.First();
            var increasing = indexChannel.Increasing;

            return indexChannel.IsTimeIndex
                ? channelDataRecord.GetUnixTimeSeconds(0)
                : channelDataRecord.GetDouble(0);
        }

        public void Merge(ChannelDataReader reader)
        {
            //reader.GetIndexRange(int i = 0);
            var indexChannel = reader.Indices.First();
            var increasing = indexChannel.Increasing;

            // Get the full range of the reader.  
            //... This is the range that we need to select existing ChannelDataChunks from the database to update
            var updateRange = reader.GetIndexRange();

            // Get DataChannelChunk list from database for the computed range and Uid
            var filter = BuildDataFilter(reader.Uid, reader.GetName(0), updateRange, increasing);
            var results = GetData(filter, increasing);

            // TODO: Modify to handle multiple ChannelDataChunks
            var existingDataReader = results.First().GetReader();

            if (reader.RecordsAffected > 0)
            {
                BulkWriteChunks(
                    ToChunks(
                        MergeSequence(existingDataReader.AsEnumerable(), reader.AsEnumerable())), 
                    reader.Uid, 
                    reader.Mnemonics, 
                    reader.Units);
            }
        }

        /// <summary>
        /// Merges two sequences of log data to update a log data value "chunk"
        /// </summary>
        /// <param name="existingLogDataSequence">The existing log data sequence.</param>
        /// <param name="updateLogDataSequence">The update log data sequence.</param>
        /// <returns>The merged sequence of data</returns>
        private IEnumerable<IChannelDataRecord> MergeSequence(
            IEnumerable<IChannelDataRecord> existingLogDataSequence,
            IEnumerable<IChannelDataRecord> updateLogDataSequence)
        {
            string uid = string.Empty;

            using (var existingEnum = existingLogDataSequence.GetEnumerator())
            using (var updateEnum = updateLogDataSequence.GetEnumerator())
            {
                var endOfExisting = !existingEnum.MoveNext();
                var endOfUpdate = !updateEnum.MoveNext();

                while (!(endOfExisting && endOfUpdate))
                {
                    uid = endOfExisting ? string.Empty : existingEnum.Current.Uid;

                    if (!endOfExisting && (endOfUpdate || GetIndexValue(existingEnum.Current) < GetIndexValue(updateEnum.Current)))
                    {
                        yield return existingEnum.Current;
                        endOfExisting = !existingEnum.MoveNext();
                    }
                    else
                    {
                        updateEnum.Current.Uid = uid;
                        yield return updateEnum.Current;
                        if (!endOfExisting && GetIndexValue(existingEnum.Current) == GetIndexValue(updateEnum.Current))
                        {
                            endOfExisting = !existingEnum.MoveNext();
                        }
                        endOfUpdate = !updateEnum.MoveNext();
                    }
                }
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

        /// <summary>
        /// Builds the data filter for the database query.
        /// </summary>
        /// <param name="uid">The uid of the log.</param>
        /// <param name="indexCurve">The index curve mnemonic.</param>
        /// <param name="range">The request range.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The query filter.</returns>
        private FilterDefinition<ChannelDataChunk> BuildDataFilter(string uid, string indexCurve, Range<double> range, bool increasing)
        {
            var filters = new List<FilterDefinition<ChannelDataChunk>>();
            filters.Add(Builders<ChannelDataChunk>.Filter.EqIgnoreCase("Uid", uid));

            var rangeFilters = new List<FilterDefinition<ChannelDataChunk>>();
            var filter = increasing ?
                Builders<ChannelDataChunk>.Filter.Gte("Indices.End", range.Start) :
                Builders<ChannelDataChunk>.Filter.Lte("Indices.End", range.Start);
            rangeFilters.Add(filter);

            filter = increasing ?
                Builders<ChannelDataChunk>.Filter.Lte("Indices.Start", range.End) :
                Builders<ChannelDataChunk>.Filter.Gte("Indices.Start", range.End);
            rangeFilters.Add(filter);

            if (rangeFilters.Count > 0)
                rangeFilters.Add(Builders<ChannelDataChunk>.Filter.EqIgnoreCase("Indices.Mnemonic", indexCurve));

            if (rangeFilters.Count > 0)
                filters.Add(Builders<ChannelDataChunk>.Filter.And(rangeFilters));

            return Builders<ChannelDataChunk>.Filter.And(filters);
        }

        /// <summary>
        /// Gets the log data from channelDataValues collection.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The list of log data chunks that fit the query criteria sorted by the start index.</returns>
        private List<ChannelDataChunk> GetData(FilterDefinition<ChannelDataChunk> filter, bool increasing)
        {
            var collection = GetCollection();
            var sortBuilder = Builders<ChannelDataChunk>.Sort;
            var sortField = "Indices.0.Start";

            var sort = increasing
                ? sortBuilder.Ascending(sortField)
                : sortBuilder.Descending(sortField);

            if (Logger.IsDebugEnabled)
            {
                var filterJson = filter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Data query filters: {0}", filterJson);
            }

            return collection
                .Find(filter ?? "{}")
                .Sort(sort)
                .ToList();
        }
    }
}
