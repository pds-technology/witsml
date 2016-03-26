using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.MongoDb;

namespace PDS.Witsml.Server.Data.Channels
{
    [Export]
    public class ChannelDataChunkAdapter : MongoDbDataAdapter<ChannelDataChunk>
    {
        private static readonly int RangeSize = Settings.Default.LogIndexRangeSize;

        [ImportingConstructor]
        public ChannelDataChunkAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelDataChunk, ObjectTypes.Id)
        {

        }

        public List<ChannelDataChunk> GetData(string uid, string indexCurve, Range<double?> range, bool increasing)
        {
            var filter = BuildDataFilter(uid, indexCurve, range, increasing);
            return GetData(filter, increasing);
        }

        public void Add(ChannelDataReader reader)
        {
            if (reader == null || reader.RecordsAffected <= 0)
                return;

            BulkWriteChunks(
                ToChunks(
                    reader.AsEnumerable()), 
                reader.Uid,
                string.Join(",", reader.Mnemonics),
                string.Join(",", reader.Units));
        }

        public void Merge(ChannelDataReader reader)
        {
            if (reader == null || reader.RecordsAffected <= 0)
                return;

            //reader.GetIndexRange(int i = 0);
            var indexChannel = reader.GetIndex();
            var increasing = indexChannel.Increasing;

            // Get the full range of the reader.  
            //... This is the range that we need to select existing ChannelDataChunks from the database to update
            var updateRange = reader.GetIndexRange();

            // Get DataChannelChunk list from database for the computed range and Uid
            var filter = BuildDataFilter(reader.Uid, indexChannel.Mnemonic, updateRange, increasing);
            var results = GetData(filter, increasing);

            BulkWriteChunks(
                ToChunks(
                    MergeSequence(results.GetRecords(), reader.AsEnumerable())),
                reader.Uid,
                string.Join(",", reader.Mnemonics),
                string.Join(",", reader.Units));
        }

        private void BulkWriteChunks(IEnumerable<ChannelDataChunk> chunks, string uid, string mnemonics, string units)
        {
            var collection = GetCollection();

            collection.BulkWrite(chunks
                .Select(dc =>
                {
                    if (string.IsNullOrWhiteSpace(dc.Id))
                    {
                        dc.Id = NewUid();
                        dc.Uid = uid;
                        dc.MnemonicList = mnemonics;
                        dc.UnitList = units;

                        return (WriteModel<ChannelDataChunk>) new InsertOneModel<ChannelDataChunk>(dc);
                    }

                    var filter = Builders<ChannelDataChunk>.Filter;
                    var update = Builders<ChannelDataChunk>.Update;

                    return new UpdateOneModel<ChannelDataChunk>(
                        filter.Eq(f => f.Uid, uid) & filter.Eq(f => f.Id, dc.Id),
                        update
                            .Set(u => u.Indices, dc.Indices)
                            .Set(u => u.MnemonicList, mnemonics)
                            .Set(u => u.UnitList, units)
                            .Set(u => u.Data, dc.Data));
                })
                .ToList());
        }

        private IEnumerable<ChannelDataChunk> ToChunks(IEnumerable<IChannelDataRecord> records)
        {
            var data = new List<string>();
            var id = string.Empty;
            ChannelIndexInfo indexChannel = null;
            Range<int>? plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;

            foreach (var record in records)
            {
                indexChannel = record.GetIndex();
                var increasing = indexChannel.Increasing;

                double index = record.GetIndexValue();

                if (!plannedRange.HasValue)
                {
                    plannedRange = ComputeRange(index, RangeSize, increasing);
                    id = record.Id;
                    startIndex = index;
                }

                // TODO: Can we use this instead? plannedRange.Value.Contains(index, increasing);
                if (WithinRange(index, plannedRange.Value.End, increasing, false))
                {
                    id = string.IsNullOrEmpty(id) ? record.Id : id;
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
                        Id = id,
                        Data = "[" + String.Join(",", data) + "]",
                        Indices = new List<ChannelIndexInfo> { newIndex }
                    };

                    plannedRange = ComputeRange(index, RangeSize, increasing);
                    data = new List<string>();
                    data.Add(record.GetJson());
                    startIndex = index;
                    endIndex = index;
                    id = record.Id;
                }
            }

