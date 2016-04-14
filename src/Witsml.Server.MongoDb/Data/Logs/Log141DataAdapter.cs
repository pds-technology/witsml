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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 141 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.Logs.LogDataAdapter{Log, LogCurveInfo}" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml141Configuration" />
    [Export(typeof(IEtpDataAdapter))]
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export141(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [Export141(ObjectTypes.Log, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataAdapter : LogDataAdapter<Log, LogCurveInfo>, IWitsml141Configuration
    {
        private static readonly int MaxDataNodes = Settings.Default.MaxDataNodes;
        private static readonly int MaxDataPoints = Settings.Default.MaxDataPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log141DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log141DataAdapter(IDatabaseProvider databaseProvider) : base(databaseProvider, ObjectNames.Log141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Log"/> object.
        /// </summary>
        /// <param name="capServer">The capServer instance.</param>
        public void GetCapabilities(CapServer capServer)
        {
            capServer.Add(Functions.GetFromStore, new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = MaxDataNodes,
                MaxDataPoints = MaxDataPoints
            });

            capServer.Add(Functions.AddToStore, new ObjectWithConstraint(ObjectTypes.Log)
            {
                MaxDataNodes = MaxDataNodes,
                MaxDataPoints = MaxDataPoints
            });

            capServer.Add(Functions.UpdateInStore, ObjectTypes.Log);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Log);
        }

        /// <summary>
        /// Adds a <see cref="Log"/> entity to the data store.
        /// </summary>
        /// <param name="entity">The <see cref="Log"/> to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(Log entity)
        {
            SetDefaultValues(entity);
            Logger.DebugFormat("Adding Log with uid '{0}' and name '{1}'", entity.Uid, entity.Name);

            Validate(Functions.AddToStore, entity);
            Logger.DebugFormat("Validated Log with uid '{0}' and name '{1}' for Add", entity.Uid, entity.Name);

            // Extract Data                    
            var reader = ExtractDataReaders(entity).FirstOrDefault();

            // Insert Log and Log Data
            InsertEntity(entity);
            InsertLogData(entity, reader);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }

        /// <summary>
        /// Updates the specified <see cref="Log"/> instance in the store.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<Log>();
            Logger.DebugFormat("Updating Log with uid '{0}'.", uri.ObjectId);

            // Extract Data
            var entity = Parse(parser.Context.Xml);

            Validate(Functions.UpdateInStore, entity);
            Logger.DebugFormat("Validated Log with uid '{0}' and name '{1}' for Update", uri, entity.Name);

            var ignored = GetIgnoredElementNames().Concat(new[] { "direction" }).ToArray();
            UpdateEntity(parser, uri, ignored);

            // Update Log Data and Index Range
            var readers = ExtractDataReaders(entity, GetEntity(uri));
            UpdateLogDataAndIndexRange(uri, readers);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>An instance of <see cref="Log" />.</returns>
        protected override Log Parse(string xml)
        {
            var list = WitsmlParser.Parse<LogList>(xml);
            return list.Log.FirstOrDefault();
        }

        protected override IEnergisticsCollection CreateCollection(List<Log> entities)
        {
            return new LogList() { Log = entities };
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
            return curve?.Mnemonic?.Value;
        }

        protected override string GetIndexCurveMnemonic(Log log)
        {
            return log.IndexCurve;
        }

        protected override IDictionary<int, string> GetUnitsByColumnIndex(Log log)
        {
            return log.LogCurveInfo
                .Select(x => x.Unit)
                .ToArray()
                .Select((unit, index) => new { Unit = unit, Index = index })
                .ToDictionary(x => x.Index, x => x.Unit);
        }

        protected override Range<double?> GetIndexRange(Log log, LogCurveInfo curve)
        {
            double? start = null;
            double? end = null;

            if (curve.MinIndex != null)
                start = curve.MinIndex.Value;
            if (curve.MaxIndex != null)
                end = curve.MaxIndex.Value;

            return new Range<double?>(start, end);
        }

        protected override Range<double?> GetDateTimeIndexRange(Log log, LogCurveInfo curve)
        {
            double? start = null;
            double? end = null;

            if (curve.MinDateTimeIndex.HasValue)
                start = DateTimeOffset.Parse(curve.MinDateTimeIndex.Value.ToString("o")).ToUnixTimeSeconds();
            if (curve.MaxDateTimeIndex.HasValue)
                end = DateTimeOffset.Parse(curve.MaxDateTimeIndex.Value.ToString("o")).ToUnixTimeSeconds();

            return new Range<double?>(start, end);
        }

        protected override void SetLogDataValues(Log log, List<string> logDataValues, IEnumerable<string> mnemonics, IDictionary<int, string> units)
        {
            if (log.LogData == null)
                log.LogData = new List<LogData>();

            log.LogData.Add(new LogData()
            {
                MnemonicList = string.Join(",", mnemonics),
                UnitList = string.Join(",", units.Values),
                Data = logDataValues
            });
        }

        protected override void SetLogIndexRange(Log log)
        {
            var isTimeLog = IsTimeLog(log);
            var reader = log.GetReaders().FirstOrDefault();

            if (reader != null && reader.Read())
            {
                if (isTimeLog)
                    log.StartDateTimeIndex = reader.GetDateTimeOffset(0);
                else
                    log.StartIndex.Value = reader.GetDouble(0);

                reader.MoveTo(reader.RecordsAffected - 1);

                if (isTimeLog)
                    log.EndDateTimeIndex = reader.GetDateTimeOffset(0);
                else
                    log.EndIndex.Value = reader.GetDouble(0);
            }
        }

        protected override UpdateDefinition<Log> UpdateCommonData(MongoDbUpdate<Log> mongoUpdate, UpdateDefinition<Log> logHeaderUpdate, Log entity, TimeSpan? offset)
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
                Description = curve.CurveDescription ?? curve.Mnemonic.Value,
                Mnemonic = curve.Mnemonic.Value,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml ?? ObjectTypes.Unknown,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic.Value,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                StartIndex = curveIndexes[curve.Mnemonic.Value].Start.IndexToScale(indexMetadata.Scale, isTimeLog),
                EndIndex = curveIndexes[curve.Mnemonic.Value].End.IndexToScale(indexMetadata.Scale, isTimeLog),
                Indexes = new List<IndexMetadataRecord>()
                {
                    indexMetadata
                }
            };
        }

        private void SetDefaultValues(Log entity)
        {
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Create();

            if (!entity.Direction.HasValue)
            {
                entity.Direction = LogIndexDirection.increasing;
            }

            if (entity.LogCurveInfo != null)
            {
                foreach (var logCurve in entity.LogCurveInfo)
                {
                    if (string.IsNullOrWhiteSpace(logCurve.Uid))
                        logCurve.Uid = logCurve.Mnemonic.Value;
                }
            }
        }

        private IEnumerable<ChannelDataReader> ExtractDataReaders(Log entity, Log existing = null)
        {
            if (existing == null)
            {
                var readers = entity.GetReaders().ToList();
                entity.LogData = null;
                return readers;
            }

            existing.LogData = entity.LogData;
            return existing.GetReaders().ToList();
        }
    }
}
