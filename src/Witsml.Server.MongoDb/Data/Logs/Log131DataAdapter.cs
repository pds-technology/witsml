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
using PDS.Witsml.Server.Providers;

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
                var logHeaders = GetEntities(logs.Select(x => x.GetObjectId()))
                    .ToDictionary(x => x.GetObjectId());

                logs.ForEach(l =>
                {
                    var logHeader = logHeaders[l.GetObjectId()];
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
            entity.CommonData = entity.CommonData.Update(true);

            //Validate(Functions.AddToStore, entity);

            // Extract Data
            var reader = ExtractDataReader(entity);

            InsertEntity(entity);

            // Add ChannelDataChunks
            _channelDataChunkAdapter.Add(reader);

            return new WitsmlResult(ErrorCodes.Success, entity.Uid);
        }

        /// <summary>
        /// Updates the specified <see cref="Log"/> instance in the store.
        /// </summary>
        /// <param name="entity">The <see cref="Log"/> instance.</param>
        /// <param name="parser">The update parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(Log entity, WitsmlQueryParser parser)
        {
            var dataObjectId = entity.GetObjectId();
            entity.CommonData = entity.CommonData.Update();

            //Validate(Functions.UpdateInStore, entity);

            // Extract Data
            var saved = GetEntity(dataObjectId);
            var reader = ExtractDataReader(entity, saved);

            UpdateEntity(parser, dataObjectId);

            // Merge ChannelDataChunks
            _channelDataChunkAdapter.Merge(reader);

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
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(Log entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.Uid) && Exists(entity.GetObjectId()))
            {
                Logger.DebugFormat("Update Log with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);
                return Update(entity, null);
            }
            else
            {
                Logger.DebugFormat("Add Log with uid '{0}' and name '{1}'.", entity.Uid, entity.Name);
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
            ChannelDataReader reader = null;

            if (existing == null)
            {
                reader = entity.GetReader();
                entity.LogData = null;
                return reader;
            }

            var logData = existing.LogData;
            existing.LogData = entity.LogData;
            entity.LogData = null;

            reader = existing.GetReader();
            existing.LogData = logData;

            return reader;
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(Log log, LogCurveInfo curve)
        {
            var uri = curve.GetUri(log);

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
                Indexes = new List<IndexMetadataRecord>(),
            };
        }
    }
}
