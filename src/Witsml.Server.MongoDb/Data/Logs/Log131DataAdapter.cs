using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ReferenceData;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for a 131 <see cref="Log" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML131.Log}" />
    /// <seealso cref="PDS.Witsml.Server.Configuration.IWitsml131Configuration" />
    [Export(typeof(IWitsml131Configuration))]
    [Export(typeof(IWitsmlDataAdapter<Log>))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log131DataAdapter : MongoDbDataAdapter<Log>, IWitsml131Configuration
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

            var fields = (OptionsIn.ReturnElements.IdOnly.Equals(returnElements))
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : null;

            var ignored = new List<string> { "startIndex", "endIndex", "startDateTimeIndex", "endDateTimeIndex", "logData" };
            var logs = QueryEntities(parser, fields, ignored);

            // Only get LogData when returnElements != "header-only" and returnElements != "id-only"
            if (!OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements) && !OptionsIn.ReturnElements.IdOnly.Equals(returnElements))
            {
                var logHeaders = GetEntities(logs.Select(x => x.GetObjectId()))
                    .ToDictionary(x => x.GetObjectId());

                logs.ForEach(l =>
                {
                    var logHeader = logHeaders[l.GetObjectId()];
                    l.LogData = QueryLogDataValues(logHeader, parser);
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
            entity.CommonData = entity.CommonData.Update();

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
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Update(Log entity)
        {
            var dataObjectId = entity.GetObjectId();
            entity.CommonData = entity.CommonData.Update();

            //Validate(Functions.UpdateInStore, entity);

            // Extract Data
            var saved = GetEntity(dataObjectId);
            var reader = ExtractDataReader(entity, saved);

            // TODO: wait for selective update to be implemented
            //UpdateEntity(entity, dataObjectId);

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
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>An instance of <see cref="Log" />.</returns>
        protected override Log Parse(string xml)
        {
            var list = WitsmlParser.Parse<LogList>(xml);
            return list.Log.FirstOrDefault();
        }

        private List<string> QueryLogDataValues(Log log, WitsmlQueryParser parser)
        {
            var range = GetLogDataSubsetRange(log, parser);
            var mnemonics = GetMnemonicList(log, parser);
            var increasing = log.Direction.GetValueOrDefault() == LogIndexDirection.increasing;

            var chunks = _channelDataChunkAdapter.GetData(log.Uid, mnemonics.First(), range, increasing);
            var records = chunks.GetRecords(range, increasing);

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

        private string[] GetMnemonicList(Log log, WitsmlQueryParser parser)
        {
            // TODO: limit mnemonics based on returnElements

            return log.LogCurveInfo
                .Select(x => x.Mnemonic)
                .ToArray();
        }

        private List<string> FormatLogData(IEnumerable<IChannelDataRecord> records, string[] mnemonics)
        {
            var logData = new List<string>();

            // TODO: limit data to requested mnemonics

            foreach (var record in records)
            {
                var values = new object[record.FieldCount];
                record.GetValues(values);
                logData.Add(string.Join(",", values));
            }

            return logData;
        }

        private ChannelDataReader ExtractDataReader(Log input, Log existing = null)
        {
            ChannelDataReader reader = null;

            if (existing == null)
            {
                reader = input.GetReader();
                input.LogData = null;
                return reader;
            }

            var logData = existing.LogData;
            existing.LogData = input.LogData;
            input.LogData = null;

            reader = existing.GetReader();
            existing.LogData = logData;

            return reader;
        }
    }
}
