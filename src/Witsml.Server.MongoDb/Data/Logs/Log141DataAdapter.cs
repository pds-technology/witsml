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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Datatypes.Object;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Data.Transactions;
using PDS.Witsml.Server.Providers.Store;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 141 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.Logs.LogDataAdapter{Log, LogCurveInfo}" />
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IWitsml141Configuration))]
    [Export141(ObjectTypes.Log, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataAdapter : LogDataAdapter<Log, LogCurveInfo>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log141DataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log141DataAdapter(IContainer container, IDatabaseProvider databaseProvider) : base(container, databaseProvider, ObjectNames.Log141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Log"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for Log data version {0}.", capServer.Version);

            var dataObject = new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = WitsmlSettings.MaxDataNodes,
                MaxDataPoints = WitsmlSettings.MaxDataPoints
            };

            capServer.Add(Functions.GetFromStore, dataObject);
            capServer.Add(Functions.AddToStore, dataObject);
            capServer.Add(Functions.UpdateInStore, dataObject);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Log);
        }

        /// <summary>
        /// Adds a <see cref="Log" /> entity to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The <see cref="Log" /> to be added.</param>
        public override void Add(WitsmlQueryParser parser, Log dataObject)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                ClearIndexValues(dataObject);

                // Extract Data                    
                var readers = ExtractDataReaders(dataObject);
                InsertEntity(dataObject, transaction);
                UpdateLogDataAndIndexRange(dataObject.GetUri(), readers, transaction);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Updates the specified <see cref="Log" /> instance in the store.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, Log dataObject)
        {
            var uri = dataObject.GetUri();
            using (var transaction = DatabaseProvider.BeginTransaction(uri))
            {
                UpdateEntity(parser, uri, transaction);
                var readers = ExtractDataReaders(dataObject, GetEntity(uri));
                UpdateLogDataAndIndexRange(uri, readers, transaction);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Creates a generic measure.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="uom">The uom.</param>
        /// <returns></returns>
        protected override object CreateGenericMeasure(double value, string uom)
        {
            return new GenericMeasure() { Value = value, Uom = uom };
        }

        /// <summary>
        /// Determines whether the specified log is increasing.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected override bool IsIncreasing(Log log)
        {
            return log.IsIncreasing();
        }

        /// <summary>
        /// Determines whether the specified log is a time log.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="includeElapsedTime">if set to <c>true</c>, include elapsed time.</param>
        /// <returns></returns>
        protected override bool IsTimeLog(Log log, bool includeElapsedTime = false)
        {
            return log.IsTimeLog(includeElapsedTime);
        }

        /// <summary>
        /// Gets the log curve.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns></returns>
        protected override LogCurveInfo GetLogCurve(Log log, string mnemonic)
        {
            return log?.LogCurveInfo.GetByUid(mnemonic) ?? log?.LogCurveInfo.GetByMnemonic(mnemonic);
        }

        /// <summary>
        /// Gets the log curves.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected override List<LogCurveInfo> GetLogCurves(Log log)
        {
            return log.LogCurveInfo;
        }

        /// <summary>
        /// Gets the mnemonic.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <returns></returns>
        protected override string GetMnemonic(LogCurveInfo curve)
        {
            Logger.DebugFormat("Getting logCurveInfo mnemonic: {0}", curve?.Mnemonic?.Value);
            return curve?.Mnemonic?.Value;
        }

        /// <summary>
        /// Gets the index curve mnemonic.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected override string GetIndexCurveMnemonic(Log log)
        {
            Logger.DebugFormat("Getting log index curve mnemonic: {0}", log.IndexCurve);
            return log.IndexCurve;
        }

        /// <summary>
        /// Gets the units by column index.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected override IDictionary<int, string> GetUnitsByColumnIndex(Log log)
        {
            Logger.Debug("Getting logCurveInfo units by column index.");

            return log.LogCurveInfo
                .Select(x => x.Unit)
                .ToArray()
                .Select((unit, index) => new { Unit = unit, Index = index })
                .ToDictionary(x => x.Index, x => x.Unit);
        }

        /// <summary>
        /// Gets the null values by column index.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns></returns>
        protected override IDictionary<int, string> GetNullValuesByColumnIndex(Log log)
        {
            Logger.Debug("Getting logCurveInfo null values by column index.");

            return log.LogCurveInfo
                .Select(x => x.NullValue)
                .ToArray()
                .Select((nullValue, index) => new { NullValue = string.IsNullOrWhiteSpace(nullValue) ? log.NullValue : nullValue, Index = index })
                .ToDictionary(x => x.Index, x => x.NullValue);
        }

        /// <summary>
        /// Gets the index range.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="increasing">if set to <c>true</c> [increasing].</param>
        /// <param name="isTimeIndex">if set to <c>true</c> [is time index].</param>
        /// <returns></returns>
        protected override Range<double?> GetIndexRange(LogCurveInfo curve, bool increasing = true, bool isTimeIndex = false)
        {
            return curve.GetIndexRange(increasing, isTimeIndex);
        }

        /// <summary>
        /// Sets the log data values.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="logDataValues">The log data values.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <param name="units">The units.</param>
        protected override void SetLogDataValues(Log log, List<string> logDataValues, IEnumerable<string> mnemonics, IEnumerable<string> units)
        {
            Logger.Debug("Settings logData values.");

            if (log.LogData == null)
                log.LogData = new List<LogData>();

            log.LogData.Add(new LogData()
            {
                MnemonicList = string.Join(",", mnemonics),
                UnitList = string.Join(",", units),
                Data = logDataValues
            });
        }

        /// <summary>
        /// Sets the log index range.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="logHeader">The log that has the header information.</param>
        /// <param name="ranges">The ranges.</param>
        /// <param name="indexCurve">The index curve.</param>
        protected override void SetLogIndexRange(Log log, Log logHeader, Dictionary<string, Range<double?>> ranges, string indexCurve)
        {
            var isTimeLog = IsTimeLog(logHeader);

            if (log.LogCurveInfo != null)
            {
                Logger.Debug("Setting logCurveInfo min/max index ranges.");

                foreach (var logCurve in log.LogCurveInfo)
                {
                    var mnemonic = logCurve.Mnemonic.Value;
                    Range<double?> range;

                    if (!ranges.TryGetValue(mnemonic, out range))
                        continue;

                    // Sort range in min/max order
                    range = range.Sort();

                    if (isTimeLog)
                    {
                        if (range.Start.HasValue && !double.IsNaN(range.Start.Value) && logCurve.MinDateTimeIndex.HasValue)
                            logCurve.MinDateTimeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.Start.Value);
                        if (range.End.HasValue && !double.IsNaN(range.End.Value) && logCurve.MaxDateTimeIndex.HasValue)
                            logCurve.MaxDateTimeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.End.Value);                       
                    }
                    else
                    {
                        if (range.Start.HasValue && logCurve.MinIndex != null)
                            logCurve.MinIndex.Value = range.Start.Value;
                        if (range.End.HasValue && logCurve.MaxIndex != null)
                            logCurve.MaxIndex.Value = range.End.Value;
                    }
                }
            }

            // Set index curve range separately, since logCurveInfo may not exist
            SetIndexCurveRange(log, ranges, indexCurve, isTimeLog);
        }

        /// <summary>
        /// Updates the common data.
        /// </summary>
        /// <param name="logHeaderUpdate">The log header update.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        protected override UpdateDefinition<Log> UpdateCommonData(UpdateDefinition<Log> logHeaderUpdate, Log entity, TimeSpan? offset)
        {
            if (entity?.CommonData == null)
                return logHeaderUpdate;

            if (entity.CommonData.DateTimeCreation.HasValue)
            {
                var creationTime = entity.CommonData.DateTimeCreation.ToOffsetTime(offset);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "CommonData.DateTimeCreation", creationTime?.ToString("o"));
                Logger.DebugFormat("Updating Common Data create time to '{0}'", creationTime);
            }

            if (entity.CommonData.DateTimeLastChange.HasValue)
            {
                var updateTime = entity.CommonData.DateTimeLastChange.ToOffsetTime(offset);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "CommonData.DateTimeLastChange", updateTime?.ToString("o"));
                Logger.DebugFormat("Updating Common Data update time to '{0}'", updateTime);
            }

            return logHeaderUpdate;
        }

        /// <summary>
        /// Converts a logCurveInfo to an index metadata record.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="indexCurve">The index curve.</param>
        /// <param name="scale">The scale.</param>
        /// <returns></returns>
        protected override IndexMetadataRecord ToIndexMetadataRecord(Log entity, LogCurveInfo indexCurve, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = indexCurve.GetUri(entity),
                Mnemonic = indexCurve.Mnemonic.Value,
                Description = indexCurve.CurveDescription,
                Uom = indexCurve.Unit,
                Scale = scale,
                IndexType = IsTimeLog(entity, true)
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = IsIncreasing(entity)
                    ? IndexDirections.Increasing
                    : IndexDirections.Decreasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        /// <summary>
        /// Converts a logCurveInfo to a channel metadata record.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="indexMetadata">The index metadata.</param>
        /// <returns></returns>
        protected override ChannelMetadataRecord ToChannelMetadataRecord(Log entity, LogCurveInfo curve, IndexMetadataRecord indexMetadata)
        {
            var uri = curve.GetUri(entity);
            var isTimeLog = IsTimeLog(entity, true);
            var curveIndexes = GetCurrentIndexRange(entity);
            var dataObject = new DataObject();

            StoreStoreProvider.SetDataObject(dataObject, curve, uri, curve.Mnemonic.Value, 0);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = curve.TypeLogData.GetValueOrDefault(LogDataType.@double).ToString().Replace("@", string.Empty),
                Description = curve.CurveDescription ?? curve.Mnemonic.Value,
                ChannelName = curve.Mnemonic.Value,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml ?? ObjectTypes.Unknown,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic.Value,
                DomainObject = dataObject,
                Status = ChannelStatuses.Active,
                StartIndex = curveIndexes[curve.Mnemonic.Value].Start.IndexToScale(indexMetadata.Scale, isTimeLog),
                EndIndex = curveIndexes[curve.Mnemonic.Value].End.IndexToScale(indexMetadata.Scale, isTimeLog),
                Indexes = new List<IndexMetadataRecord>()
                {
                    indexMetadata
                },
                CustomData = new Dictionary<string, DataValue>()
            };
        }

        /// <summary>
        /// Gets the log data delimiter.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        protected override string GetLogDataDelimiter(Log entity)
        {
            return entity.DataDelimiter ?? base.GetLogDataDelimiter(entity);
        }

        /// <summary>
        /// Partially delete the log data.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="channels">The current logCurve information.</param>
        /// <param name="currentRanges">The current channel index ranges.</param>
        /// <param name="transaction">The transaction.</param>
        protected override void PartialDeleteLogData(EtpUri uri, WitsmlQueryParser parser, List<LogCurveInfo> channels, Dictionary<string, Range<double?>> currentRanges, MongoTransaction transaction)
        {
            var uidToMnemonics = channels.ToDictionary(c => c.Uid, c => c.Mnemonic.Value);
            var updatedRanges = new Dictionary<string, Range<double?>>();

            WitsmlParser.RemoveEmptyElements(parser.Root);
            var delete = WitsmlParser.Parse<LogList>(parser.Root, false).Log.FirstOrDefault();
            var current = GetEntity(uri);
            
            delete.IndexType = current.IndexType;
            delete.Direction = current.Direction;

            var indexRange = currentRanges[current.IndexCurve];
            if (!indexRange.Start.HasValue || !ToDeleteLogData(delete, parser))
                return;

            TimeSpan? offset = null;
            var indexCurve = current.IndexCurve;
            var indexChannel = current.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == indexCurve);
            
            if (DeleteAllLogData(current, delete, updatedRanges))
            {
                ChannelDataChunkAdapter.Delete(uri);
                foreach (var curve in current.LogCurveInfo)
                {
                    updatedRanges.Add(curve.Mnemonic.Value, new Range<double?>(null, null));
                }
            }
            else
            {               
                var deletedChannels = GetDeletedChannels(current, uidToMnemonics);
                var defaultDeleteRange = GetDefaultDeleteRange(current, delete);

                var isTimeLog = current.IsTimeLog();
                var updateRanges = GetDeleteQueryIndexRange(delete, uidToMnemonics, current.IsIncreasing(), isTimeLog);
                offset = currentRanges[indexCurve].Offset;
                
                var ranges = MergePartialDeleteRanges(deletedChannels, defaultDeleteRange, currentRanges, updateRanges, indexCurve, current.IsIncreasing());

                ChannelDataChunkAdapter.PartialDeleteLogData(uri, indexCurve, current.IsIncreasing(), isTimeLog, deletedChannels, ranges, updatedRanges, transaction);
            }

            UpdateIndexRange(uri, current, updatedRanges, updatedRanges.Keys.ToList(), current.IsTimeLog(), indexChannel?.Unit, offset, true);
        }

        private List<string> GetDeletedChannels(Log current, Dictionary<string, string> uidToMnemonics)
        {
            var uids = current.LogCurveInfo.Select(l => l.Uid).ToList();
            return uidToMnemonics.Where(u => !uids.Contains(u.Key)).Select(u => u.Value).ToList();
        }

        private Dictionary<string, Range<double?>> GetDeleteQueryIndexRange(Log entity, Dictionary<string, string> uidToMnemonics, bool increasing, bool isTimeLog)
        {
            var ranges = new Dictionary<string, Range<double?>>();          

            foreach (var curve in entity.LogCurveInfo)
            {
                var mnemonic = curve.Mnemonic?.Value;
                if (string.IsNullOrEmpty(mnemonic))
                    mnemonic = uidToMnemonics[curve.Uid];

                if (!uidToMnemonics.ContainsValue(mnemonic))
                    continue;

                var range = GetIndexRange(curve, increasing, isTimeLog);
                ranges.Add(mnemonic, range);
            }

            return ranges;
        }

        private bool ToDeleteLogData(Log entity, WitsmlQueryParser parser)
        {
            var isTimeLog = entity.IsTimeLog();
            var curves = entity.LogCurveInfo;

            if (isTimeLog)
            {
                if (entity.StartDateTimeIndex.HasValue || entity.EndDateTimeIndex.HasValue
                    || (curves != null && curves.Any(c => c.MinDateTimeIndex.HasValue || c.MaxDateTimeIndex.HasValue)))
                    return true;
            }
            else
            {
                if (entity.StartIndex != null || entity.EndIndex != null
                    || (curves != null && curves.Any(c => c.MinIndex != null || c.MaxIndex != null)))
                    return true;
            }

            return ToDeleteChannelDataByMnemonic(parser, isTimeLog);
        }

        private bool ToDeleteChannelDataByMnemonic(WitsmlQueryParser parser, bool isTimeLog)
        {
            var fields = new List<string> {"mnemonic"};
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

        private bool DeleteAllLogData(Log current, Log entity, Dictionary<string, Range<double?>> updatedRanges)
        {
            if (current.LogCurveInfo == null || current.LogCurveInfo.Count == 0)
                return true;

            if (current.LogCurveInfo.Count == 1 && current.LogCurveInfo.Any(l => l.Mnemonic.Value == current.IndexCurve))
            {
                updatedRanges.Add(current.IndexCurve, new Range<double?>(null, null));
                return true;
            }                

            var increasing = current.IsIncreasing();
            var isTimeLog = current.IsTimeLog();
            var deleteAll = false;

            if (isTimeLog)
            {
                if (entity.StartDateTimeIndex.HasValue)
                {
                    if (StartsBefore(entity.StartDateTimeIndex.Value.ToUnixTimeMicroseconds(), current.StartDateTimeIndex.Value.ToUnixTimeMicroseconds(), increasing) && 
                        (!entity.EndDateTimeIndex.HasValue ||
                        StartsBefore(current.EndDateTimeIndex.Value.ToUnixTimeMicroseconds(),
                            entity.EndDateTimeIndex.Value.ToUnixTimeMicroseconds(), increasing)))
                        deleteAll = true;
                }
                else
                {
                    if (entity.EndDateTimeIndex.HasValue &&
                        StartsBefore(current.EndDateTimeIndex.Value.ToUnixTimeMicroseconds(),
                            entity.EndDateTimeIndex.Value.ToUnixTimeMicroseconds(), increasing))
                        deleteAll = true;
                }
            }
            else
            {
                if (entity.StartIndex != null)
                {
                    if (StartsBefore(entity.StartIndex.Value, current.StartIndex.Value, increasing) &&
                        (entity.EndIndex == null || StartsBefore(current.EndIndex.Value, entity.EndIndex.Value, increasing)))
                        deleteAll = true;
                }
                else
                {
                    if (entity.EndIndex != null &&
                        StartsBefore(current.EndIndex.Value, entity.EndIndex.Value, increasing))
                        deleteAll = true;
                }
            }

            if (deleteAll)
            {
                var indexCurve = current.LogCurveInfo.FirstOrDefault(l => l.Mnemonic.Value == current.IndexCurve);
                if (entity.LogCurveInfo.Any(l => l.Uid != indexCurve?.Uid || l.Mnemonic.Value != current.IndexCurve))
                    deleteAll = false;
            }

            return deleteAll;
        }

        private Range<double?> GetDefaultDeleteRange(Log current, Log entity)
        {
            var isTimeLog = current.IsTimeLog();

            double? start;
            double? end;

            if (isTimeLog)
            {
                if (!entity.StartDateTimeIndex.HasValue && !entity.EndDateTimeIndex.HasValue)
                    return new Range<double?>(null, null);

                start = entity.StartDateTimeIndex?.ToUnixTimeMicroseconds() ?? current.StartDateTimeIndex.Value.ToUnixTimeMicroseconds();

                end = entity.EndDateTimeIndex?.ToUnixTimeMicroseconds() ?? current.EndDateTimeIndex.Value.ToUnixTimeMicroseconds();
            }
            else
            {
                if (entity.StartIndex == null && entity.EndIndex == null)
                    return new Range<double?>(null, null);

                start = entity.StartIndex?.Value ?? current.StartIndex.Value;

                end = entity.EndIndex?.Value ?? current.EndIndex.Value;
            }

            return new Range<double?>(start, end);
        }

        /// <summary>
        /// Extracts the data readers.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="existing">The existing.</param>
        /// <returns></returns>
        private IEnumerable<ChannelDataReader> ExtractDataReaders(Log entity, Log existing = null)
        {
            Logger.Debug("Extracing data readers from log.");

            if (existing == null)
            {
                var readers = entity.GetReaders().ToList();
                entity.LogData = null;
                return readers;
            }

            existing.LogData = entity.LogData;
            return existing.GetReaders().ToList();
        }

        /// <summary>
        /// Clears the index values.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        private void ClearIndexValues(Log dataObject)
        {
            Logger.Debug("Clearing log and logCurveInfo index ranges.");

            if (IsTimeLog(dataObject))
            {
                dataObject.StartDateTimeIndex = null;
                dataObject.StartDateTimeIndexSpecified = false;
                dataObject.EndDateTimeIndex = null;
                dataObject.EndDateTimeIndexSpecified = false;

                foreach (var curve in dataObject.LogCurveInfo)
                {
                    curve.MinDateTimeIndex = null;
                    curve.MinDateTimeIndexSpecified = false;
                    curve.MaxDateTimeIndex = null;
                    curve.MaxDateTimeIndexSpecified = false;
                }
            }
            else
            {
                dataObject.StartIndex = null;
                dataObject.EndIndex = null;

                foreach (var curve in dataObject.LogCurveInfo)
                {
                    curve.MinIndex = null;
                    curve.MaxIndex = null;
                }
            }
        }

        /// <summary>
        /// Sets the index curve range.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="ranges">The ranges.</param>
        /// <param name="indexCurve">The index curve.</param>
        /// <param name="isTimeLog">if set to <c>true</c> [is time log].</param>
        private void SetIndexCurveRange(Log log, Dictionary<string, Range<double?>> ranges, string indexCurve, bool isTimeLog)
        {
            if (!ranges.ContainsKey(indexCurve)) return;

            var range = ranges[indexCurve];
            Logger.DebugFormat("Setting log index curve ranges: {0}; {1}", indexCurve, range);

            if (isTimeLog)
            {
                if (log.StartDateTimeIndex != null)
                {
                    if (range.Start.HasValue && !double.IsNaN(range.Start.Value))
                        log.StartDateTimeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.Start.Value);
                    else
                        log.StartDateTimeIndex = null;
                }

                if (log.EndDateTimeIndex != null)
                {
                    if (range.End.HasValue && !double.IsNaN(range.End.Value))
                        log.EndDateTimeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.End.Value);
                    else
                        log.EndDateTimeIndex = null;
                }
            }
            else
            {
                if (log.StartIndex != null)
                {
                    if (range.Start.HasValue)
                        log.StartIndex.Value = range.Start.Value;
                    else
                        log.StartIndex = null;
                }

                if (log.EndIndex != null)
                {
                    if (range.End.HasValue)
                        log.EndIndex.Value = range.End.Value;
                    else
                        log.EndIndex = null;
                }
            }
        }
    }
}
