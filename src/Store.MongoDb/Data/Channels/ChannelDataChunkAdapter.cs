//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Transactions;
using PDS.WITSMLstudio.Store.Models;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for channel data.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.MongoDbDataAdapter{ChannelDataChunk}" />
    [Export]
    public class ChannelDataChunkAdapter : MongoDbDataAdapter<ChannelDataChunk>
    {
        private const string ChannelDataChunk = "channelDataChunk";

        /// <summary>The file name</summary>
        public const string FileName = "FileName";

        /// <summary>The bucket name</summary>
        public const string BucketName = "channelData";


        /// <summary>
        /// Encapsulates the results of a chunk-by-chunk search for data within range or within node count limit
        /// </summary>
        public class GetDataFilterResult
        {
            /// <summary>
            /// Number of rows fetched
            /// </summary>
            public int Count { get; set; }
            /// <summary>
            /// Whether the fetch process should continue fetching data as the limiter Func has not been satisfied
            /// </summary>
            public bool Keep { get; set; }
        }

        /// <summary>
        /// Function to provide strict MaxDataNodes limiting for querying channel data chunks
        /// </summary>
        public static readonly Func<ChannelDataChunk, Range<double?>, GetDataFilterResult, GetDataFilterResult> GetDataLogMaxDataNodesGetLimiter =
            (channelDataChunk, range, filterResult) =>
            {
                filterResult.Keep = filterResult.Count <= WitsmlSettings.LogMaxDataNodesGet;
                filterResult.Count += channelDataChunk.RecordCount;
                return filterResult;
            };

        /// <summary>
        /// Function to provide returning channel data chunks until we reach the end, or the specified range limits have been found (whichever happens first)
        /// </summary>
        public static Func<ChannelDataChunk, Range<double?>, GetDataFilterResult, GetDataFilterResult> GetDataSearchUntilFoundOrEndChunkLimiter =
            (channelDataChunk, range, filterResult) =>
            {
                filterResult.Count += channelDataChunk.RecordCount;
                filterResult.Keep = channelDataChunk.Indices[0].End < range.End;
                return filterResult;
            };

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelDataChunkAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public ChannelDataChunkAdapter(IContainer container, IDatabaseProvider databaseProvider) : base(container, databaseProvider, ChannelDataChunk, ObjectTypes.Uid)
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
        /// <param name="reverse">if set to <c>true</c> if the ascending flag was reversed.</param>
        /// <param name="fetchLimiter">user-provided Func that provides the logic for how much data should be inspected to find what we're looking for. null defaults to LogMaxDataNodesGet (limit strictly based on row/node count), also available is GetDataSearchUntilFoundOrEndChunkLimiter which will search up to and including the last chunk which is outside the requested range </param>
        /// <returns>
        /// A collection of <see cref="List{ChannelDataChunk}" /> items.
        /// </returns>
        /// <exception cref="WitsmlException"></exception>
        public List<ChannelDataChunk> GetData(string uri, string indexChannel, Range<double?> range, bool ascending, bool reverse = false, Func<ChannelDataChunk, Range<double?>, GetDataFilterResult, GetDataFilterResult> fetchLimiter = null)
        {
            Logger.DebugFormat("Getting channel data for {0}; Index Channel: {1}; {2}", uri, indexChannel, range);

            try
            {
                var filter = BuildDataFilter(uri, indexChannel, range, reverse ? !ascending : ascending);
                GetDataFilterResult result = new GetDataFilterResult();
                Func<ChannelDataChunk, Range<double?>, GetDataFilterResult, GetDataFilterResult> f =
                    fetchLimiter ?? GetDataLogMaxDataNodesGetLimiter;

                var data = GetData(filter, ascending)
                    .TakeWhile(x => f(x, range, result).Keep )
                    .ToList();

                GetMongoFileData(data);

                return data;
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
        /// <param name="reader">The <see cref="ChannelDataReader" /> used to parse the data.</param>
        /// <exception cref="WitsmlException"></exception>
        public void Add(ChannelDataReader reader)
        {
            if (reader == null || reader.RecordsAffected <= 0) return;

            Logger.Debug("Adding ChannelDataChunk records with a ChannelDataReader.");

            try
            {
                BulkWriteChunks(
                    ToChunks(
                        reader.AsEnumerable()),
                    reader.Uri,
                    string.Join(",", reader.Mnemonics),
                    string.Join(",", reader.Units),
                    string.Join(",", reader.NullValues)
                );

                CreateChannelDataChunkIndex();
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when adding data chunks: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorAddingToDataStore, ex);
            }
            catch (FormatException ex)
            {
                Logger.ErrorFormat("Error when adding data chunks: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorMaxDocumentSizeExceeded, ex);
            }
        }

        /// <summary>
        /// Merges <see cref="ChannelDataChunk" /> data for updates.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <exception cref="WitsmlException"></exception>
        public void Merge(ChannelDataReader reader)
        {
            if (reader == null || reader.RecordsAffected <= 0) return;

            Logger.Debug("Merging records in ChannelDataReader.");

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
                var rangeSize = WitsmlSettings.GetRangeSize(indexChannel.IsTimeIndex);

                // Based on the range of the updates, compute the range of the data chunk(s) 
                //... so we can merge updates with existing data.
                var chunkRange = new Range<double?>(
                      Range.ComputeRange(updateRange.Start.Value, rangeSize, increasing).Start,
                      Range.ComputeRange(updateRange.End.Value, rangeSize, increasing).End
                      );

                // Get DataChannelChunk list from database for the computed range and URI
                //specifically using a chunk limiter that will seek until the end of range is found regardless of the default read limit in the config file
                var results = GetData(reader.Uri, indexChannel.Mnemonic, chunkRange, increasing, false, GetDataSearchUntilFoundOrEndChunkLimiter);

                // Backup existing chunks for the transaction
                AttachChunks(results);

                // Check if reader overlaps existing data
                var hasOverlap = false;
                var existingRange = new Range<double?>();
                var existingMnemonics = results.Count > 0 ? results[0]?.MnemonicList.Split(',') : new string[0];

                if (results.Count > 0)
                {
                    existingRange = new Range<double?>(results.Min(x => x.Indices[0].Start), results.Max(x => x.Indices[0].End));
                    hasOverlap = updateRange.Overlaps(existingRange, increasing);
                }

                try
                {
                    if (hasOverlap)
                    {
                        WriteRecordsToChunks(reader, MergeSequence(results.GetRecords(), reader.AsEnumerable(), updateRange, rangeSize));
                    }
                    else
                    {
                        // If there is no existing data add reader records only
                        if (results.Count == 0)
                        {
                            WriteRecordsToChunks(reader, reader.AsEnumerable());
                        }
                        // If there is only one chunk and the mnemonics match
                        else if (existingMnemonics != null && existingMnemonics.OrderBy(t => t).SequenceEqual(reader.Mnemonics.OrderBy(t => t)) && results.Count == 1)
                        {
                            // If the update is before the existing range
                            if (updateRange.EndsBefore(existingRange.Start.GetValueOrDefault(), increasing, true))
                            {
                                WriteRecordsToChunks(reader, reader.AsEnumerable().Concat(results.GetRecords()));
                            }
                            // If the update is after the existing range
                            else if (updateRange.StartsAfter(existingRange.End.GetValueOrDefault(), increasing, true))
                            {
                                WriteRecordsToChunks(reader, results.GetRecords().Concat(reader.AsEnumerable()));
                            }
                        }
                        // Resort to merging the records
                        else
                        {
                            WriteRecordsToChunks(reader, MergeSequence(results.GetRecords(), reader.AsEnumerable(), updateRange, rangeSize));
                        }
                    }
                    CreateChannelDataChunkIndex();
                }
                catch (FormatException ex)
                {
                    Logger.ErrorFormat("Error when merging data: {0}", ex);
                    throw new WitsmlException(ErrorCodes.ErrorMaxDocumentSizeExceeded, ex);
                }
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when merging data: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        private void WriteRecordsToChunks(ChannelDataReader reader, IEnumerable<IChannelDataRecord> records)
        {
            BulkWriteChunks(
                ToChunks(records),
                reader.Uri,
                string.Join(",", reader.Mnemonics),
                string.Join(",", reader.Units),
                string.Join(",", reader.NullValues)
            );
        }

        private void AttachChunks(IEnumerable<ChannelDataChunk> chunks)
        {
            chunks.ForEach(cdc =>
                Transaction?.Attach(MongoDbAction.Update, DbCollectionName, IdPropertyName, cdc.ToBsonDocument(),
                    new EtpUri(cdc.Uid)));
            Transaction?.Save();
        }

        /// <summary>
        /// Deletes all <see cref="ChannelDataChunk"/> entries for the data object represented by the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        public override void Delete(EtpUri uri)
        {
            Logger.DebugFormat("Deleting channel data for {0}", uri);

            try
            {
                var filter = Builders<ChannelDataChunk>.Filter.Eq("Uri", uri.Uri.ToLower());
                DeleteMongoFiles(filter);
                GetCollection().DeleteMany(filter);
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when deleting data: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorDeletingFromDataStore, ex);
            }
        }

        /// <summary>
        /// Partially deletes log data for specified channels and ranges.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="indexCurve">The index curve.</param>
        /// <param name="increasing">if set to <c>true</c> if the log data is increasing.</param>
        /// <param name="isTimeLog">if set to <c>true</c> the log is time indexed.</param>
        /// <param name="deletedChannels">The deleted channels.</param>
        /// <param name="ranges">The ranges.</param>
        /// <param name="updatedRanges">The updated ranges.</param>
        public void PartialDeleteLogData(EtpUri uri, string indexCurve, bool increasing, bool isTimeLog, List<string> deletedChannels, Dictionary<string, Range<double?>> ranges, Dictionary<string, Range<double?>> updatedRanges)
        {
            try
            {
                // Get DataChannelChunk list from database for the log
                var filter = BuildDataFilter(uri, indexCurve, new Range<double?>(null, null), increasing);
                var results = GetData(filter, increasing).ToList();

                // Backup existing chunks for the transaction
                AttachChunks(results);

                var channelRanges = new Dictionary<string, List<double?>>();
                foreach (var range in ranges)
                {
                    channelRanges.Add(range.Key, new List<double?> { null, null });
                }
                if (!channelRanges.ContainsKey(indexCurve))
                    channelRanges.Add(indexCurve, new List<double?> { null, null });

                BulkWriteChunks(PartialDeleteChunks(results, deletedChannels, ranges, channelRanges, increasing), uri, null, null, null);

                foreach (var chanelRange in channelRanges)
                {
                    var rangeValues = chanelRange.Value;
                    updatedRanges.Add(chanelRange.Key, new Range<double?>(rangeValues[0], rangeValues[1]));
                }
            }
            catch (MongoException ex)
            {
                Logger.ErrorFormat("Error when deleting data: {0}", ex);
                throw new WitsmlException(ErrorCodes.ErrorUpdatingInDataStore, ex);
            }
        }

        /// <summary>
        /// Gets the total data row count from channelDataChunks for the specified uri.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The total data row count.</returns>
        public int GetDataRowCount(EtpUri uri)
        {
            var filter = Builders<ChannelDataChunk>.Filter.Eq("Uri", uri.Uri.ToLower());
            var dataRowCountName = "DataRowCount";
            var dataRowCount = 0;

            var aggregate = GetCollection().Aggregate().Match(filter).Group(new BsonDocument {{"_id", ""}, {dataRowCountName, new BsonDocument("$sum", "$RecordCount")} });
            var dataRowCountDocument = aggregate.ToList().FirstOrDefault();

            if (dataRowCountDocument != null)
            {
                dataRowCount = dataRowCountDocument[dataRowCountName].AsInt32;
            }

            return dataRowCount;
        }

        /// <summary>
        /// Bulks writes <see cref="ChannelDataChunk" /> records for insert and update
        /// </summary>
        /// <param name="chunks">The chunks.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="units">The units.</param>
        /// <param name="nullValues">The null values.</param>
        private void BulkWriteChunks(IEnumerable<ChannelDataChunk> chunks, string uri, string mnemonics, string units, string nullValues)
        {
            Logger.DebugFormat("Bulk writing ChannelDataChunks for uri '{0}', mnemonics '{1}' and units '{2}'.", uri, mnemonics, units);

            var transaction = Transaction;
            var collection = GetCollection();
            var uriLower = uri.ToLower();

            var writeModels = chunks
                .Select(dc =>
                {
                    if (dc == null)
                        return null;

                    if (string.IsNullOrWhiteSpace(dc.Uid))
                    {
                        dc.Uid = Guid.NewGuid().ToString();
                        dc.Uri = uriLower;
                        dc.MnemonicList = mnemonics;
                        dc.UnitList = units;
                        dc.NullValueList = nullValues;

                        if (transaction != null)
                        {
                            var chunk = new ChannelDataChunk { Uid = dc.Uid };
                            transaction.Attach(MongoDbAction.Add, DbCollectionName, IdPropertyName, chunk.ToBsonDocument(), new EtpUri(dc.Uid));
                        }

                        UpdateMongoFile(dc);

                        return (WriteModel<ChannelDataChunk>)new InsertOneModel<ChannelDataChunk>(dc);
                    }

                    if (dc.Indices != null)
                    {
                        var filter = Builders<ChannelDataChunk>.Filter;
                        var update = Builders<ChannelDataChunk>.Update;

                        UpdateMongoFile(dc);

                        return new UpdateOneModel<ChannelDataChunk>(
                            filter.Eq(f => f.Uri, uriLower) & filter.Eq(f => f.Uid, dc.Uid),
                            update
                                .Set(u => u.Indices, dc.Indices)
                                .Set(u => u.MnemonicList, dc.MnemonicList)
                                .Set(u => u.UnitList, dc.UnitList)
                                .Set(u => u.NullValueList, dc.NullValueList)
                                .Set(u => u.Data, dc.Data)
                                .Set(u => u.RecordCount, dc.RecordCount));
                    }

                    // Delete data chunk
                    transaction?.Attach(MongoDbAction.Delete, DbCollectionName, IdPropertyName, dc.ToBsonDocument(), new EtpUri(dc.Uri));

                    var chunkFilter = Builders<ChannelDataChunk>.Filter;
                    var mongoFileFilter = Builders<ChannelDataChunk>.Filter.Eq("Uri", dc.Uri);

                    DeleteMongoFiles(mongoFileFilter);

                    return
                        new DeleteOneModel<ChannelDataChunk>(chunkFilter.Eq(f => f.Uri, uriLower) &
                                                             chunkFilter.Eq(f => f.Uid, dc.Uid));
                })
                .Where(wm => wm != null)
                .ToList();

            if (writeModels.Count > 0)
                collection.BulkWrite(writeModels);

            transaction?.Save();
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
            List<ChannelIndexInfo> indexes = null;
            ChannelIndexInfo indexChannel = null;
            Range<long>? plannedRange = null;
            double startIndex = 0;
            double endIndex = 0;
            double? previousIndex = null;
            IList<string> chunkMnemonics = null;
            IList<string> chunkUnits = null;
            IList<string> chunkNullValues = null;
            var chunkSettingsSet = false;

            foreach (var record in records)
            {
                indexChannel = record.GetIndex();
                indexes = record.Indices.Select(x => x.Clone()).ToList();
                var increasing = indexChannel.Increasing;
                var index = record.GetIndexValue();
                var rangeSize = WitsmlSettings.GetRangeSize(indexChannel.IsTimeIndex);

                if (previousIndex.HasValue)
                {
                    if (previousIndex.Value == index)
                    {
                        Logger.ErrorFormat("Data node index repeated for uri: {0}; channel {1}; index: {2}", record.Uri, indexChannel.Mnemonic, index);
                        throw new WitsmlException(ErrorCodes.NodesWithSameIndex);
                    }
                    if (increasing && previousIndex.Value > index || !increasing && previousIndex.Value < index)
                    {
                        var error = $"Data node index not in sequence for uri: {record.Uri}: channel: {indexChannel.Mnemonic}; index: {index}";
                        Logger.Error(error);
                        throw new InvalidOperationException(error);
                    }
                }

                previousIndex = index;

                if (!plannedRange.HasValue)
                {
                    plannedRange = Range.ComputeRange(index, rangeSize, increasing);
                    Logger.Debug($"Computed planned range: {plannedRange}");
                    id = record.Id;
                    startIndex = index;
                }

                // TODO: Can we use this instead? plannedRange.Value.Contains(index, increasing) or a new method?
                if (WithinRange(index, plannedRange.Value.End, increasing, false))
                {
                    id = string.IsNullOrEmpty(id) ? record.Id : id;
                    data.Add(record.GetJson());
                    endIndex = index;

                    if (chunkSettingsSet)
                        continue;

                    chunkMnemonics = record.Mnemonics;
                    chunkUnits = record.Units;
                    chunkNullValues = record.NullValues;
                    chunkSettingsSet = true;
                }
                else
                {
                    //var newIndex = indexChannel.Clone();
                    //newIndex.Start = startIndex;
                    //newIndex.End = endIndex;
                    indexes[0].Start = startIndex;
                    indexes[0].End = endIndex;

                    Logger.DebugFormat("ChannelDataChunk created with id '{0}', startIndex '{1}' and endIndex '{2}'.", id, startIndex, endIndex);

                    yield return new ChannelDataChunk
                    {
                        Uid = id,
                        Data = "[" + String.Join(",", data) + "]",
                        //Indices = new List<ChannelIndexInfo> { newIndex },
                        Indices = indexes,
                        RecordCount = data.Count,
                        MnemonicList = string.Join(",", chunkMnemonics),
                        UnitList = string.Join(",", chunkUnits),
                        NullValueList = string.Join(",", chunkNullValues)
                    };

                    plannedRange = Range.ComputeRange(index, rangeSize, increasing);
                    Logger.Debug($"Computed planned range: {plannedRange}");
                    data = new List<string> { record.GetJson() };
                    startIndex = index;
                    endIndex = index;
                    id = record.Id;
                    chunkMnemonics = record.Mnemonics;
                    chunkUnits = record.Units;
                    chunkNullValues = record.NullValues;
                }
            }

            if (data.Count > 0 && indexes != null)
            {
                //var newIndex = indexChannel.Clone();
                //newIndex.Start = startIndex;
                //newIndex.End = endIndex;
                indexes = indexes.Select(x => x.Clone()).ToList();
                indexes[0].Start = startIndex;
                indexes[0].End = endIndex;

                Logger.DebugFormat("ChannelDataChunk created with id '{0}', startIndex '{1}' and endIndex '{2}'.", id, startIndex, endIndex);

                var chunk = new ChannelDataChunk
                {
                    Uid = id,
                    Data = "[" + string.Join(",", data) + "]",
                    //Indices = new List<ChannelIndexInfo> { newIndex },
                    Indices = indexes,
                    RecordCount = data.Count
                };

                if (chunkMnemonics != null)
                {
                    chunk.MnemonicList = string.Join(",", chunkMnemonics);
                    chunk.UnitList = string.Join(",", chunkUnits);
                    chunk.NullValueList = string.Join(",", chunkNullValues);
                }

                yield return chunk;
            }
        }

        private IEnumerable<ChannelDataChunk> PartialDeleteChunks(IEnumerable<ChannelDataChunk> chunks, List<string> deletedChannels, Dictionary<string, Range<double?>> ranges, Dictionary<string, List<double?>> updatedRanges, bool increasing)
        {
            foreach (var chunk in chunks)
            {
                yield return PartialDeleteDataChunk(chunk, deletedChannels, ranges, updatedRanges, increasing);
            }
        }

        private ChannelDataChunk PartialDeleteDataChunk(ChannelDataChunk chunk, List<string> deletedChannels, Dictionary<string, Range<double?>> ranges, Dictionary<string, List<double?>> updatedRanges, bool increasing)
        {
            var indexChannel = chunk.Indices.FirstOrDefault();
            var indexRange = updatedRanges[indexChannel.Mnemonic];
            var reader = chunk.GetReader();

            var channelRanges = reader.GetChannelRanges();
            if (!ToPartialDeleteChunk(reader, deletedChannels, channelRanges, ranges, updatedRanges, increasing))
            {
                foreach (var updatedRange in updatedRanges)
                {
                    if (!channelRanges.ContainsKey(updatedRange.Key))
                        continue;

                    var update = updatedRange.Value;
                    if (!update[0].HasValue)
                        update[0] = channelRanges[updatedRange.Key].Start;
                    update[1] = channelRanges[updatedRange.Key].End;
                }

                if (!indexRange[0].HasValue)
                    indexRange[0] = indexChannel.Start;
                indexRange[1] = indexChannel.End;

                return null;
            }

            var data = new List<string>();
            var records = reader.AsEnumerable();

            double? start = null;
            double? end = null;

            using (var existingEum = records.GetEnumerator())
            {
                var endOfExisting = !existingEum.MoveNext();

                while (!endOfExisting)
                {
                    existingEum.Current.PartialDeleteRecord(deletedChannels, ranges, updatedRanges, increasing);
                    if (existingEum.Current.HasValues())
                    {
                        indexChannel = existingEum.Current.GetIndex();
                        var index = existingEum.Current.GetIndexValue();
                        if (!start.HasValue)
                            start = index;
                        end = index;

                        data.Add(existingEum.Current.GetJson());
                    }

                    endOfExisting = !existingEum.MoveNext();
                }
            }

            if (data.Count == 0)
            {
                chunk.Indices = null;
                return chunk;
            }

            var newChunk = new ChannelDataChunk
            {
                Uid = chunk.Uid,
                MnemonicList = chunk.MnemonicList,
                UnitList = chunk.UnitList,
                NullValueList = chunk.NullValueList,
                Uri = chunk.Uri.ToLower()
            };

            var indexes = chunk.Indices.Select(x => x.Clone()).ToList();
            indexes[0].Start = start.GetValueOrDefault();
            indexes[0].End = end.GetValueOrDefault();
            newChunk.Indices = indexes;

            DeleteChunkChannels(newChunk, deletedChannels);
            newChunk.RecordCount = data.Count;
            newChunk.Data = "[" + string.Join(",", data) + "]";

            if (!indexRange[0].HasValue)
                indexRange[0] = indexes[0].Start;
            indexRange[1] = indexes[0].End;

            return newChunk;
        }

        private bool ToPartialDeleteChunk(ChannelDataReader reader, List<string> deletedChannels, Dictionary<string, Range<double?>> channelRanges, Dictionary<string, Range<double?>> ranges, Dictionary<string, List<double?>> updatedRanges, bool increasing)
        {
            var updatedChannels = ranges.Keys.ToList();

            if (!reader.Mnemonics.Any(m => deletedChannels.Contains(m) || updatedChannels.Contains(m)))
                return false;

            var toPartialDelete = false;

            foreach (var channelRange in channelRanges)
            {
                if (!ranges.ContainsKey(channelRange.Key))
                    continue;

                var range = ranges[channelRange.Key];
                if (!RangesOverlap(channelRange.Value, range, increasing))
                    continue;

                toPartialDelete = true;
                break;
            }

            return toPartialDelete;
        }

        private bool RangesOverlap(Range<double?> current, Range<double?> update, bool increasing)
        {
            return !StartsBefore(current.End.GetValueOrDefault(), update.Start.GetValueOrDefault(), increasing)
                && !StartsBefore(update.End.GetValueOrDefault(), current.Start.GetValueOrDefault(), increasing);
        }

        private bool StartsBefore(double a, double b, bool increasing)
        {
            return increasing
                ? a < b
                : a > b;
        }

        private void DeleteChunkChannels(ChannelDataChunk chunk, List<string> deletedChannels)
        {
            var separator = ',';
            var mnemonics = chunk.MnemonicList.Split(separator);
            var units = chunk.UnitList.Split(separator);
            var nullValues = chunk.NullValueList.Split(separator);

            var newMnemonics = new List<string>();
            var newUnits = new List<string>();
            var newNullValues = new List<string>();

            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                if (deletedChannels.Contains(mnemonic))
                    continue;

                newMnemonics.Add(mnemonic);
                newUnits.Add(units[i]);
                newNullValues.Add(nullValues[i]);
            }

            chunk.MnemonicList = string.Join(separator.ToString(), newMnemonics);
            chunk.UnitList = string.Join(separator.ToString(), newUnits);
            chunk.NullValueList = string.Join(separator.ToString(), newNullValues);
        }

        /// <summary>
        /// Merges two sequences of channel data to update a channel data value "chunk"
        /// </summary>
        /// <param name="existingChunks">The existing channel data chunks.</param>
        /// <param name="updatedChunks">The updated channel data chunks.</param>
        /// <param name="updateRange">The update range.</param>
        /// <param name="rangeSize">The chunk range.</param>
        /// <returns>The merged sequence of channel data.</returns>
        private IEnumerable<IChannelDataRecord> MergeSequence(
            IEnumerable<IChannelDataRecord> existingChunks,
            IEnumerable<IChannelDataRecord> updatedChunks,
            Range<double?> updateRange,
            long rangeSize)
        {
            Logger.DebugFormat("Merging existing and update ChannelDataRecords: {0}", updateRange);

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
                        existingEnum.Current?.MergeRecord(updateEnum.Current, rangeSize, IndexOrder.After, increasing);
                        Logger.Debug($"(1) Keeping update with id: '{updateEnum.Current.Id}' at index: {updateEnum.Current.GetValue(0)}");
                        yield return updateEnum.Current;
                        endOfUpdate = !updateEnum.MoveNext();
                    }
                    // Existing value is not contained in the update range
                    else if (!endOfExisting &&
                             (endOfUpdate ||
                              !(new Range<double?>(updateRange.Start.Value, updateRange.End.Value)
                                  .Contains(existingEnum.Current.GetIndexValue(), increasing))))
                    {
                        if (updateEnum.Current != null)
                            existingEnum.Current.MergeRecord(updateEnum.Current, rangeSize, IndexOrder.Before, increasing);

                        Logger.Debug($"(2) Keeping existing with id: '{existingEnum.Current.Id}' at index: {existingEnum.Current.GetValue(0)}");
                        yield return existingEnum.Current;
                        endOfExisting = !existingEnum.MoveNext();
                    }
                    else // existing and update overlap
                    {
                        if (!endOfExisting && !endOfUpdate)
                        {
                            if (existingEnum.Current.GetIndexValue() == updateEnum.Current.GetIndexValue())
                            {
                                existingEnum.Current.MergeRecord(updateEnum.Current, rangeSize, IndexOrder.Same, increasing);

                                if (existingEnum.Current.HasValues())
                                {
                                    Logger.Debug($"(3) Keeping existing with id: '{existingEnum.Current.Id}' at index: {existingEnum.Current.GetValue(0)}");
                                    yield return existingEnum.Current;
                                }

                                endOfExisting = !existingEnum.MoveNext();
                                endOfUpdate = !updateEnum.MoveNext();
                            }
                            // Update starts after existing
                            else if (new Range<double?>(updateEnum.Current.GetIndexValue(), null)
                                .StartsAfter(existingEnum.Current.GetIndexValue(), increasing, inclusive: false))
                            {
                                existingEnum.Current.MergeRecord(updateEnum.Current, rangeSize, IndexOrder.Before, increasing);

                                if (existingEnum.Current.HasValues())
                                {
                                    Logger.Debug($"(4) Keeping existing with id: '{existingEnum.Current.Id}' at index: {existingEnum.Current.GetValue(0)}");
                                    yield return existingEnum.Current;
                                }

                                endOfExisting = !existingEnum.MoveNext();
                            }
                            else // Update Starts Before existing
                            {
                                existingEnum.Current?.MergeRecord(updateEnum.Current, rangeSize, IndexOrder.After, increasing);

                                if (updateEnum.Current.HasValues())
                                {
                                    Logger.Debug($"(5) Keeping update with id: '{updateEnum.Current.Id}' at index: {updateEnum.Current.GetValue(0)}");
                                    yield return updateEnum.Current;
                                }

                                endOfUpdate = !updateEnum.MoveNext();
                            }
                        }
                    }
                }
            }
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

            return increasing ? current < end : current > end;
        }

        /// <summary>
        /// Gets the channel data stored in a unified format.
        /// </summary>
        /// <param name="filter">The data filter.</param>
        /// <param name="ascending">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <returns>The list of channel data chunks that fit the query criteria sorted by the primary index.</returns>
        internal IEnumerable<ChannelDataChunk> GetData(FilterDefinition<ChannelDataChunk> filter, bool ascending)
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
                Logger.DebugFormat("Channel Data query filters: {0}", filterJson);
            }

            return collection
                .Find(filter ?? "{}")
                .Sort(sort)
                .ToEnumerable();
        }

        /// <summary>
        /// Builds the data filter for the database query.
        /// </summary>
        /// <param name="uri">The URI of the data object.</param>
        /// <param name="indexChannel">The index channel mnemonic.</param>
        /// <param name="range">The request range.</param>
        /// <param name="ascending">if set to <c>true</c> the data will be sorted in ascending order.</param>
        /// <returns>The query filter.</returns>
        internal FilterDefinition<ChannelDataChunk> BuildDataFilter(string uri, string indexChannel, Range<double?> range, bool ascending)
        {
            var builder = Builders<ChannelDataChunk>.Filter;
            var filters = new List<FilterDefinition<ChannelDataChunk>>();
            var rangeFilters = new List<FilterDefinition<ChannelDataChunk>>();

            filters.Add(builder.Eq("Uri", uri.ToLower()));

            if (range.Start.HasValue)
            {
                var endFilter = ascending
                    ? builder.Gte("Indices.0.End", range.Start.Value)
                    : builder.Lte("Indices.0.End", range.Start.Value);

                Logger.DebugFormat("Building end filter with start range '{0}'.", range.Start.Value);
                rangeFilters.Add(endFilter);
            }

            if (range.End.HasValue)
            {
                var startFilter = ascending
                    ? builder.Lte("Indices.0.Start", range.End)
                    : builder.Gte("Indices.0.Start", range.End);

                Logger.DebugFormat("Building start filter with end range '{0}'.", range.End.Value);
                rangeFilters.Add(startFilter);
            }

            if (rangeFilters.Count > 0)
                filters.Add(builder.And(rangeFilters));

            return builder.And(filters);
        }

        private void CreateChannelDataChunkIndex()
        {
            var keys = Builders<ChannelDataChunk>.IndexKeys.Ascending("Uri").Ascending("Indices.0.Start").Ascending("Indices.0.End");
            GetCollection().Indexes.CreateOneAsync(new CreateIndexModel<ChannelDataChunk>(keys));

            var uri = Builders<ChannelDataChunk>.IndexKeys.Ascending("Uri");
            GetCollection().Indexes.CreateOneAsync(new CreateIndexModel<ChannelDataChunk>(uri));

            var start = Builders<ChannelDataChunk>.IndexKeys.Ascending("Indices.0.Start");
            GetCollection().Indexes.CreateOneAsync(new CreateIndexModel<ChannelDataChunk>(start));

            var end = Builders<ChannelDataChunk>.IndexKeys.Ascending("Indices.0.End");
            GetCollection().Indexes.CreateOneAsync(new CreateIndexModel<ChannelDataChunk>(end));
        }

        private void UpdateMongoFile(ChannelDataChunk dc)
        {
            Logger.DebugFormat("Updating MongoDb Channel Data files: {0}", dc.Uri);

            var bucket = GetMongoFileBucket();

            if (dc.Data.Length >= WitsmlSettings.MaxDataLength)
            {
                var bytes = Encoding.UTF8.GetBytes(dc.Data);

                var loadOptions = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { FileName, dc.Uid },
                        { "DataBytes", bytes.Length }
                    }
                };

                if (!string.IsNullOrEmpty(dc.Uid))
                    DeleteMongoFile(bucket, dc.Uid);

                bucket.UploadFromBytes(dc.Uid, bytes, loadOptions);
                dc.Data = null;
            }
            else
            {
                if (!string.IsNullOrEmpty(dc.Uid))
                    DeleteMongoFile(bucket, dc.Uid);
            }
        }

        private void DeleteMongoFiles(FilterDefinition<ChannelDataChunk> filter)
        {
            Logger.Debug("Deleting MongoDb Channel Data files.");

            var filters = new List<FilterDefinition<ChannelDataChunk>>
            {
                filter,
                Builders<ChannelDataChunk>.Filter.Eq(c => c.Data, null)
            };

            var chunkFilter = Builders<ChannelDataChunk>.Filter.And(filters);
            var chunks = GetData(chunkFilter, true);

            var bucket = GetMongoFileBucket();

            foreach (var chunk in chunks)
            {
                DeleteMongoFile(bucket, chunk.Uid);
            }
        }

        private void DeleteMongoFile(IGridFSBucket bucket, string fileId)
        {
            Logger.DebugFormat("Deleting MongoDb Channel Data file: {0}", fileId);

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileName], fileId);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return;

            bucket.Delete(mongoFile.Id);
        }

        private void GetMongoFileData(IEnumerable<ChannelDataChunk> dcList)
        {
            Logger.Debug("Getting MongoDb Channel Data files.");

            var bucket = GetMongoFileBucket();

            foreach (var dc in dcList.Where(c => string.IsNullOrEmpty(c.Data)))
            {
                var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileName], dc.Uid);
                var mongoFile = bucket.Find(filter).FirstOrDefault();

                if (mongoFile == null)
                    continue;

                var bytes = bucket.DownloadAsBytes(mongoFile.Id);
                dc.Data = Encoding.UTF8.GetString(bytes);
            }
        }

        private IGridFSBucket GetMongoFileBucket()
        {
            var db = DatabaseProvider.GetDatabase();
            return new GridFSBucket(db, new GridFSBucketOptions
            {
                BucketName = BucketName,
                ChunkSizeBytes = WitsmlSettings.ChunkSizeBytes
            });
        }
    }
}
