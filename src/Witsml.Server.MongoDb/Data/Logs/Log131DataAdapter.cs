using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 131 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML131.Log}" />
    /// <seealso cref="PDS.Witsml.Server.Data.Channels.IChannelDataProvider" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml131Configuration" />
    [Export(typeof(IWitsml131Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export131(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [Export131(ObjectTypes.Log, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log131DataAdapter : MongoDbDataAdapter<Log>, IChannelDataProvider, IWitsml131Configuration
    {
        private static readonly bool StreamIndexValuePairs = Settings.Default.StreamIndexValuePairs;
        private readonly ChannelDataChunkAdapter _channelDataChunkAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log131DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Log131DataAdapter(IDatabaseProvider databaseProvider, ChannelDataChunkAdapter channelDataChunkAdapter) : base(databaseProvider, ObjectNames.Log131)
        {
            _channelDataChunkAdapter = channelDataChunkAdapter;
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
        /// Queries the object(s) specified by the parser.
        /// </summary>
        /// <param name="parser">The parser that specifies the query parameters.</param>
        /// <returns>Queried objects.</returns>
        public override WitsmlResult<IEnergisticsCollection> Query(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();
            Logger.DebugFormat("Querying with return elements '{0}'", returnElements);

            var fields = OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : OptionsIn.ReturnElements.DataOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, "UidWell", "UidWellbore" }
                : OptionsIn.ReturnElements.Requested.Equals(returnElements)
                ? new List<string>()
                : null;

            var ignored = new List<string> { "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "logData" };
            var logs = QueryEntities(parser, fields, ignored);

            if (OptionsIn.ReturnElements.All.Equals(returnElements) ||
                OptionsIn.ReturnElements.DataOnly.Equals(returnElements) ||
                (fields != null && fields.Contains("LogData")))
            {
                var logHeaders = GetEntities(logs.Select(x => x.GetUri()))
                    .ToDictionary(x => x.GetUri());

                logs.ForEach(l =>
                {
                    var logHeader = logHeaders[l.GetUri()];
                    var mnemonics = GetMnemonicList(logHeader, parser);

                    l.LogData = QueryLogDataValues(logHeader, parser, mnemonics);

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

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new LogList()
                {
                    Log = logs
                });
        }

        /// <summary>
        /// Adds a <see cref="Log"/> entity to the data store.
        /// </summary>
        /// <param name="entity">The Log instance to add to the store.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(Log entity)
        {
            entity.Uid = NewUid(entity.Uid);
            entity.CommonData = entity.CommonData.Create();
            if (!entity.Direction.HasValue)
            {
                entity.Direction = LogIndexDirection.increasing;
            }

            Logger.DebugFormat("Adding Log with uid '{0}' and name '{1}'", entity.Uid, entity.Name);

            //Validate(Functions.AddToStore, entity);
            //Logger.DebugFormat("Validated Log with uid '{0}' and name '{1}' for Add", entity.Uid, entity.Name);

            // Extract Data
            var reader = ExtractDataReader(entity);

            InsertEntity(entity);

            if (reader != null)
            {
                var indexCurve = reader.Indices[0];
                var allMnemonics = new[] { indexCurve.Mnemonic }.Concat(reader.Mnemonics).ToArray();

                var ranges = GetCurrentIndexRange(entity);
                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, entity.Direction == LogIndexDirection.increasing);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Add(reader);

                // Update index range
                UpdateIndexRange(entity.GetUri(), entity, ranges, allMnemonics, entity.IndexType == LogIndexType.datetime, indexCurve.Unit);
            }

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
            //Validate(Functions.UpdateInStore, entity);

            var ignored = new[] { "logData", "direction" };
            UpdateEntity(parser, uri, ignored);

            // Extract Data
            var entity = Parse(parser.Context.Xml);
            var reader = ExtractDataReader(entity, GetEntity(uri));

            UpdateLogDataAndIndex(uri, reader);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            UpdateLogDataAndIndex(uri, reader);
        }

        /// <summary>
        /// Gets the channel metadata for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(EtpUri uri)
        {
            var entity = GetEntity(uri);
            var metadata = new List<ChannelMetadataRecord>();
            var index = 0;

            if (entity.LogCurveInfo == null || !entity.LogCurveInfo.Any())
                return metadata;

            var indexCurve = entity.LogCurveInfo.FirstOrDefault(x => x.Mnemonic == entity.IndexCurve.Value);
            var indexMetadata = ToIndexMetadataRecord(entity, indexCurve);

            // Skip the indexCurve after updating the ChannelStreamingProducer
            metadata.AddRange(
                entity.LogCurveInfo
                .Where(x => StreamIndexValuePairs || !x.Mnemonic.EqualsIgnoreCase(indexCurve.Mnemonic))
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
            var mnemonics = entity.LogCurveInfo.Select(x => x.Mnemonic);
            var increasing = entity.Direction.GetValueOrDefault() == LogIndexDirection.increasing;

            return GetChannelData(uri, mnemonics.First(), range, increasing);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<Log> GetAll(EtpUri? parentUri = null)
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
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Delete(EtpUri uri)
        {
            var result = base.Delete(uri);

            if (result.Code == ErrorCodes.Success)
                result = _channelDataChunkAdapter.Delete(uri);

            return result;
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

        private IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, string indexChannel, Range<double?> range, bool increasing)
        {
            var chunks = _channelDataChunkAdapter.GetData(uri, indexChannel, range, increasing);
            return chunks.GetRecords(range, increasing);
        }

        private List<string> QueryLogDataValues(Log log, WitsmlQueryParser parser, IDictionary<int, string> mnemonics)
        {
            var range = GetLogDataSubsetRange(log, parser);
            var increasing = log.Direction.GetValueOrDefault() == LogIndexDirection.increasing;
            var records = GetChannelData(log.GetUri(), mnemonics[0], range, increasing);

            return FormatLogData(records, mnemonics);
        }

        private Range<double?> GetLogDataSubsetRange(Log log, WitsmlQueryParser parser)
        {
            var isTimeLog = log.IndexType.GetValueOrDefault() == LogIndexType.datetime;

            return Range.Parse(
                parser.PropertyValue(isTimeLog ? "startDateTimeIndex" : "startIndex"),
                parser.PropertyValue(isTimeLog ? "endDateTimeIndex" : "endIndex"),
                isTimeLog);
        }

        private IEnumerable<string> GetLogCurveInfoMnemonics(WitsmlQueryParser parser)
        {
            var mnemonics = Enumerable.Empty<string>();
            var logCurveInfos = parser.Properties("logCurveInfo");

            if (logCurveInfos != null && logCurveInfos.Any())
            {
                var mnemonicList = parser.Properties(logCurveInfos, "mnemonic");

                if (mnemonicList != null && mnemonicList.Any())
                {
                    mnemonics = mnemonicList.Select(x => x.Value);
                }
            }

            return mnemonics;
        }

        private IDictionary<int, string> ComputeMnemonicIndexes(string[] allMnemonics, string[] queryMnemonics, string returnElements)
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

        private IDictionary<int, string> GetMnemonicList(Log log, WitsmlQueryParser parser)
        {
            if (log.LogCurveInfo == null)
                return new Dictionary<int, string>(0);

            var allMnemonics = log.LogCurveInfo.Select(x => x.Mnemonic).ToArray();
            var queryMnemonics = GetLogCurveInfoMnemonics(parser).ToArray();

            return ComputeMnemonicIndexes(allMnemonics, queryMnemonics, parser.ReturnElements());
        }

        private List<string> FormatLogData(IEnumerable<IChannelDataRecord> records, IDictionary<int, string> mnemonics)
        {
            var logData = new List<string>();
            var slices = mnemonics.Keys.ToArray();

            foreach (var record in records)
            {
                var values = new object[record.FieldCount];
                record.GetValues(values);

                // use timestamp format for time index values
                if (record.Indices.Select(x => x.IsTimeIndex).FirstOrDefault())
                    values[0] = record.GetDateTimeOffset(0).ToString("o");

                // Limit data to requested mnemonics
                if (slices.Any())
                {
                    values = values
                        .Where((x, i) => slices.Contains(i))
                        .ToArray();
                }

                logData.Add(string.Join(",", values));
            }

            return logData;
        }

        private void FormatLogHeader(Log log, IDictionary<int, string> mnemonics, string returnElements)
        {
            // Remove LogCurveInfos from the Log header if slicing by column
            if (log.LogCurveInfo != null && !OptionsIn.ReturnElements.All.Equals(returnElements))
            {
                log.LogCurveInfo.RemoveAll(x => !mnemonics.Values.Contains(x.Mnemonic));
            }
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

        private ChannelMetadataRecord ToChannelMetadataRecord(Log entity, LogCurveInfo curve, IndexMetadataRecord indexMetadata)
        {
            var uri = curve.GetUri(entity);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = curve.TypeLogData.GetValueOrDefault(LogDataType.@double).ToString().Replace("@", string.Empty),
                Description = curve.CurveDescription ?? curve.Mnemonic,
                Mnemonic = curve.Mnemonic,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml == null ? ObjectTypes.Unknown : curve.ClassWitsml.Name,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                Indexes = new List<IndexMetadataRecord>()
                {
                    indexMetadata
                }
            };
        }

        private IndexMetadataRecord ToIndexMetadataRecord(Log entity, LogCurveInfo indexCurve, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = indexCurve.GetUri(entity),
                Mnemonic = indexCurve.Mnemonic,
                Description = indexCurve.CurveDescription,
                Uom = indexCurve.Unit,
                Scale = scale,
                IndexType = entity.IndexType == LogIndexType.datetime || entity.IndexType == LogIndexType.elapsedtime
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = entity.Direction == LogIndexDirection.decreasing
                    ? IndexDirections.Decreasing
                    : IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private void UpdateLogDataAndIndex(EtpUri uri, ChannelDataReader reader)
        {
            var current = GetEntity(uri);

            // Merge ChannelDataChunks
            if (reader != null)
            {
                var indexCurve = reader.Indices[0];
                var allMnemonics = new[] { indexCurve.Mnemonic }.Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(current);
                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, current.Direction == LogIndexDirection.increasing);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Merge(reader);

                // Update index range
                UpdateIndexRange(uri, current, ranges, allMnemonics, current.IndexType == LogIndexType.datetime, indexCurve.Unit);
            }
        }

        private GenericMeasure UpdateGenericMeasure(GenericMeasure gmObject, double gmValue, string uom)
        {
            if (gmObject == null)
            {
                gmObject = new GenericMeasure();
            }
            gmObject.Value = gmValue;
            gmObject.Uom = uom;

            return gmObject;
        }

        private Dictionary<string, List<double?>> GetCurrentIndexRange(Log entity)
        {
            var ranges = new Dictionary<string, List<double?>>();
            var isTimeLog = entity.IndexType == LogIndexType.datetime;

            foreach (var curve in entity.LogCurveInfo)
            {
                var range = new List<double?> { null, null };
                if (isTimeLog)
                {
                    if (curve.MinDateTimeIndex.HasValue)
                        range[0] = DateTimeOffset.Parse(curve.MinDateTimeIndex.Value.ToString("o")).ToUnixTimeSeconds();
                    if (curve.MaxDateTimeIndex.HasValue)
                        range[1] = DateTimeOffset.Parse(curve.MaxDateTimeIndex.Value.ToString("o")).ToUnixTimeSeconds();
                }
                else
                {
                    if (curve.MinIndex != null)
                        range[0] = curve.MinIndex.Value;
                    if (curve.MaxIndex != null)
                        range[1] = curve.MaxIndex.Value;
                }
                ranges.Add(curve.Mnemonic, range);
            }

            return ranges;
        }

        private void GetUpdatedLogHeaderIndexRange(ChannelDataReader reader, string[] mnemonics, Dictionary<string, List<double?>> ranges, bool increasing = true)
        {
            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                List<double?> current;

                if (ranges.ContainsKey(mnemonic))
                {
                    current = ranges[mnemonic];
                }
                else
                {
                    current = new List<double?> { null, null };
                    ranges.Add(mnemonic, current);
                }

                var update = reader.GetChannelIndexRange(i);

                if (!current[0].HasValue || !update.StartsAfter(current[0].Value, increasing))
                    current[0] = update.Start;
                if (!current[1].HasValue || !update.EndsBefore(current[1].Value, increasing))
                    current[1] = update.End;
            }
        }

        private void UpdateIndexRange(EtpUri uri, Log entity, Dictionary<string, List<double?>> ranges, IEnumerable<string> mnemonics, bool isTimeLog, string indexUnit)
        {
            var collection = GetCollection();
            var mongoUpdate = new MongoDbUpdate<Log>(GetCollection(), null);
            var filter = MongoDbUtility.GetEntityFilter<Log>(uri);
            UpdateDefinition<Log> logIndexUpdate = null;

            foreach (var mnemonic in mnemonics)
            {
                var curve = entity.LogCurveInfo.FirstOrDefault(c => c.Uid.EqualsIgnoreCase(mnemonic));
                if (curve == null)
                    continue;

                var filters = new List<FilterDefinition<Log>>();
                filters.Add(filter);
                filters.Add(MongoDbUtility.BuildFilter<Log>("LogCurveInfo.Uid", curve.Uid));
                var curveFilter = Builders<Log>.Filter.And(filters);

                var updateBuilder = Builders<Log>.Update;
                UpdateDefinition<Log> updates = null;

                var range = ranges[mnemonic];
                var isIndexCurve = mnemonic == entity.IndexCurve.Value;
                if (isTimeLog)
                {
                    if (range[0].HasValue)
                    {
                        var minDate = DateTimeOffset.FromUnixTimeSeconds((long)range[0].Value).ToString("o");
                        updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MinDateTimeIndex", minDate);
                        if (isIndexCurve)
                        {
                            logIndexUpdate = MongoDbUtility.BuildUpdate(logIndexUpdate, "StartDateTimeIndex", minDate);
                        }
                    }
                    if (range[1].HasValue)
                    {
                        var maxDate = DateTimeOffset.FromUnixTimeSeconds((long)range[1].Value).ToString("o");
                        updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxDateTimeIndex", maxDate);
                        if (isIndexCurve)
                        {
                            logIndexUpdate = MongoDbUtility.BuildUpdate(logIndexUpdate, "EndDateTimeIndex", maxDate);
                        }
                    }
                }
                else
                {
                    if (range[0].HasValue)
                    {
                        curve.MinIndex = UpdateGenericMeasure(curve.MinIndex, range[0].Value, indexUnit);
                        updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MinIndex", curve.MinIndex);
                        if (isIndexCurve)
                        {
                            logIndexUpdate = MongoDbUtility.BuildUpdate(logIndexUpdate, "StartIndex", curve.MinIndex);
                        }
                    }

                    if (range[1].HasValue)
                    {
                        curve.MaxIndex = UpdateGenericMeasure(curve.MaxIndex, range[1].Value, indexUnit);
                        updates = MongoDbUtility.BuildUpdate(updates, "LogCurveInfo.$.MaxIndex", curve.MaxIndex);
                        if (isIndexCurve)
                        {
                            logIndexUpdate = MongoDbUtility.BuildUpdate(logIndexUpdate, "EndIndex", curve.MaxIndex);
                        }
                    }
                }
                if (updates != null)
                    mongoUpdate.UpdateFields(curveFilter, updates);
            }

            if (logIndexUpdate != null)
                mongoUpdate.UpdateFields(filter, logIndexUpdate);
        }
    }
}
