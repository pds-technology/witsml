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
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    public abstract class LogDataAdapter<T, TChild> : MongoDbDataAdapter<T>, IChannelDataProvider where T : IWellboreObject where TChild : IUniqueId
    {
        private readonly bool _streamIndexValuePairs;

        protected LogDataAdapter(IDatabaseProvider databaseProvider, string dbCollectionName) : base(databaseProvider, dbCollectionName)
        {
            _streamIndexValuePairs = Settings.Default.StreamIndexValuePairs;
        }

        [Import]
        public ChannelDataChunkAdapter ChannelDataChunkAdapter { get; set; }

        /// <summary>
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>Queried objects.</returns>
        public override WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();
            var logs = QueryEntities(parser);

            if (IncludeLogData(parser, returnElements))
            {
                var logHeaders = GetEntities(logs.Select(x => x.GetUri()))
                    .ToDictionary(x => x.GetUri());

                logs.ForEach(l =>
                {
                    var logHeader = logHeaders[l.GetUri()];
                    var mnemonics = GetMnemonicList(logHeader, parser);

                    QueryLogDataValues(l, logHeader, parser, mnemonics);
                    FormatLogHeader(l, mnemonics, returnElements);
                });
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                logs.ForEach(l =>
                {
                    var mnemonics = GetMnemonicList(l, parser);
                    FormatLogHeader(l, mnemonics, returnElements);
                });
            }

            SortIndex(logs);

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                CreateCollection(logs));
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            UpdateLogDataAndIndexRange(uri, new List<ChannelDataReader> { reader });
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
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<T> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var ids = parentUri.Value.GetObjectIds().ToDictionary(x => x.Key, y => y.Value);
                var uidWellbore = ids[ObjectTypes.Wellbore];
                var uidWell = ids[ObjectTypes.Well];

                query = query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore);
            }

            return query
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(EtpUri uri)
        {
            return GetEntity(uri);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Delete(EtpUri uri)
        {
            Logger.DebugFormat("Delete for Log with uri '{0}'.", uri.Uri);

            var result = base.Delete(uri);

            if (result.Code == ErrorCodes.Success)
                result = ChannelDataChunkAdapter.Delete(uri);

            return result;
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="returnElements">The return elements.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(string returnElements)
        {
            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : OptionsIn.ReturnElements.DataOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, "UidWell", "UidWellbore" }
                : OptionsIn.ReturnElements.Requested.Equals(returnElements)
                ? new List<string>()
                : null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNames()
        {
            return new List<string> { "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "logData" };
        }

        protected bool IncludeLogData(WitsmlQueryParser parser, string returnElements)
        {
            return OptionsIn.ReturnElements.All.Equals(returnElements) ||
                   OptionsIn.ReturnElements.DataOnly.Equals(returnElements) ||
                   parser.Contains("logData");
        }

        protected IDictionary<int, string> GetMnemonicList(T log, WitsmlQueryParser parser)
        {
            var allMnemonics = GetLogHeaderMnemonics(log);
            if (allMnemonics == null)
            {
                return new Dictionary<int, string>(0);
            }

            var queryMnemonics = GetLogDataMnemonics(parser).ToArray();
            if (!queryMnemonics.Any())
            {
                queryMnemonics = GetLogCurveInfoMnemonics(parser).ToArray();
            }

            return ComputeMnemonicIndexes(allMnemonics, queryMnemonics, parser.ReturnElements());
        }

        protected string[] GetLogHeaderMnemonics(T log)
        {
            var logCurves = GetLogCurves(log);
            return logCurves?.Select(GetMnemonic).ToArray();
        }

        protected IEnumerable<string> GetLogCurveInfoMnemonics(WitsmlQueryParser parser)
        {
            var mnemonics = Enumerable.Empty<string>();
            var logCurveInfos = parser.Properties("logCurveInfo").ToArray();

            if (logCurveInfos.Any())
            {
                var mnemonicList = parser.Properties(logCurveInfos, "mnemonic").ToArray();

                if (mnemonicList.Any())
                {
                    mnemonics = mnemonicList.Select(x => x.Value);
                }
            }

            return mnemonics;
        }

        protected IEnumerable<string> GetLogDataMnemonics(WitsmlQueryParser parser)
        {
            var mnemonics = Enumerable.Empty<string>();
            var logData = parser.Property("logData");

            if (logData != null)
            {
                var mnemonicList = parser.Properties(logData, "mnemonicList")
                    .Take(1)
                    .ToArray();

                if (mnemonicList.Any())
                {
                    mnemonics = mnemonicList.First().Value.Split(',');
                }
            }

            return mnemonics;
        }

        protected IDictionary<int, string> ComputeMnemonicIndexes(string[] allMnemonics, string[] queryMnemonics, string returnElements)
        {
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

        protected IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, string indexChannel, Range<double?> range, bool increasing)
        {
            var chunks = ChannelDataChunkAdapter.GetData(uri, indexChannel, range, increasing);
            return chunks.GetRecords(range, increasing);
        }

        protected IDictionary<int, string> GetUnitList(T log, int[] slices)
        {
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

        protected void QueryLogDataValues(T log, T logHeader, WitsmlQueryParser parser, IDictionary<int, string> mnemonics)
        {
            var range = GetLogDataSubsetRange(logHeader, parser);
            var units = GetUnitList(logHeader, mnemonics.Keys.ToArray());
            var records = GetChannelData(logHeader.GetUri(), mnemonics[0], range, IsIncreasing(logHeader));
            var logData = FormatLogData(log, records.GetReader(), mnemonics, units);

            SetLogDataValues(log, logData, mnemonics.Values, units.Values);
        }

        protected Range<double?> GetLogDataSubsetRange(T log, WitsmlQueryParser parser)
        {
            var isTimeLog = IsTimeLog(log);

            return Range.Parse(
                parser.PropertyValue(isTimeLog ? "startDateTimeIndex" : "startIndex"),
                parser.PropertyValue(isTimeLog ? "endDateTimeIndex" : "endIndex"),
                isTimeLog);
        }

        protected List<string> FormatLogData(T log, ChannelDataReader reader, IDictionary<int, string> mnemonics, IDictionary<int, string> units)
        {
            var logData = new List<string>();
            var slices = mnemonics.Keys.ToArray();
            var isTimeIndex = reader.Indices.Select(x => x.IsTimeIndex).FirstOrDefault();

            var ranges = new Dictionary<string, Range<double?>>();

            for (int i = 1; i < reader.FieldCount; i++)
            {
                var range = reader.GetChannelIndexRange(i);

                // Filter mnemonics with no channel values
                if (!range.Start.HasValue)
                {
                    mnemonics.Remove(i);
                    units.Remove(i);
                }
                else
                    ranges.Add(mnemonics[i], range);
            }

            var indexRange = new List<double>();

            // Read through each row
            while (reader.Read())
            {
                var values = new List<object>();
                var index = reader.GetDouble(0);

                // Use timestamp format for time index values
                values.Add(isTimeIndex
                    ? reader.GetDateTimeOffset(0).ToString("o")
                    : (object)index);

                for (int i = 1; i < reader.FieldCount; i++)
                {
                    var channelValue = reader.GetValue(i);
                    // Limit data to requested mnemonics
                    if ((!slices.Any() || slices.Contains(i)) && channelValue != null)
                        values.Add(channelValue);
                }

                // Filter rows with no channel values
                if (values.Count > 1)
                {
                    logData.Add(string.Join(",", values));
                    if (indexRange.Count == 0)
                    {
                        indexRange.Add(index);
                        indexRange.Add(index);
                    }
                    else
                    {
                        indexRange[1] = index;
                    }
                }
            }

            if (logData.Count > 0)
            {
                ranges.Add(reader.GetIndex(0).Mnemonic, new Range<double?>(indexRange.First(), indexRange.Last()));
                SetLogIndexRange(log, ranges);
            }

            return logData;
        }

        protected void FormatLogHeader(T log, IDictionary<int, string> mnemonics, string returnElements)
        {
            RemoveLogCurveInfos(log, mnemonics.Values.ToArray());
        }

        protected void RemoveLogCurveInfos(T log, string[] mnemonics)
        {
            var logCurves = GetLogCurves(log);
            logCurves?.RemoveAll(x => !mnemonics.Contains(GetMnemonic(x)));
        }

        protected void InsertLogData(T entity, ChannelDataReader reader)
        {
            if (entity == null || reader == null) return;

            var indexCurve = reader.Indices[0];
            Logger.DebugFormat("Index curve mnemonic from reader: {0}.", indexCurve == null ? "'null'" : indexCurve.Mnemonic);
            if (indexCurve == null) return;

            var allMnemonics = new[] { indexCurve.Mnemonic }.Concat(reader.Mnemonics).ToArray();
            Logger.DebugFormat("All Mnemonics from reader: {0}", string.Join(", ", allMnemonics));

            var ranges = GetCurrentIndexRange(entity);

            var isTimeLog = IsTimeLog(entity, true);
            TimeSpan? offset = null;

            if (isTimeLog)
                offset = reader.GetIndexRange().Offset;

            GetUpdatedIndexRange(reader, allMnemonics, ranges, IsIncreasing(entity));

            // Add ChannelDataChunks
            ChannelDataChunkAdapter.Add(reader);

            // Update index range
            UpdateIndexRange(entity.GetUri(), entity, ranges, allMnemonics, isTimeLog, indexCurve.Unit, offset);
        }

        protected void UpdateLogDataAndIndexRange(EtpUri uri, IEnumerable<ChannelDataReader> readers, TimeSpan? offset = null)
        {
            Logger.DebugFormat("Updating log data and index for log uri '{0}'.", uri.Uri);

            // Get Updated Log
            var current = GetEntity(uri);

            // Get current index information
            var ranges = GetCurrentIndexRange(current);

            var indexUnit = string.Empty;
            var updateMnemonics = new List<string>();

            var updateIndex = false;
            var checkOffset = true;

            Logger.Debug("Merging ChannelDataChunks with ChannelDataReaders.");

            // Merge ChannelDataChunks
            foreach (var reader in readers)
            {
                var indexCurve = reader.Indices[0];

                if (string.IsNullOrEmpty(indexUnit))
                {
                    indexUnit = indexCurve.Unit;
                    updateMnemonics.Add(indexCurve.Mnemonic);
                }

                updateMnemonics.AddRange(reader.Mnemonics
                    .Where(m => !updateMnemonics.Contains(m)));

                var isTimeLog = IsTimeLog(current, true);
                if (isTimeLog && checkOffset && !offset.HasValue)
                {
                    offset = reader.GetChannelIndexRange(0).Offset;
                    checkOffset = false;
                }

                // Update index range for each logData element
                GetUpdatedIndexRange(reader, updateMnemonics.ToArray(), ranges, IsIncreasing(current));

                // Update log data
                ChannelDataChunkAdapter.Merge(reader);
                updateIndex = true;
            }

            // Update index range
            if (updateIndex)
            {
                UpdateIndexRange(uri, current, ranges, updateMnemonics, IsTimeLog(current), indexUnit, offset);
            }
        }

        protected Dictionary<string, Range<double?>> GetCurrentIndexRange(T entity)
        {
            var ranges = new Dictionary<string, Range<double?>>();
            var logCurves = GetLogCurves(entity);
            var isTimeLog = IsTimeLog(entity);

            foreach (var curve in logCurves)
            {
                var mnemonic = GetMnemonic(curve);

                var range = isTimeLog
                    ? GetDateTimeIndexRange(entity, curve)
                    : GetIndexRange(entity, curve);

                Logger.DebugFormat("Index range for curve '{0}' - start: {1}, end: {2}.", mnemonic, range.Start, range.End);
                ranges.Add(mnemonic, range);
            }

            return ranges;
        }

        protected void GetUpdatedIndexRange(ChannelDataReader reader, string[] mnemonics, Dictionary<string, Range<double?>> ranges, bool increasing = true)
        {
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

        protected void UpdateIndexRange(EtpUri uri, T entity, Dictionary<string, Range<double?>> ranges, IEnumerable<string> mnemonics, bool isTimeLog, string indexUnit, TimeSpan? offset)
        {
            Logger.DebugFormat("Updating index range with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);

            var mongoUpdate = new MongoDbUpdate<T>(GetCollection(), null);
            var filter = MongoDbUtility.GetEntityFilter<T>(uri);
            var logCurves = GetLogCurves(entity);
            UpdateDefinition<T> logHeaderUpdate = null;

            foreach (var mnemonic in mnemonics)
            {
                var curve = logCurves.FirstOrDefault(c => c.Uid.EqualsIgnoreCase(mnemonic));
                if (curve == null) continue;

                var curveFilter = Builders<T>.Filter.And(filter,
                    MongoDbUtility.BuildFilter<T>("LogCurveInfo.Uid", curve.Uid));

                var range = ranges[mnemonic];
                var isIndexCurve = mnemonic == GetIndexCurveMnemonic(entity);

                logHeaderUpdate = isTimeLog 
                    ? UpdateDateTimeIndexRange(mongoUpdate, curveFilter, logHeaderUpdate, range, isIndexCurve, offset) 
                    : UpdateIndexRange(mongoUpdate, curveFilter, logHeaderUpdate, range, isIndexCurve, indexUnit);
            }

            logHeaderUpdate = UpdateCommonData(mongoUpdate, logHeaderUpdate, entity, offset);

            if (logHeaderUpdate != null)
            {
                mongoUpdate.UpdateFields(filter, logHeaderUpdate);
            }
        }

        private UpdateDefinition<T> UpdateIndexRange(MongoDbUpdate<T> mongoUpdate, FilterDefinition<T> curveFilter, UpdateDefinition<T> logHeaderUpdate, Range<double?> range, bool isIndexCurve, string indexUnit)
        {
            UpdateDefinition<T> updates = null;

            if (range.Start.HasValue)
            {
                var minIndex = CreateGenericMeasure(range.Start.Value, indexUnit);
                updates = MongoDbUtility.BuildUpdate<T>(null, "LogCurveInfo.$.MinIndex", minIndex);
                Logger.DebugFormat("Building MongoDb Update for MinIndex '{0}'", minIndex);

                if (isIndexCurve)
                {
                    logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "StartIndex", minIndex);
                }
            }

            if (range.End.HasValue)
            {
                var maxIndex = CreateGenericMeasure(range.End.Value, indexUnit);
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxIndex", maxIndex);
                Logger.DebugFormat("Building MongoDb Update for MaxIndex '{0}'", maxIndex);

                if (isIndexCurve)
                {
                    logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "EndIndex", maxIndex);
                }
            }

            if (updates != null)
            {
                mongoUpdate.UpdateFields(curveFilter, updates);
            }

            return logHeaderUpdate;
        }

        private UpdateDefinition<T> UpdateDateTimeIndexRange(MongoDbUpdate<T> mongoUpdate, FilterDefinition<T> curveFilter, UpdateDefinition<T> logHeaderUpdate, Range<double?> range, bool isIndexCurve, TimeSpan? offset)
        {
            UpdateDefinition<T> updates = null;

            if (range.Start.HasValue)
            {
                var minDate = DateTimeOffset.FromUnixTimeSeconds((long)range.Start.Value).ToOffsetTime(offset).ToString("o");
                updates = MongoDbUtility.BuildUpdate<T>(null, "LogCurveInfo.$.MinDateTimeIndex", minDate);
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MinDateTimeIndexSpecified", true);
                Logger.DebugFormat("Building MongoDb Update for MinDateTimeIndex '{0}'", minDate);

                if (isIndexCurve)
                {
                    logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "StartDateTimeIndex", minDate);
                    logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "StartDateTimeIndexSpecified", true);
                }
            }

            if (range.End.HasValue)
            {
                var maxDate = DateTimeOffset.FromUnixTimeSeconds((long)range.End.Value).ToOffsetTime(offset).ToString("o");
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxDateTimeIndex", maxDate);
                updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxDateTimeIndexSpecified", true);
                Logger.DebugFormat("Building MongoDb Update for MaxDateTimeIndex '{0}'", maxDate);

                if (isIndexCurve)
                {
                    logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "EndDateTimeIndex", maxDate);
                    logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "EndDateTimeIndexSpecified", true);
                }
            }

            if (updates != null)
            {
                mongoUpdate.UpdateFields(curveFilter, updates);
            }

            return logHeaderUpdate;
        }

        protected abstract bool IsIncreasing(T log);

        protected abstract bool IsTimeLog(T log, bool includeElapsedTime = false);

        protected abstract List<TChild> GetLogCurves(T log);

        protected abstract string GetMnemonic(TChild curve);

        protected abstract string GetIndexCurveMnemonic(T log);

        protected abstract IDictionary<int, string> GetUnitsByColumnIndex(T log);

        protected abstract Range<double?> GetIndexRange(T log, TChild curve);

        protected abstract Range<double?> GetDateTimeIndexRange(T log, TChild curve);

        protected abstract void SetLogIndexRange(T log, Dictionary<string, Range<double?>> ranges);

        protected abstract void SortIndex(List<T> logs);

        protected abstract void SetLogDataValues(T log, List<string> logData, IEnumerable<string> mnemonics, IEnumerable<string> units);

        protected abstract UpdateDefinition<T> UpdateCommonData(MongoDbUpdate<T> mongoUpdate, UpdateDefinition<T> logHeaderUpdate, T entity, TimeSpan? offset);

        protected abstract object CreateGenericMeasure(double value, string uom);

        protected abstract IEnergisticsCollection CreateCollection(List<T> entities);

        protected abstract IndexMetadataRecord ToIndexMetadataRecord(T entity, TChild indexCurve, int scale = 3);

        protected abstract ChannelMetadataRecord ToChannelMetadataRecord(T entity, TChild curve, IndexMetadataRecord indexMetadata);
    }
}