            if (data.Count > 0 && indexChannel != null)
            {
                var newIndex = indexChannel.Clone();
                newIndex.Start = startIndex;
                newIndex.End = endIndex;

                yield return new ChannelDataChunk()
                {
                    Id = id,
                    Data = "[" + String.Join(",", data) + "]",
                    Indices = new List<ChannelIndexInfo> { newIndex }
                };
            }
        }

        /// <summary>
        /// Merges two sequences of channel data to update a channel data value "chunk"
        /// </summary>
        /// <param name="existingChunks">The existing channel data chunks.</param>
        /// <param name="updatedChunks">The updated channel data chunks.</param>
        /// <returns>The merged sequence of channel data.</returns>
        private IEnumerable<IChannelDataRecord> MergeSequence(
            IEnumerable<IChannelDataRecord> existingChunks,
            IEnumerable<IChannelDataRecord> updatedChunks)
        {
            string id = string.Empty;

            using (var existingEnum = existingChunks.GetEnumerator())
            using (var updateEnum = updatedChunks.GetEnumerator())
            {
                var endOfExisting = !existingEnum.MoveNext();
                var endOfUpdate = !updateEnum.MoveNext();

                while (!(endOfExisting && endOfUpdate))
                {
                    id = endOfExisting ? string.Empty : existingEnum.Current.Id;

                    if (!endOfExisting && (endOfUpdate || ExistingBefore(
                                                            existingEnum.Current.GetIndexValue(),
                                                            updateEnum.Current.GetIndexValue(), 
                                                            existingEnum.Current.GetIndex().Increasing)))
                    {
                        yield return existingEnum.Current;
                        endOfExisting = !existingEnum.MoveNext();
                    }
                    else
                    {
                        updateEnum.Current.Id = id;
                        yield return updateEnum.Current;

                        if (!endOfExisting && existingEnum.Current.GetIndexValue() == updateEnum.Current.GetIndexValue())
                        {
                            endOfExisting = !existingEnum.MoveNext();
                        }

                        endOfUpdate = !updateEnum.MoveNext();
                    }
                }
            }
        }

        private bool ExistingBefore(double existingValue, double updateValue, bool increasing)
        {
            return increasing
                ? existingValue < updateValue
                : existingValue >= updateValue;
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
        /// Gets the channel data stored in a unified format.
        /// </summary>
        /// <param name="filter">The data filter.</param>
        /// <param name="increasing">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <returns>The list of channel data chunks that fit the query criteria sorted by the primary index.</returns>
        private List<ChannelDataChunk> GetData(FilterDefinition<ChannelDataChunk> filter, bool increasing)
        {
            var collection = GetCollection();
            var sortBuilder = Builders<ChannelDataChunk>.Sort;
            var sortField = "Indices.0.Start";

            var sort = increasing
                ? sortBuilder.Ascending(sortField)
                : sortBuilder.Descending(sortField);

            if (Logger.IsDebugEnabled && filter != null)
            {
                var filterJson = filter.Render(collection.DocumentSerializer, collection.Settings.SerializerRegistry);
                Logger.DebugFormat("Data query filters: {0}", filterJson);
            }

            return collection
                .Find(filter ?? "{}")
                .Sort(sort)
                .ToList();
        }

        /// <summary>
        /// Builds the data filter for the database query.
        /// </summary>
        /// <param name="uid">The uid of the data object.</param>
        /// <param name="indexCurve">The index curve mnemonic.</param>
        /// <param name="range">The request range.</param>
        /// <param name="increasing">if set to <c>true</c> the index is increasing.</param>
        /// <returns>The query filter.</returns>
        private FilterDefinition<ChannelDataChunk> BuildDataFilter(string uid, string indexCurve, Range<double?> range, bool increasing)
        {
            var builder = Builders<ChannelDataChunk>.Filter;
            var filters = new List<FilterDefinition<ChannelDataChunk>>();
            var rangeFilters = new List<FilterDefinition<ChannelDataChunk>>();

            filters.Add(builder.EqIgnoreCase("Uid", uid));

            if (range.Start.HasValue)
            {
                var endFilter = increasing
                    ? builder.Gte("Indices.End", range.Start.Value)
                    : builder.Lte("Indices.End", range.Start.Value);
                rangeFilters.Add(endFilter);
            }

            if (range.End.HasValue)
            {
                var startFilter = increasing
                    ? builder.Lte("Indices.Start", range.End)
                    : builder.Gte("Indices.Start", range.End);
                rangeFilters.Add(startFilter);
            }

            if (rangeFilters.Count > 0)
                rangeFilters.Add(builder.EqIgnoreCase("Indices.Mnemonic", indexCurve));

            if (rangeFilters.Count > 0)
                filters.Add(builder.And(rangeFilters));

            return builder.And(filters);
        }
    }
}
