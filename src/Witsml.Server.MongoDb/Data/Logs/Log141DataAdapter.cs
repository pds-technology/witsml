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
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Properties;
using PDS.Witsml.Server.Providers;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 141 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML141.Log}" />
    /// <seealso cref="PDS.Witsml.Server.Data.Channels.IChannelDataProvider" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml141Configuration" />
    [Export(typeof(IWitsml141Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [Export(typeof(IEtpDataAdapter<Log>))]
    [Export141(ObjectTypes.Log, typeof(IEtpDataAdapter))]
    [Export141(ObjectTypes.Log, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataAdapter : MongoDbDataAdapter<Log>, IChannelDataProvider, IWitsml141Configuration
    {
        private static readonly int MaxDataNodes = Settings.Default.MaxDataNodes;
        private static readonly int MaxDataPoints = Settings.Default.MaxDataPoints;

        private readonly ChannelDataChunkAdapter _channelDataChunkAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Log141DataAdapter"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="channelDataChunkAdapter">The channels data adapter.</param>
        [ImportingConstructor]
        public Log141DataAdapter(IDatabaseProvider databaseProvider, ChannelDataChunkAdapter channelDataChunkAdapter) : base(databaseProvider, ObjectNames.Log141)
        {
            _channelDataChunkAdapter = channelDataChunkAdapter;
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
                var logHeaders = GetEntities(logs.Select(x => x.GetObjectId()))
                    .ToDictionary(x => x.GetObjectId());

                logs.ForEach(l =>
                {
                    var logHeader = logHeaders[l.GetObjectId()];
                    var mnemonics = GetMnemonicList(logHeader, parser);

                    l.LogData = new List<LogData>() { QueryLogDataValues(logHeader, parser, mnemonics) };

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
            entity.CommonData = entity.CommonData.Update(true);

            Validate(Functions.AddToStore, entity);

            // Extract Data
            var readers = ExtractDataReaders(entity);

            InsertEntity(entity);

            // Add ChannelDataChunks
            _channelDataChunkAdapter.Add(readers.FirstOrDefault());

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
            var entity = Parse(parser.Context.Xml);
            var dataObjectId = entity.GetObjectId();

            //entity.CommonData = entity.CommonData.Update();
            //Validate(Functions.UpdateInStore, entity);

            // Extract Data
            var saved = GetEntity(dataObjectId);
            var readers = ExtractDataReaders(entity, saved);
            var ignored = new[] { "logData" };

            UpdateEntity(parser, dataObjectId, ignored);

            // Merge ChannelDataChunks
            foreach (var reader in readers)
            {
                _channelDataChunkAdapter.Merge(reader);
            }

            // TODO: Fix later
            //UpdateLogHeaderRanges(entity);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The parser that specifies the object.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Delete(WitsmlQueryParser parser)
        {
            var entity = Parse(parser.Context.Xml);
            var dataObjectId = entity.GetObjectId();

            DeleteEntity(dataObjectId);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Gets the channel metadata for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(EtpUri uri)
        {
            var entity = GetEntity(uri.ToDataObjectId());
            var metadata = new List<ChannelMetadataRecord>();
            var index = 0;

            if (entity.LogCurveInfo == null || !entity.LogCurveInfo.Any())
                return metadata;

            metadata.AddRange(entity.LogCurveInfo.Select(x =>
            {
                var channel = ToChannelMetadataRecord(entity, x);
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
            var entity = GetEntity(uri.ToDataObjectId());
            var mnemonics = entity.LogCurveInfo.Select(x => x.Mnemonic.Value);
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
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="parser">The input parser.</param>
        /// <returns>A WITSML result.</returns>
        public override WitsmlResult Put(WitsmlQueryParser parser)
        {
            var entity = Parse(parser.Context.Xml);

            if (!string.IsNullOrWhiteSpace(entity.Uid) && Exists(entity.GetObjectId()))
            {
                return Update(parser);
            }
            else
            {
                return Add(entity);
            }
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

        private LogData QueryLogDataValues(Log log, WitsmlQueryParser parser, IDictionary<int, string> mnemonics)
        {
            var range = GetLogDataSubsetRange(log, parser);
            var units = GetUnitList(log, parser, mnemonics.Keys.ToArray());
            var increasing = log.Direction.GetValueOrDefault() == LogIndexDirection.increasing;
            var records = GetChannelData(log.GetUri(), mnemonics[0], range, increasing);

            return FormatLogData(records, mnemonics, units);
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
            var allMnemonics = log.LogCurveInfo.Select(x => x.Mnemonic.Value).ToArray();
            var queryMnemonics = GetLogCurveInfoMnemonics(parser).ToArray();

            return ComputeMnemonicIndexes(allMnemonics, queryMnemonics, parser.ReturnElements());
        }

        private string[] GetUnitList(Log log, WitsmlQueryParser parser, int[] slices)
        {
            // Get a list of all of the units
            var allUnits = log.LogCurveInfo
                .Select(x => x.Unit)
                .ToArray()
                .Select((unit, index) => new { Unit = unit, Index = index });

            // Limit units based on slices previously calculated for mnemonics
            return allUnits
                .Where(x => slices.Contains(x.Index))
                .Select(x => x.Unit)
                .ToArray();
        }

        private LogData FormatLogData(IEnumerable<IChannelDataRecord> records, IDictionary<int, string> mnemonics, string[] units)
        {
            var logData = new LogData()
            {
                MnemonicList = string.Join(",", mnemonics.Values),
                UnitList = string.Join(",", units),
                Data = new List<string>()
            };

            var slices = mnemonics.Keys.ToArray();
            foreach (var record in records)
            {
                var values = new object[record.FieldCount];
                record.GetValues(values);

                // Limit data to requested mnemonics
                if (slices.Any())
                {
                    values = values
                        .Where((x, i) => slices.Contains(i))
                        .ToArray();
                }

                logData.Data.Add(string.Join(",", values));
            }

            return logData;
        }

        private void FormatLogHeader(Log log, IDictionary<int, string> mnemonics, string returnElements)
        {
            // Remove LogCurveInfos from the Log header if slicing by column
            if (log.LogCurveInfo != null && !OptionsIn.ReturnElements.All.Equals(returnElements))
            {
                log.LogCurveInfo.RemoveAll(x => !mnemonics.Values.Contains(x.Mnemonic.Value));
            }
        }

        private IEnumerable<ChannelDataReader> ExtractDataReaders(Log entity, Log existing = null)
        {
            IEnumerable<ChannelDataReader> readers = null;

            if (existing == null)
            {
                readers = entity.GetReaders().ToList();
                entity.LogData = null;
                return readers;
            }

            var logData = existing.LogData;
            existing.LogData = entity.LogData;
            entity.LogData = null;

            readers = existing.GetReaders().ToList();
            existing.LogData = logData;

            return readers;
        }

        private List<LogData> EmptyLogData(LogData logData)
        {
            return new List<LogData>()
            {
                new LogData()
                {
                    MnemonicList = logData != null ? logData.MnemonicList : null,
                    UnitList = logData != null ? logData.UnitList : null
                }
            };
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(Log log, LogCurveInfo curve)
        {
            var uri = curve.GetUri(log);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = curve.TypeLogData.GetValueOrDefault(LogDataType.@double).ToString().Replace("@", string.Empty),
                Description = curve.CurveDescription ?? curve.Mnemonic.Value,
                Mnemonic = curve.Mnemonic.Value,
                Uom = curve.Unit,
                MeasureClass = curve.ClassWitsml == null ? ObjectTypes.Unknown : curve.ClassWitsml,
                Source = curve.DataSource ?? ObjectTypes.Unknown,
                Uuid = curve.Mnemonic.Value,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                Indexes = new List<IndexMetadataRecord>(),
            };
        }

        //private LogData QueryLogDataValues(Log log, WitsmlQueryParser parser)
        //{
        //    var returnElements = parser.ReturnElements();
        //    var logDataElement = parser.Property("logData");

        //    if (logDataElement == null &&
        //        !OptionsIn.ReturnElements.All.Equals(returnElements) &&
        //        !OptionsIn.ReturnElements.DataOnly.Equals(returnElements))
        //    {
        //        return null;
        //    }

        //    Tuple<double?, double?> range;
        //    var increasing = log.Direction != LogIndexDirection.decreasing;
        //    var isTimeLog = log.IndexType == LogIndexType.datetime;

        //    if (isTimeLog)
        //    {
        //        var startIndex = ToNullableUnixSeconds(parser.PropertyValue("startDateTimeIndex"));
        //        var endIndex = ToNullableUnixSeconds(parser.PropertyValue("endDateTimeIndex"));
        //        range = new Tuple<double?, double?>(startIndex, endIndex);
        //    }
        //    else
        //    {
        //        var startIndex = ToNullableDouble(parser.PropertyValue("startIndex"));
        //        var endIndex = ToNullableDouble(parser.PropertyValue("endIndex"));
        //        range = new Tuple<double?, double?>(startIndex, endIndex);
        //    }

        //    var mnemonics = log.LogCurveInfo.Select(x => x.Mnemonic.Value).ToList();

        //    if (logDataElement != null)
        //    {
        //        var mnemonicElement = logDataElement.Elements().FirstOrDefault(e => e.Name.LocalName == "mnemonicList");
        //        if (mnemonicElement != null)
        //        {
        //            var target = logDataElement.Elements().FirstOrDefault(e => e.Name.LocalName == "mnemonicList").Value.Split(',');
        //            mnemonics = target.Where(m => mnemonics.Contains(m)).ToList();
        //        }
        //    }

        //    return _channelDataChunkAdapter.GetLogData(log.Uid, mnemonics, range, increasing);
        //}

        //private double? ToNullableDouble(string doubleStr)
        //{
        //    return string.IsNullOrEmpty(doubleStr) ? (double?)null : double.Parse(doubleStr);
        //}

        //private double? ToNullableUnixSeconds(string dateTimeStr)
        //{
        //    return string.IsNullOrEmpty(dateTimeStr)? (double?)null: DateTimeOffset.Parse(dateTimeStr).ToUnixTimeSeconds();
        //}

        #region UpdateLogHeaderRanges Code
        //private void UpdateLogData(Log log, List<LogDataValues> logDataChanges)
        //{
        //    var database = DatabaseProvider.GetDatabase();
        //    var collection = database.GetCollection<LogDataValues>(_DbLogDataValuesDocumentName);
        //    var changeIndexes = logDataChanges.Select(x => x.Index);


        //    List<LogDataValues> newLogDataChanges;
        //    List<LogDataValues> updateLogDataChanges;

        //    // Pull existing indexes but only for those that are in our change list.
        //    var existingIndexes = collection.AsQueryable()
        //        .Where(x => x.UidLog == log.Uid && changeIndexes.Contains(x.Index))
        //        .Select(x => x.Index)
        //        .ToList();

        //    newLogDataChanges = logDataChanges.Where(ldc => !existingIndexes.Contains(ldc.Index)).ToList();
        //    updateLogDataChanges = logDataChanges.Where(ldc => existingIndexes.Contains(ldc.Index)).ToList();


        //    if (newLogDataChanges.Any())
        //    {
        //        //CreateLogDataValues(log, newLogDataChanges);
        //    }

        //    if (updateLogDataChanges.Any())
        //    {
        //        updateLogDataChanges.ForEach(ldc =>
        //        {
        //            var query = collection.AsQueryable()
        //                .Where(x => x.UidLog == log.Uid && x.Index == ldc.Index);

        //            var existingLogDataValues = query.FirstOrDefault();
        //            var updateFilter = Builders<LogDataValues>.Filter.Eq("Uid", existingLogDataValues.Uid);
        //            var update = Builders<LogDataValues>.Update.Set("Data", ldc.Data);
        //            collection.UpdateOne(updateFilter, update);
        //        });
        //    }

        //    // Update index range references within the log
        //    UpdateLogHeaderRanges(log);
        //}

        // TODO: Update later (the right way)
        //private void UpdateLogHeaderRanges(Log log)
        //{
        //    var database = DatabaseProvider.GetDatabase();
        //    var collection = database.GetCollection<Log>(_DbDocumentName);
        //    var updateFilter = Builders<Log>.Filter.Eq("Uid", log.Uid);

        //    // Find the Log that needs to be updated
        //    var dbLog = GetEntity(log.Uid, _DbDocumentName);

        //    // Get the min and max index range for this log.
        //    double startIndex;
        //    double endIndex;
        //    GetLogDataIndexRange(log, out startIndex, out endIndex);
        //    dbLog.StartIndex = UpdateGenericMeasure(dbLog.StartIndex, startIndex);
        //    dbLog.EndIndex = UpdateGenericMeasure(dbLog.EndIndex, endIndex);
        //    var update = Builders<Log>.Update.Set("StartIndex", dbLog.StartIndex);
        //    update.Set("EndIndex", dbLog.EndIndex);


        //    if (dbLog.LogCurveInfo != null)
        //    {
        //        dbLog.LogCurveInfo.ForEach(x =>
        //        {
        //            x.MinIndex = UpdateGenericMeasure(x.MinIndex, startIndex);
        //            x.MaxIndex = UpdateGenericMeasure(x.MaxIndex, endIndex);
        //        });
        //        update.Set("LogCurveInfo", dbLog.LogCurveInfo);
        //    }
        //    collection.UpdateOne(updateFilter, update);
        //}

        //private void GetLogDataIndexRange(Log log, out double startIndex, out double endIndex)
        //{
        //    var database = DatabaseProvider.GetDatabase();
        //    var collection = database.GetCollection<LogDataValues>(_DbLogDataValuesDocumentName);

        //    // Fetch the LogDataValue record for the Log with the smallest index
        //    var min = collection.AsQueryable()
        //        .Where(x => x.UidLog == log.Uid)
        //        .OrderBy(x => x.Index)
        //        .Select(x => x.Index)
        //        .Take(1).FirstOrDefault();

        //    var max = collection.AsQueryable()
        //        .Where(x => x.UidLog == log.Uid)
        //        .OrderByDescending(x => x.Index)
        //        .Select(x => x.Index)
        //        .Take(1).FirstOrDefault();

        //    // Initialize
        //    startIndex = min;
        //    endIndex = max;
        //}

        private GenericMeasure UpdateGenericMeasure(GenericMeasure gmObject, double gmValue)
        {
            if (gmObject == null)
            {
                gmObject = new GenericMeasure();
            }
            gmObject.Value = gmValue;

            return gmObject;
        }
        #endregion
    }
}
