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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 131 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.Logs.LogDataAdapter{Log, LogCurveInfo}" />
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IWitsml131Configuration))]
    [Export131(ObjectTypes.Log, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log131DataAdapter : LogDataAdapter<Log, LogCurveInfo>, IWitsml131Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Log131DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log131DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Log131)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Log"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, ObjectTypes.Log);
            capServer.Add(Functions.AddToStore, ObjectTypes.Log);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Log);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Log);
        }

        /// <summary>
        /// Adds a <see cref="Log" /> entity to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The Log instance to add to the store.</param>
        public override void Add(WitsmlQueryParser parser, Log dataObject)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                ClearIndexValues(dataObject);

                // Extract Data
                var reader = ExtractDataReader(dataObject);
                InsertEntity(dataObject, transaction);
                UpdateLogDataAndIndexRange(dataObject.GetUri(), new[] { reader }, transaction);
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

                // Update Log Data and Index Range
                var reader = ExtractDataReader(dataObject, GetEntity(uri));
                UpdateLogDataAndIndexRange(uri, new[] { reader }, transaction);
                transaction.Commit();
            }
        }

        protected override object CreateGenericMeasure(double value, string uom)
        {
            return new GenericMeasure() { Value = value, Uom = uom };
        }

        protected override bool IsIncreasing(Log log)
        {
            return log.Direction.GetValueOrDefault() == LogIndexDirection.increasing;
        }

        protected override bool IsTimeLog(Log log, bool includeElapsedTime = false)
        {
            return log.IndexType.GetValueOrDefault() == LogIndexType.datetime ||
                   (log.IndexType.GetValueOrDefault() == LogIndexType.elapsedtime && includeElapsedTime);
        }

        protected override List<LogCurveInfo> GetLogCurves(Log log)
        {
            return log.LogCurveInfo;
        }

        protected override string GetMnemonic(LogCurveInfo curve)
        {
            return curve?.Mnemonic;
        }

        protected override string GetIndexCurveMnemonic(Log log)
        {
            return log.IndexCurve.Value;
        }

        protected override IDictionary<int, string> GetUnitsByColumnIndex(Log log)
        {
            return log.LogCurveInfo
                .Select(x => x.Unit)
                .ToArray()
                .Select((unit, index) => new { Unit = unit, Index = index })
                .ToDictionary(x => x.Index, x => x.Unit);
        }

        protected override IDictionary<int, string> GetNullValuesByColumnIndex(Log log)
        {
            return log.LogCurveInfo
                .Select(x => x.NullValue)
                .ToArray()
                .Select((nullValue, index) => new { NullValue = string.IsNullOrWhiteSpace(nullValue) ? log.NullValue : nullValue, Index = index })
                .ToDictionary(x => x.Index, x => x.NullValue);
        }

        protected override Range<double?> GetIndexRange(LogCurveInfo curve, bool increasing = true, bool isTimeIndex = false)
        {
            return curve.GetIndexRange(increasing, isTimeIndex);
        }

        protected override void SetLogDataValues(Log log, List<string> logDataValues, IEnumerable<string> mnemonics, IEnumerable<string> units)
        {
            log.LogData = logDataValues;
        }

        protected override void SetLogIndexRange(Log log, Dictionary<string, Range<double?>> ranges)
        {
            if (log.LogCurveInfo == null)
                return;

            var isTimeLog = IsTimeLog(log);
            var increasing = IsIncreasing(log);

            foreach (var logCurve in log.LogCurveInfo)
            {
                var mnemonic = logCurve.Mnemonic;
                Range<double?> range;

                if (!ranges.TryGetValue(mnemonic, out range))
                    continue;

                // Sort range in min/max order
                range = range.Sort();

                if (isTimeLog)
                {
                    if (range.Start.HasValue && !double.IsNaN(range.Start.Value))
                        logCurve.MinDateTimeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.Start.Value).DateTime;
                    if (range.End.HasValue && !double.IsNaN(range.End.Value))
                        logCurve.MaxDateTimeIndex = DateTimeExtensions.FromUnixTimeMicroseconds((long)range.End.Value).DateTime;

                    if (logCurve.ColumnIndex == log.IndexCurve.ColumnIndex)
                    {
                        log.StartDateTimeIndex = increasing ? logCurve.MinDateTimeIndex : logCurve.MaxDateTimeIndex;
                        log.EndDateTimeIndex = increasing ? logCurve.MaxDateTimeIndex : logCurve.MinDateTimeIndex;
                    }
                }
                else
                {
                    if (range.Start.HasValue)
                        logCurve.MinIndex.Value = range.Start.Value;
                    if (range.End.HasValue)
                        logCurve.MaxIndex.Value = range.End.Value;

                    if (logCurve.ColumnIndex == log.IndexCurve.ColumnIndex)
                    {
                        log.StartIndex.Value = increasing ? logCurve.MinIndex.Value : logCurve.MaxIndex.Value;
                        log.EndIndex.Value = increasing ? logCurve.MaxIndex.Value : logCurve.MinIndex.Value;
                    }
                }
            }
        }

        protected override UpdateDefinition<Log> UpdateCommonData(MongoDbUpdate<Log> mongoUpdate, UpdateDefinition<Log> logHeaderUpdate, Log entity, TimeSpan? offset)
        {
            if (entity?.CommonData == null)
                return logHeaderUpdate;

            if (entity.CommonData.DateTimeCreation.HasValue)
            {
                var creationTime = entity.CommonData.DateTimeCreation; //.ToOffsetTime(offset);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "CommonData.DateTimeCreation", creationTime?.ToString("o"));
                Logger.DebugFormat("Updating Common Data create time to '{0}'", creationTime);
            }

            if (entity.CommonData.DateTimeLastChange.HasValue)
            {
                var updateTime = entity.CommonData.DateTimeLastChange; //.ToOffsetTime(offset);
                logHeaderUpdate = MongoDbUtility.BuildUpdate(logHeaderUpdate, "CommonData.DateTimeLastChange", updateTime?.ToString("o"));
                Logger.DebugFormat("Updating Common Data update time to '{0}'", updateTime);
            }

            return logHeaderUpdate;
        }

        protected override IndexMetadataRecord ToIndexMetadataRecord(Log entity, LogCurveInfo indexCurve, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = indexCurve.GetUri(entity),
                Mnemonic = indexCurve.Mnemonic,
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

        protected override ChannelMetadataRecord ToChannelMetadataRecord(Log entity, LogCurveInfo curve, IndexMetadataRecord indexMetadata)
        {
            var uri = curve.GetUri(entity);
            var isTimeLog = IsTimeLog(entity, true);
            var curveIndexes = GetCurrentIndexRange(entity);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = curve.TypeLogData.GetValueOrDefault(LogDataType.@double).ToString().Replace("@", string.Empty),
                Description = curve.CurveDescription ?? curve.Mnemonic,
                Mnemonic = curve.Mnemonic,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml?.Name ?? ObjectTypes.Unknown,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                StartIndex = curveIndexes[curve.Mnemonic].Start.IndexToScale(indexMetadata.Scale, isTimeLog),
                EndIndex = curveIndexes[curve.Mnemonic].End.IndexToScale(indexMetadata.Scale, isTimeLog),
                Indexes = new List<IndexMetadataRecord>()
                {
                    indexMetadata
                }
            };
        }

        private ChannelDataReader ExtractDataReader(Log entity, Log existing = null)
        {
            if (existing == null)
            {
                var reader = entity.GetReader();
                entity.LogData = null;
                return reader;
            }

            existing.LogData = entity.LogData;
            return existing.GetReader();
        }

        private void ClearIndexValues(Log dataObject)
        {
            var isTimeLog = dataObject.IndexType.GetValueOrDefault() == LogIndexType.datetime;

            if (dataObject.IndexType.GetValueOrDefault() == LogIndexType.datetime)
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
    }
}
