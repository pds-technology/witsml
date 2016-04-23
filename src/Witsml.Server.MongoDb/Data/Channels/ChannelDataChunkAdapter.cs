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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.Datatypes;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for channel data.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{ChannelDataChunk}" />
    [Export]
    public class ChannelDataChunkAdapter : MongoDbDataAdapter<ChannelDataChunk>
    {
        private static readonly int RangeSize = Settings.Default.ChannelDataChunkRangeSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataChunkAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public ChannelDataChunkAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectTypes.ChannelDataChunk, ObjectTypes.Id)
        {
            Logger.Debug("Creating instance.");
        }

        /// <summary>
        /// Gets a list of ChannelDataChunk data for a given data object URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="indexChannel">The index channel.</param>
        /// <param name="range">The index range to select data for.</param>
        /// <param name="ascending">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <param name="requestLatestValues">The request latest values.</param>
        /// <returns>A collection of <see cref="List{ChannelDataChunk}" /> items.</returns>
        /// <exception cref="WitsmlException"></exception>
        public List<ChannelDataChunk> GetData(string uri, string indexChannel, Range<double?> range, bool ascending)
        {
            try
            {
                var filter = BuildDataFilter(uri, indexChannel, range, ascending);

                return GetData(filter, ascending);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error getting data: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Adds ChannelDataChunks using the specified reader.
        /// </summary>
        /// <param name="reader">The <see cref="ChannelDataReader"/> used to parse the data.</param>
        public void Add(ChannelDataReader reader)
        {
            Logger.Debug("Adding ChannelDataChunk records with a ChannelDataReader.");

            if (reader == null || reader.RecordsAffected <= 0)
                return;

            try
            {
                BulkWriteChunks(
                    ToChunks(
                        reader.AsEnumerable()),
                    reader.Uri,
                    string.Join(",", reader.Mnemonics),
                    string.Join(",", reader.Units));
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when adding data chunks: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
        }


        /// <summary>
        /// Merges <see cref="ChannelDataChunk"/> data for updates.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public void Merge(ChannelDataReader reader)
        {
            if (reader == null || reader.RecordsAffected <= 0)
                return;

            try
            {
                // Get the full range of the reader.  
                //... This is the range that we need to select existing ChannelDataChunks from the database to update
                var updateRange = reader.GetIndexRange();

                // Make sure we have a valid index range; otherwise, nothing to do
                if (!updateRange.Start.HasValue || !updateRange.End.HasValue)
                    return;

                var indexChannel = reader.GetIndex();
                var increasing = indexChannel.Increasing;

                // Based on the range of the updates, compute the range of the data chunk(s) 
                //... so we can merge updates with existing data.
                var existingRange = new Range<double?>(
                    Range.ComputeRange(updateRange.Start.Value, RangeSize, increasing).Start,
                    Range.ComputeRange(updateRange.End.Value, RangeSize, increasing).End
                );                

                // Get DataChannelChunk list from database for the computed range and URI
                var filter = BuildDataFilter(reader.Uri, indexChannel.Mnemonic, existingRange, increasing);
                var results = GetData(filter, increasing);

                BulkWriteChunks(
                    ToChunks(
                        MergeSequence(results.GetRecords(), reader.AsEnumerable(), updateRange)),
                    reader.Uri,
                    string.Join(",", reader.Mnemonics),
                    string.Join(",", reader.Units));
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when merging data: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Deletes all <see cref="ChannelDataChunk"/> entries for the data object represented by the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public override void Delete(EtpUri uri)
        {
            try
            {
                var filter = Builders<ChannelDataChunk>.Filter.EqIgnoreCase("Uri", uri.Uri);
                GetCollection().DeleteMany(filter);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when deleting data: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Bulks writes <see cref="ChannelDataChunk"/> records for insert and update
        /// </summary>
        /// <param name="chunks">The chunks.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="units">The units.</param>
        private void BulkWriteChunks(IEnumerable<ChannelDataChunk> chunks, string uri, string mnemonics, string units)
        {
            Logger.DebugFormat("Bulk writing ChannelDataChunks for uri '{0}', mnemonics '{1}' and units '{2}'.", uri, mnemonics, units);

            var collection = GetCollection();

            collection.BulkWrite(chunks
                .Select(dc =>
                {
                    if (string.IsNullOrWhiteSpace(dc.Id))
                    {
                        dc.Id = Guid.NewGuid().ToString();
                        dc.Uri = uri;
                        dc.MnemonicList = mnemonics;
                        dc.UnitList = units;

                        return (WriteModel<ChannelDataChunk>) new InsertOneModel<ChannelDataChunk>(dc);
                    }

                    var filter = Builders<ChannelDataChunk>.Filter;
                    var update = Builders<ChannelDataChunk>.Update;

                    return new UpdateOneModel<ChannelDataChunk>(
                        filter.Eq(f => f.Uri, uri) & filter.Eq(f => f.Id, dc.Id),
                        update
                            .Set(u => u.Indices, dc.Indices)
                            .Set(u => u.MnemonicList, mnemonics)
                            .Set(u => u.UnitList, units)
                            .Set(u => u.Data, dc.Data)
                            .Set(u => u.RecordCount, dc.RecordCount));
                })
                .ToList());
        }


        /// <summary>
        /// Combines <see cref="IEnumerable{IChannelDataRecord}"/> data into RangeSize chunks for storage into the database
        /// </summary>
        /// <param name="records">The <see cref="IEnumerable{ChannelDataChunk}"/> records to be chunked.</param>
        /// <returns>An <see cref="IEnumerable{ChannelDataChunk}"/> of channel data.</returns>
        private IEnumerable<ChannelDataChunk> ToChunks(IEnumerable<IChannelDataRecord> records)
        {
            Logger.Debug("Converting ChannelDataRecords to ChannelDataChunks.");

            var data = new List<string>();
            var id = string.Empty;
            ChannelIndexInfo indexChannel = null;
            Range<int>? plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;
            double? previousIndex = null;

            foreach (var record in records)
            {
                indexChannel = record.GetIndex();
                var increasing = indexChannel.Increasing;
                var index = record.GetIndexValue();

                if (previousIndex.HasValue) 
                {
                    if (previousIndex.Value == index)
                    {
                        Logger.ErrorFormat("Data node index repeated for uri: {0}; channel {1}; index: {2}", record.Uri, indexChannel.Mnemonic, index);
                        throw new WitsmlException(ErrorCodes.NodesWithSameIndex);
                    }
                    if (increasing && previousIndex.Value > index || !increasing && previousIndex.Value < index)
                    {
                        var error = string.Format("Data node index not in sequence for uri: {0}: channel: {1}; index: {2}", record.Uri, indexChannel.Mnemonic, index);
                        Logger.Error(error);
                        throw new InvalidOperationException(error);
                    } 
                }
                
                previousIndex = index;

                if (!plannedRange.HasValue)
                {
                    plannedRange = Range.ComputeRange(index, RangeSize, increasing);
                    id = record.Id;
                    startIndex = index;
                }

                // TODO: Can we use this instead? plannedRange.Value.Contains(index, increasing) or a new method?
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
                    Logger.DebugFormat("ChannelDataChunk created with id '{0}', startIndex '{1}' and endIndex '{2}'.", id, startIndex, endIndex);

                    yield return new ChannelDataChunk()
                    {
                        Id = id,
                        Data = "[" + String.Join(",", data) + "]",
                        Indices = new List<ChannelIndexInfo> { newIndex },
                        RecordCount = data.Count
                    };

                    plannedRange = Range.ComputeRange(index, RangeSize, increasing);
                    data = new List<string>() { record.GetJson() };
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
                Logger.DebugFormat("ChannelDataChunk created with id '{0}', startIndex '{1}' and endIndex '{2}'.", id, startIndex, endIndex);

                yield return new ChannelDataChunk()
                {
                    Id = id,
                    Data = "[" + String.Join(",", data) + "]",
                    Indices = new List<ChannelIndexInfo> { newIndex },
                    RecordCount = data.Count
                };
            }
        }

        /// <summary>
        /// Merges two sequences of channel data to update a channel data value "chunk"
        /// </summary>
        /// <param name="existingChunks">The existing channel data chunks.</param>
        /// <param name="updatedChunks">The updated channel data chunks.</param>
        /// <param name="updateRange">The update range.</param>
        /// <returns>The merged sequence of channel data.</returns>
        private IEnumerable<IChannelDataRecord> MergeSequence(
            IEnumerable<IChannelDataRecord> existingChunks,
            IEnumerable<IChannelDataRecord> updatedChunks,
            Range<double?> updateRange)
        {
            Logger.DebugFormat("Merging existing and update ChannelDataRecords for range - start: {0}, end: {1}", updateRange.Start, updateRange.End);

            var id = string.Empty;

            using (var existingEnum = existingChunks.GetEnumerator())
            using (var updateEnum = updatedChunks.GetEnumerator())
            {
                var endOfExisting = !existingEnum.MoveNext();
                var endOfUpdate = !updateEnum.MoveNext();

                // Is log data increasing or decreasing?
                var increasing = !endOfExisting 
                    ? existingEnum.Current.Indices.First().Increasing 
                    : (!endOfUpdate ? updateEnum.Current.Indices.First().Increasing : true);

                while (!(endOfExisting && endOfUpdate))
                {
                    // If the existing data starts after the update data
                    if (!endOfUpdate &&
                        (endOfExisting ||
                        new Range<double?>(existingEnum.Current.GetIndexValue(), null)
                        .StartsAfter(updateEnum.Current.GetIndexValue(), increasing, inclusive: false)))
                    {
                        updateEnum.Current.Id = id;
                        yield return updateEnum.Current;
                        endOfUpdate = !updateEnum.MoveNext();
                    }

                    // Existing value is not contained in the update range
                    else if (!endOfExisting &&
                        (endOfUpdate || 
                        !(new Range<double?>(updateRange.Start.Value, updateRange.End.Value)
                        .Contains(existingEnum.Current.GetIndexValue(), increasing))))
                    {
                        id = endOfExisting ? string.Empty : existingEnum.Current.Id;
                        yield return existingEnum.Current;
                        endOfExisting = !existingEnum.MoveNext();
                    }
                    else // existing and update overlap
                    {
                        if (!endOfExisting && !endOfUpdate)
                        {
                            if (existingEnum.Current.GetIndexValue() == updateEnum.Current.GetIndexValue())
                            {
                                id = existingEnum.Current.Id;
                                var mergedRow = MergeRow(existingEnum.Current, updateEnum.Current);

                                if (mergedRow.HasValues())
                                {
                                    yield return mergedRow;
                                }

                                endOfExisting = !existingEnum.MoveNext();
                                endOfUpdate = !updateEnum.MoveNext();
                            }
                            // Update starts after existing
                            else if ( new Range<double?>(updateEnum.Current.GetIndexValue(), null)
                                .StartsAfter(existingEnum.Current.GetIndexValue(), increasing, inclusive:false))
                            {
                                id = existingEnum.Current.Id;
                                var mergedRow = MergeRow(existingEnum.Current, updateEnum.Current, clear: true);

                                if (mergedRow.HasValues())
                                {
                                    yield return mergedRow;
                                }

                                endOfExisting = !existingEnum.MoveNext();
                            }

                            else // Update Starts Before existing
                            {
                                updateEnum.Current.Id = id;
                                yield return updateEnum.Current;
                                endOfUpdate = !updateEnum.MoveNext();
                            }
                        }
                    }
                }
            }
        }

        private IChannelDataRecord MergeRow(IChannelDataRecord existingRecord, IChannelDataRecord updateRecord, bool clear = false)
        {
            Logger.DebugFormat("Merging existing record with index '{0}' with update record with index '{1}'.", existingRecord.GetIndexValue(), updateRecord.GetIndexValue());

            var existingIndexValue = existingRecord.GetIndexValue();
            var increasing = existingRecord.GetIndex().Increasing;

            for (var i = updateRecord.Indices.Count; i < updateRecord.FieldCount; i++)
            {
                var mnemonicRange = updateRecord.GetChannelIndexRange(i);
                if (mnemonicRange.Contains(existingIndexValue, increasing))
                {
                    existingRecord.SetValue(i, clear ? null : updateRecord.GetValue(i));
                }
            }

            return existingRecord;
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
        /// <param name="ascending">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <returns>The list of channel data chunks that fit the query criteria sorted by the primary index.</returns>
        private List<ChannelDataChunk> GetData(FilterDefinition<ChannelDataChunk> filter, bool ascending)
        {
            var collection = GetCollection();
            var sortBuilder = Builders<ChannelDataChunk>.Sort;
            var sortField = "Indices.0.Start";

            var sort = ascending
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
        /// <param name="uri">The URI of the data object.</param>
        /// <param name="indexChannel">The index channel mnemonic.</param>
        /// <param name="range">The request range.</param>
        /// <param name="ascending">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <returns>The query filter.</returns>
        private FilterDefinition<ChannelDataChunk> BuildDataFilter(string uri, string indexChannel, Range<double?> range, bool ascending)
        {
            var builder = Builders<ChannelDataChunk>.Filter;
            var filters = new List<FilterDefinition<ChannelDataChunk>>();
            var rangeFilters = new List<FilterDefinition<ChannelDataChunk>>();

            filters.Add(builder.EqIgnoreCase("Uri", uri));

            if (range.Start.HasValue)
            {
                var endFilter = ascending
                    ? builder.Gte("Indices.End", range.Start.Value)
                    : builder.Lte("Indices.End", range.Start.Value);
                Logger.DebugFormat("Building end filter with start range '{0}'.", range.Start.Value);

                rangeFilters.Add(endFilter);
            }

            if (range.End.HasValue)
            {
                var startFilter = ascending
                    ? builder.Lte("Indices.Start", range.End)
                    : builder.Gte("Indices.Start", range.End);
                Logger.DebugFormat("Building start filter with end range '{0}'.", range.End.Value);

                rangeFilters.Add(startFilter);
            }

            if (rangeFilters.Count > 0)
            {
                rangeFilters.Add(builder.EqIgnoreCase("Indices.Mnemonic", indexChannel));
                Logger.DebugFormat("Building mnemonic filter with index channel '{0}'.", indexChannel);
            }

            if (rangeFilters.Count > 0)
                filters.Add(builder.And(rangeFilters));

            return builder.And(filters);
        }
    }
}
