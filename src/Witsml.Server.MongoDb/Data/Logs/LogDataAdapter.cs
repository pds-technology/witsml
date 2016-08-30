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
using Energistics.DataAccess;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Data.Transactions;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for Log objects.
    /// </summary>
    /// <typeparam name="T">The data object type</typeparam>
    /// <typeparam name="TChild">The type of the child.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{T}" />
    /// <seealso cref="PDS.Witsml.Server.Data.Channels.IChannelDataProvider" />
    public abstract class LogDataAdapter<T, TChild> : MongoDbDataAdapter<T>, IChannelDataProvider where T : IWellboreObject where TChild : IUniqueId
    {
        private readonly bool _streamIndexValuePairs = WitsmlSettings.StreamIndexValuePairs;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogDataAdapter{T, TChild}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        protected LogDataAdapter(IContainer container, IDatabaseProvider databaseProvider, string dbCollectionName) : base(container, databaseProvider, dbCollectionName)
        {
        }

        /// <summary>
        /// Gets or sets the channel data chunk adapter.
        /// </summary>
        /// <value>The channel data chunk adapter.</value>
        [Import]
        public ChannelDataChunkAdapter ChannelDataChunkAdapter { get; set; }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            var logs = QueryEntities(parser);

            if (parser.IncludeLogData())
            {
                ValidateGrowingObjectDataRequest(parser, logs);

                var logHeaders = GetEntities(logs.Select(x => x.GetUri()))
                    .ToDictionary(x => x.GetUri());

                logs.ForEach(l =>
                {
                    var logHeader = logHeaders[l.GetUri()];
                    var mnemonics = GetMnemonicList(logHeader, parser);

                    // Query the log data
                    QueryLogDataValues(l, logHeader, parser, mnemonics, context);

                    FormatLogHeader(l, mnemonics.Values.ToArray());
                });
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                logs.ForEach(l =>
                {
                    var mnemonics = GetMnemonicList(l, parser);
                    FormatLogHeader(l, mnemonics.Values.ToArray());
                });
            }

            return logs;
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            UpdateLogDataAndIndexRange(uri, new[] { reader });
        }

        /// <summary>
        /// Gets the channel metadata for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(EtpUri uri)
        {
            var entity = GetEntity(uri);
            var logCurves = GetLogCurves(entity);
            var metadata = new List<ChannelMetadataRecord>();
            var index = 0;

            if (!logCurves.Any())
                return metadata;

            var mnemonic = GetIndexCurveMnemonic(entity);
            var indexCurve = logCurves.FirstOrDefault(x => GetMnemonic(x).EqualsIgnoreCase(mnemonic));
            var indexMetadata = ToIndexMetadataRecord(entity, indexCurve);

            // Skip the indexCurve if StreamIndexValuePairs setting is false
            metadata.AddRange(
                logCurves
                .Where(x => _streamIndexValuePairs || !GetMnemonic(x).EqualsIgnoreCase(indexMetadata.Mnemonic))
                .Select(x =>
                {
                    var channel = ToChannelMetadataRecord(entity, x, indexMetadata);
                    channel.ChannelId = index++;
                    return channel;
                }));

            return metadata;
        }

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <returns>A collection of channel data.</returns>
        public IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range)
        {
            var entity = GetEntity(uri);
            var mnemonics = GetLogHeaderMnemonics(entity);
            var increasing = IsIncreasing(entity);

            return GetChannelData(uri, mnemonics.First(), range, increasing);
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser that specifies the object.</param>
        public override void Delete(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<T>();

            if (parser.HasElements())
            {
                using (var transaction = DatabaseProvider.BeginTransaction(uri))
                {
                    var current = Get(uri);
                    var channels = GetLogCurves(current);
                    var currentRanges = GetCurrentIndexRange(current);

                    PartialDeleteEntity(parser, uri, transaction);

                    PartialDeleteLogData(uri, parser, channels, currentRanges, transaction);

                    transaction.Commit();
                }
            }
            else
            {
                Delete(uri);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            using (var transaction = DatabaseProvider.BeginTransaction(uri))
            {
                Logger.DebugFormat("Deleting Log with uri '{0}'.", uri);

                DeleteEntity(uri, transaction);
                ChannelDataChunkAdapter.Delete(uri);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();

            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : parser.IncludeLogData()
                ? new List<string> { IdPropertyName, "UidWell", "UidWellbore" }
                : OptionsIn.ReturnElements.Requested.Equals(returnElements)
                ? new List<string>()
                : null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForQuery(WitsmlQueryParser parser)
        {
            return new List<string> { "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "logData" };
        }

        /// <summary>
        /// Gets a list of the element names to ignore during an update.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForUpdate(WitsmlQueryParser parser)
        {
            return GetIgnoredElementNamesForQuery(parser)
                .Concat(new[]
                {
                    "direction", "objectGrowing", "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex",
                    "minIndex", "maxIndex", "minDateTimeIndex", "maxDateTimeIndex"
                })
                .ToList();
        }

        private IDictionary<int, string> GetMnemonicList(T log, WitsmlQueryParser parser)
        {
            Logger.Debug("Getting mnemonic list for log.");

            var allMnemonics = GetLogHeaderMnemonics(log);
            if (allMnemonics == null)
            {
                return new Dictionary<int, string>(0);
            }

            var queryMnemonics = parser.GetLogDataMnemonics()?.ToArray() ?? new string[0];
            if (!queryMnemonics.Any())
            {
                queryMnemonics = parser.GetLogCurveInfoMnemonics()
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }

            return ComputeMnemonicIndexes(allMnemonics, queryMnemonics, parser.ReturnElements());
        }

        private string[] GetLogHeaderMnemonics(T log)
        {
            Logger.Debug("Getting log header mnemonics.");

            var logCurves = GetLogCurves(log);
            return logCurves?.Select(GetMnemonic).ToArray();
        }

        private IDictionary<int, string> ComputeMnemonicIndexes(string[] allMnemonics, string[] queryMnemonics, string returnElements)
        {
            Logger.DebugFormat("Computing mnemonic indexes. Return Elements: {0}", returnElements);

            // Start with all mnemonics
            var mnemonicIndexes = allMnemonics
                .Select((mn, index) => new { Mnemonic = mn, Index = index });

            // Check if mnemonics need to be filtered
            if (queryMnemonics.Any() && !OptionsIn.ReturnElements.All.Equals(returnElements))
            {
                // always return the index channel
                mnemonicIndexes = mnemonicIndexes
                    .Where(x => x.Index == 0 || queryMnemonics.Contains(x.Mnemonic));
            }

            // create an index-to-mnemonic map
            return mnemonicIndexes
                .ToDictionary(x => x.Index, x => x.Mnemonic);
        }

        /// <summary>
        /// Gets the channel data for a given index range.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="indexChannel">The index channel.</param>
        /// <param name="range">The range to query the channel data.</param>
        /// <param name="increasing">if set to <c>true</c> if the log is increasing, false otherwise.</param>
        /// <param name="requestLatestValues">The number of latest values requested, null if not requested.</param>
        /// <returns>The channel data records requested</returns>
        private IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, string indexChannel, Range<double?> range, bool increasing, int? requestLatestValues = null)
        {
            Logger.DebugFormat("Getting channel data for log: {0}", uri);

            increasing = requestLatestValues.HasValue ? !increasing : increasing;

            var chunks = ChannelDataChunkAdapter.GetData(uri, indexChannel, range, increasing);
            return chunks.GetRecords(range, increasing, reverse: requestLatestValues.HasValue);
        }

        private IDictionary<int, string> GetUnitList(T log, int[] slices)
        {
            Logger.Debug("Getting unit list for log.");

            // Get a list of all of the units
            var allUnits = GetUnitsByColumnIndex(log);

            // Start with all units
            var unitIndexes = allUnits
                .Select((unit, index) => new { Unit = unit.Value, Index = index });

            // Get indexes for each slice
            if (slices.Any())
            {
                // always return the index channel
                unitIndexes = unitIndexes
                    .Where(x => x.Index == 0 || slices.Contains(x.Index));
            }

            return unitIndexes.ToDictionary(x => x.Index, x => x.Unit);
        }

        private IDictionary<int, string> GetNullValueList(T log, int[] slices)
        {
            Logger.Debug("Getting null value list for log.");

            // Get a list of all of the null values
            var allNullValues = GetNullValuesByColumnIndex(log);

            // Start with all units
            var nullValuesIndexes = allNullValues
                .Select((nullValue, index) => new { NullValue = nullValue.Value, Index = index });

            // Get indexes for each slice
            if (slices.Any())
            {
                // always return the index channel
                nullValuesIndexes = nullValuesIndexes
                    .Where(x => x.Index == 0 || slices.Contains(x.Index));
            }

            return nullValuesIndexes.ToDictionary(x => x.Index, x => x.NullValue);
        }

        private void QueryLogDataValues(T log, T logHeader, WitsmlQueryParser parser, IDictionary<int, string> mnemonics, ResponseContext context)
        {
            Logger.DebugFormat("Query data values for log. Log Uid = {0}", log.Uid);

            if (mnemonics.Count <= 0)
            {
                Logger.Warn("No mnemonics requested for log data query.");
                return;
            }

            if (context.MaxDataNodes <= 0 || context.MaxDataPoints <= 0)
            {
                // Log why we are skipping.
                Logger.Debug("Query Response maximum data nodes or data points has been reached.");
                return;
            }

            // Get the latest values request if one was supplied.
            var requestLatestValues = parser.RequestLatestValues();

            // If latest values have been requested then
            //... don't allow more than the maximum.
            if (requestLatestValues.HasValue)
            {
                requestLatestValues = Math.Min(WitsmlSettings.MaxDataNodes, requestLatestValues.Value);
                Logger.DebugFormat("Request latest value = {0}", requestLatestValues);
            }

            // TODO: If requesting latest values figure out a range that will contain the last values that we want.
            // if there is a request for latest values then the range should be ignored.
            var range = requestLatestValues.HasValue
                ? Range.Empty
                : GetLogDataSubsetRange(logHeader, parser);

            var keys = mnemonics.Keys.ToArray();
            var units = GetUnitList(logHeader, keys);
            var nullValues = GetNullValueList(logHeader, keys);
            var records = GetChannelData(logHeader.GetUri(), mnemonics[0], range, IsIncreasing(logHeader), requestLatestValues);

            // Get a reader for the log's channel data
            var reader = records.GetReader(mnemonics.Values.ToArray(), units, nullValues);

            // Slice the reader for the requested mnemonics
            //reader.Slice(mnemonics, units, nullValues);
            var count = FormatLogData(log, logHeader, reader, context, mnemonics, units, nullValues);           

            // Update the response context growing object totals
            context.UpdateGrowingObjectTotals(count, keys.Length);
        }

        private Range<double?> GetLogDataSubsetRange(T log, WitsmlQueryParser parser)
        {
            var isTimeLog = IsTimeLog(log);

            return Range.Parse(
                parser.PropertyValue(isTimeLog ? "startDateTimeIndex" : "startIndex"),
                parser.PropertyValue(isTimeLog ? "endDateTimeIndex" : "endIndex"),
                isTimeLog);
        }

        private int FormatLogData(
            T log, T logHeader, ChannelDataReader reader, ResponseContext context,
            IDictionary<int, string> mnemonicSlices, IDictionary<int, string> units, IDictionary<int, string> nullValues)
        {
            Logger.Debug("Formatting logData values.");
            Dictionary<string, Range<double?>> ranges;

            var logData = reader.GetData(context, mnemonicSlices, units, nullValues, out ranges);
            if (logData.Count > 0)
            {
                var data = logData
                    .Select(row => string.Join(GetLogDataDelimiter(logHeader), row.SelectMany(x => x)))
                    .ToList();
                SetLogDataValues(log, data, mnemonicSlices.Values, units.Values);
                SetLogIndexRange(log, logHeader, ranges, reader.Indices.FirstOrDefault()?.Mnemonic);
            }

            return logData.Count;
        }

        private void FormatLogHeader(T log, string[] mnemonics)
        {
            var logCurves = GetLogCurves(log);
            RemoveLogCurveInfos(logCurves, mnemonics);
            FormatLogCurveInfos(logCurves);
        }

        private void RemoveLogCurveInfos(List<TChild> logCurves, string[] mnemonics)
        {
            Logger.Debug("Removing logCurveInfos from response.");
            logCurves?.RemoveAll(x => !mnemonics.Contains(GetMnemonic(x)));
        }

        /// <summary>
        /// Formats the log curve infos.
        /// </summary>
        /// <param name="logCurves">The log curves.</param>
        protected virtual void FormatLogCurveInfos(List<TChild> logCurves)
        {
        }

        /// <summary>
        /// Updates the log data and index range.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="readers">The readers.</param>
        /// <param name="transaction">The transaction.</param>
        protected void UpdateLogDataAndIndexRange(EtpUri uri, IEnumerable<ChannelDataReader> readers, MongoTransaction transaction = null)
        {
            Logger.DebugFormat("Updating log data and index for log uri '{0}'.", uri.Uri);

            // Get Updated Log
            var current = GetEntity(uri);

            // Get current index information
            var ranges = GetCurrentIndexRange(current);

            TimeSpan? offset = null;
            var isTimeLog = IsTimeLog(current, true);
            var updateMnemonics = new List<string>();
            var indexUnit = string.Empty;
            var updateIndexRanges = false;
            var checkOffset = true;

            Logger.Debug("Merging ChannelDataChunks with ChannelDataReaders.");

            // Merge ChannelDataChunks
            foreach (var reader in readers)
            {
                if (reader == null)
                    continue;

                var indexCurve = reader.Indices[0];

                if (string.IsNullOrEmpty(indexUnit))
                {
                    indexUnit = indexCurve.Unit;
                }

                updateMnemonics.Clear();
                updateMnemonics.Add(indexCurve.Mnemonic);

                updateMnemonics.AddRange(reader.Mnemonics
                    .Where(m => !updateMnemonics.Contains(m)));

                if (isTimeLog && checkOffset)
                {
                    offset = reader.GetChannelIndexRange(0).Offset;
                    checkOffset = false;
                }

                // Update index range for each logData element
                GetUpdatedIndexRange(reader, updateMnemonics.ToArray(), ranges, IsIncreasing(current));

                // Update log data
                ChannelDataChunkAdapter.Merge(reader, transaction);
                updateIndexRanges = true;
            }

            // Update index range
            if (updateIndexRanges)
            {
                UpdateIndexRange(uri, current, ranges, updateMnemonics, IsTimeLog(current), indexUnit, offset);
            }
        }

        /// <summary>
        /// Gets the current index range.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        protected Dictionary<string, Range<double?>> GetCurrentIndexRange(T entity)
        {
            Logger.Debug("Getting index ranges for all logCurveInfos.");

            var ranges = new Dictionary<string, Range<double?>>();
            var logCurves = GetLogCurves(entity);
            var isTimeLog = IsTimeLog(entity);
            var increasing = IsIncreasing(entity);

            foreach (var curve in logCurves)
            {
                var mnemonic = GetMnemonic(curve);
                var range = GetIndexRange(curve, increasing, isTimeLog);

                Logger.DebugFormat("Index range for curve '{0}' - start: {1}, end: {2}.", mnemonic, range.Start, range.End);
                ranges.Add(mnemonic, range);
            }

            return ranges;
        }

        /// <summary>
        /// Merge the partial delete ranges from QueryIn with the current index range and the default index range.
        /// </summary>
        /// <param name="deletedChannels">The deleted channels.</param>
        /// <param name="defaultRange">The default delete range, i.e from startIndex/startDateTimeIndex, endIndex/endDateTimeIndex of the log header.</param>
        /// <param name="current">The current index ranges.</param>
        /// <param name="delete">The index range from the XmlIn.</param>
        /// <param name="indexCurve">The index curve.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>The channel index ranges for the partial delete.</returns>
        protected Dictionary<string, Range<double?>> MergePartialDeleteRanges(List<string> deletedChannels, Range<double?> defaultRange, Dictionary<string, Range<double?>> current, Dictionary<string, Range<double?>> delete, string indexCurve, bool increasing)
        {
            var ranges = new Dictionary<string, Range<double?>>();
            var indexRange = current[indexCurve];

            double? begin = null;
            double? finish = null;
            double? start = null;
            double? end = null;

            if (!delete.Keys.Any())
            {
                if (defaultRange.Start.HasValue || defaultRange.End.HasValue)
                {
                    foreach (var channel in current.Keys)
                    {
                        ranges.Add(channel,
                            new Range<double?>(defaultRange.Start, defaultRange.End, defaultRange.Offset));
                    }
                }

                return ranges;
            }

            foreach (var range in delete)
            {
                if (deletedChannels.Contains(range.Key))
                    continue;

                if (!range.Value.Start.HasValue && !range.Value.End.HasValue)
                {
                    if (defaultRange.Start.HasValue)
                    {
                        start = defaultRange.Start;
                        end = defaultRange.End;                      
                    }
                    else
                    {
                        start = indexRange.Start;
                        end = indexRange.End;
                    }
                    ranges.Add(range.Key, new Range<double?>(start, end, range.Value.Offset));
                }
                else
                {
                    start = range.Value.Start ?? indexRange.Start.GetValueOrDefault();
                    end = range.Value.End ?? indexRange.End.GetValueOrDefault();
                    ranges.Add(range.Key, new Range<double?>(start, end, range.Value.Offset));
                }

                if (!start.HasValue || !end.HasValue)
                    continue;

                if (begin.HasValue)
                {
                    if (StartsBefore(start.Value, begin.Value, increasing))
                        begin = start;
                    if (StartsBefore(end.Value, finish.Value, increasing))
                        finish = end;
                }
                else
                {
                    begin = start;
                    finish = end;
                }
            }

            if (!ranges.ContainsKey(indexCurve))
                ranges.Add(indexCurve, new Range<double?>(begin, finish, indexRange.Offset));

            return ranges;
        }

        /// <summary>
        /// Check if to delete channel data by mnemonic.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [is time log].</param>
        /// <returns>True if to delete channel data by mnemonic; false otherwise.</returns>
        protected bool ToDeleteChannelDataByMnemonic(WitsmlQueryParser parser, bool isTimeLog)
        {
            var fields = new List<string> { "mnemonic" };
            if (isTimeLog)
            {
                fields.Add("minDateTimeIndex");
                fields.Add("maxDateTimeIndex");
            }
            else
            {
                fields.Add("minIndex");
                fields.Add("maxDIndex");
            }
            var elements = parser.Properties(parser.Element(), "logCurveInfo");
            foreach (var element in elements)
            {
                if (!element.HasElements || element.Elements().All(e => e.Name.LocalName == "mnemonic"))
                    return true;

                var curveElements = element.Elements();
                var uidAttribute = element.Attribute("uid");
                if (uidAttribute != null)
                    continue;

                if (curveElements.All(e => fields.Contains(e.Name.LocalName)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if value a starts the before value b.
        /// </summary>
        /// <param name="a">The value a.</param>
        /// <param name="b">The value b.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <returns>True is a starts before b.</returns>
        protected bool StartsBefore(double a, double b, bool increasing)
        {
            return increasing
                ? a <= b
                : a >= b;
        }

        private void GetUpdatedIndexRange(ChannelDataReader reader, string[] mnemonics, Dictionary<string, Range<double?>> ranges, bool increasing = true)
        {
            Logger.Debug("Getting updated index ranges for all logCurveInfos.");

            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                Range<double?> current;

                if (!ranges.TryGetValue(mnemonic, out current))
                    current = new Range<double?>(null, null);

                Logger.DebugFormat("Current '{0}' index range - start: {1}, end: {2}.", mnemonic, current.Start, current.End);

                var update = reader.GetChannelIndexRange(i);
                var start = current.Start;
                var end = current.End;

                if (!start.HasValue || !update.StartsAfter(start.Value, increasing))
                    start = update.Start;

                if (!end.HasValue || !update.EndsBefore(end.Value, increasing))
                    end = update.End;

                Logger.DebugFormat("Updated '{0}' index range - start: {1}, end: {2}.", mnemonic, start, end);
                ranges[mnemonic] = new Range<double?>(start, end);
            }
        }

        /// <summary>
        /// Updates the index range.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="ranges">The ranges.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [is time log].</param>
        /// <param name="indexUnit">The index unit.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="isDelete">True if for delete log data.</param>
        protected void UpdateIndexRange(EtpUri uri, T entity, Dictionary<string, Range<double?>> ranges, IEnumerable<string> mnemonics, bool isTimeLog, string indexUnit, TimeSpan? offset, bool isDelete = false)
        {
            Logger.DebugFormat("Updating index range with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);

            var mongoUpdate = new MongoDbUpdate<T>(Container, GetCollection(), null);
            var filter = MongoDbUtility.GetEntityFilter<T>(uri);
            var increasing = IsIncreasing(entity);
            UpdateDefinition<T> logHeaderUpdate = null;

            foreach (var mnemonic in mnemonics)
            {
                var curve = GetLogCurve(entity, mnemonic);
                if (curve == null) continue;

                var curveFilter = Builders<T>.Filter.And(filter,
                    MongoDbUtility.BuildFilter<T>("LogCurveInfo.Uid", curve.Uid));

                var range = ranges[mnemonic];
                var isIndexCurve = mnemonic == GetIndexCurveMnemonic(entity);

                var currentRange = GetIndexRange(curve, increasing, isTimeLog);
                var updateRange = isDelete? range: GetUpdateRange(currentRange, range, increasing);

                logHeaderUpdate = isTimeLog
                    ? UpdateDateTimeIndexRange(mongoUpdate, curveFilter, logHeaderUpdate, updateRange, increasing, isIndexCurve, offset)
                    : UpdateIndexRange(mongoUpdate, curveFilter, logHeaderUpdate, updateRange, increasing, isIndexCurve, indexUnit);
            }

            logHeaderUpdate = UpdateCommonData(logHeaderUpdate, entity, offset);

            if (logHeaderUpdate != null)
            {
                mongoUpdate.UpdateFields(filter, logHeaderUpdate);
            }
        }

        private UpdateDefinition<T> UpdateIndexRange(MongoDbUpdate<T> mongoUpdate, FilterDefinition<T> curveFilter, UpdateDefinition<T> logHeaderUpdate, Range<double?> range, bool increasing, bool isIndexCurve, string indexUnit)
        {
            object minIndex = null;
            object maxIndex = null;

            // Sort range in min/max order
            range = range.Sort();

            if (range.Start.HasValue)
                minIndex = CreateGenericMeasure(range.Start.Value, indexUnit);
            var updates = MongoDbUtility.BuildUpdate<T>(null, "LogCurveInfo.$.MinIndex", minIndex);
            Logger.DebugFormat("Building MongoDb Update for MinIndex '{0}'", minIndex);

            if (range.End.HasValue)
                maxIndex = CreateGenericMeasure(range.End.Value, indexUnit);
            updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxIndex", maxIndex);
            Logger.DebugFormat("Building MongoDb Update for MaxIndex '{0}'", maxIndex);

            if (isIndexCurve)
            {
                var startIndex = increasing ? minIndex : maxIndex;
                var endIndex = increasing ? maxIndex : minIndex;

                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "StartIndex", startIndex);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "EndIndex", endIndex);
            }

            if (updates != null)
            {
                mongoUpdate.UpdateFields(curveFilter, updates);
            }

            return logHeaderUpdate;
        }

        private UpdateDefinition<T> UpdateDateTimeIndexRange(MongoDbUpdate<T> mongoUpdate, FilterDefinition<T> curveFilter, UpdateDefinition<T> logHeaderUpdate, Range<double?> range, bool increasing, bool isIndexCurve, TimeSpan? offset)
        {
            UpdateDefinition<T> updates = null;
            var minDate = string.Empty;
            var maxDate = string.Empty;

            // Sort range in min/max order
            range = range.Sort();

            if (range.Start.HasValue)
            {
                minDate = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.Start.Value).ToOffsetTime(offset).ToString("o");
                updates = MongoDbUtility.BuildUpdate<T>(null, "LogCurveInfo.$.MinDateTimeIndex", minDate);
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MinDateTimeIndexSpecified", true);
                Logger.DebugFormat("Building MongoDb Update for MinDateTimeIndex '{0}'", minDate);
            }

            if (range.End.HasValue)
            {
                maxDate = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.End.Value).ToOffsetTime(offset).ToString("o");
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxDateTimeIndex", maxDate);
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxDateTimeIndexSpecified", true);
                Logger.DebugFormat("Building MongoDb Update for MaxDateTimeIndex '{0}'", maxDate);
            }

            if (isIndexCurve)
            {
                var startDate = increasing ? minDate : maxDate;
                var endDate = increasing ? maxDate : minDate;

                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "StartDateTimeIndex", startDate);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "StartDateTimeIndexSpecified", true);

                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "EndDateTimeIndex", endDate);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "EndDateTimeIndexSpecified", true);
            }

            if (updates != null)
            {
                mongoUpdate.UpdateFields(curveFilter, updates);
            }

            return logHeaderUpdate;
        }

        private Range<double?> GetUpdateRange(Range<double?> current, Range<double?> update, bool increasing)
        {
            if (!update.Start.HasValue || !update.End.HasValue)
                return current;

            var start = update.Start.GetValueOrDefault();
            var end = update.End.GetValueOrDefault();

            if (current.Start.HasValue && !current.StartsAfter(start, increasing))
                start = current.Start.GetValueOrDefault();

            if (current.End.HasValue && !current.EndsBefore(end, increasing))
                end = update.End.GetValueOrDefault();

            return new Range<double?>(start, end, update.Offset);
        }

        /// <summary>
        /// Determines whether the specified log is increasing.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected abstract bool IsIncreasing(T log);

        /// <summary>
        /// Determines whether the specified log is a time log.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="includeElapsedTime">if set to <c>true</c> [include elapsed time].</param>
        /// <returns></returns>
        protected abstract bool IsTimeLog(T log, bool includeElapsedTime = false);

        /// <summary>
        /// Gets the log curve.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns></returns>
        protected abstract TChild GetLogCurve(T log, string mnemonic);

        /// <summary>
        /// Gets the log curves.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected abstract List<TChild> GetLogCurves(T log);

        /// <summary>
        /// Gets the mnemonic.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <returns></returns>
        protected abstract string GetMnemonic(TChild curve);

        /// <summary>
        /// Gets the index curve mnemonic.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected abstract string GetIndexCurveMnemonic(T log);

        /// <summary>
        /// Gets the units by column index.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected abstract IDictionary<int, string> GetUnitsByColumnIndex(T log);

        /// <summary>
        /// Gets the null values by column index.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected abstract IDictionary<int, string> GetNullValuesByColumnIndex(T log);

        /// <summary>
        /// Gets the index range.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="isTimeIndex">if set to <c>true</c> [is time index].</param>
        /// <returns></returns>
        protected abstract Range<double?> GetIndexRange(TChild curve, bool increasing = true, bool isTimeIndex = false);

        /// <summary>
        /// Sets the log index range.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="logHeader">The log that has the header information.</param>
        /// <param name="ranges">The ranges.</param>
        /// <param name="indexCurve">The index curve.</param>
        protected abstract void SetLogIndexRange(T log, T logHeader, Dictionary<string, Range<double?>> ranges, string indexCurve);

        /// <summary>
        /// Sets the log data values.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="logData">The log data.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="units">The units.</param>
        protected abstract void SetLogDataValues(T log, List<string> logData, IEnumerable<string> mnemonics, IEnumerable<string> units);

        /// <summary>
        /// Updates the common data.
        /// </summary>
        /// <param name="logHeaderUpdate">The log header update.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        protected abstract UpdateDefinition<T> UpdateCommonData(UpdateDefinition<T> logHeaderUpdate, T entity, TimeSpan? offset);

        /// <summary>
        /// Creates a generic measure.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="uom">The uom.</param>
        /// <returns></returns>
        protected abstract object CreateGenericMeasure(double value, string uom);

        /// <summary>
        /// Converts a logCurveInfo to an index metadata record.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="indexCurve">The index curve.</param>
        /// <param name="scale">The scale.</param>
        /// <returns></returns>
        protected abstract IndexMetadataRecord ToIndexMetadataRecord(T entity, TChild indexCurve, int scale = 3);

        /// <summary>
        /// Converts a logCurveInfo to a channel metadata record.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="indexMetadata">The index metadata.</param>
        /// <returns></returns>
        protected abstract ChannelMetadataRecord ToChannelMetadataRecord(T entity, TChild curve, IndexMetadataRecord indexMetadata);

        /// <summary>
        /// Partially delete the log data.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="channels">The current logCurve information.</param>
        /// <param name="currentRanges">The current channel index ranges.</param>
        /// <param name="transaction">The transaction.</param>
        protected abstract void PartialDeleteLogData(EtpUri uri, WitsmlQueryParser parser, List<TChild> channels, Dictionary<string, Range<double?>> currentRanges, MongoTransaction transaction);

        /// <summary>
        /// Gets the log data delimiter.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        protected virtual string GetLogDataDelimiter(T entity)
        {
            return ChannelDataReader.DefaultDataDelimiter;
        }
    }
}
