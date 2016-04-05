using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSet" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Energistics.DataAccess.WITSML200.ChannelSet}" />
    /// <seealso cref="PDS.Witsml.Server.Data.Channels.IChannelDataProvider" />
    [Export(typeof(IEtpDataAdapter<ChannelSet>))]
    [Export200(ObjectTypes.ChannelSet, typeof(IEtpDataAdapter))]
    [Export200(ObjectTypes.ChannelSet, typeof(IChannelDataProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class ChannelSet200DataAdapter : MongoDbDataAdapter<ChannelSet>, IChannelDataProvider
    {
        private readonly ChannelDataChunkAdapter _channelDataChunkAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSet200DataAdapter" /> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="channelDataChunkAdapter">The channel data chunk adapter.</param>
        [ImportingConstructor]
        public ChannelSet200DataAdapter(IDatabaseProvider databaseProvider, ChannelDataChunkAdapter channelDataChunkAdapter) : base(databaseProvider, ObjectNames.ChannelSet200, ObjectTypes.Uuid)
        {
            _channelDataChunkAdapter = channelDataChunkAdapter;
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

            if (entity.Channel == null || !entity.Channel.Any())
                return metadata;

            var indexMetadata = entity.Index
                .Select(x => ToIndexMetadataRecord(entity, x))
                .ToList();

            metadata.AddRange(entity.Channel.Select(x =>
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
            var indexChannel = entity.Index.FirstOrDefault();
            var increasing = indexChannel.Direction.GetValueOrDefault() == IndexDirection.increasing;
            var chunks = _channelDataChunkAdapter.GetData(uri, indexChannel.Mnemonic, range, increasing);
            return chunks.GetRecords(range, increasing);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<ChannelSet> GetAll(EtpUri? parentUri = null)
        {
            var query = GetQuery().AsQueryable();

            //if (parentUri != null)
            //{
            //    var uidLog = parentUri.Value.ObjectId;
            //    query = query.Where(x => x.Log.Uuid == uidLog);
            //}

            return query
                .OrderBy(x => x.Citation.Title)
                .ToList();
        }

        /// <summary>
        /// Adds <see cref="ChannelSet"/> to the data store.
        /// </summary>
        /// <param name="entity">The <see cref="ChannelSet"/> to be added.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public override WitsmlResult Add(ChannelSet entity)
        {
            entity.Uuid = NewUid(entity.Uuid);
            entity.Channel.ForEach(c => c.Uuid = NewUid(c.Uuid));
            entity.Citation = entity.Citation.Create();
            Logger.DebugFormat("Adding ChannelSet with uid '{0}' and name '{1}'", entity.Uuid, entity.Citation.Title);

            Validate(Functions.AddToStore, entity);
            Logger.DebugFormat("Validated ChannelSet with uid '{0}' and name '{1}' for Add", entity.Uuid, entity.Citation.Title);

            // Extract Data
            var reader = ExtractDataReader(entity);

            InsertEntity(entity);

            if (reader != null)
            {
                var increasing = entity.Index.FirstOrDefault().Direction == IndexDirection.increasing;
                var allMnemonics = reader.Indices.Select(i => i.Mnemonic).Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(entity);
                var indexCurve = entity.Index[0];

                TimeSpan? offset = null;
                var isTimeLog = indexCurve.IndexType == ChannelIndexType.datetime || indexCurve.IndexType == ChannelIndexType.elapsedtime;
                if (isTimeLog)
                    offset = reader.GetChannelIndexRange(0).Offset;

                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, increasing);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Add(reader);

                // Update index range
                UpdateIndexRange(entity.GetUri(), entity, ranges, allMnemonics, offset);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
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

            var ignored = new[] { "Data" };
            UpdateEntity(parser, uri, ignored);

            // Extract Data
            var entity = Parse(parser.Context.Xml);
            var reader = ExtractDataReader(entity, GetEntity(uri));

            UpdateChannelDataAndIndex(uri, reader);

            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            UpdateChannelDataAndIndex(uri, reader);
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

        internal ChannelDataReader ExtractDataReader(ChannelSet entity, ChannelSet existing = null)
        {
            // TODO: Handle: if (!string.IsNullOrEmpty(entity.Data.FileUri))
            // return null;

            if (existing == null)
            {
                var reader = entity.GetReader();
                entity.Data = null;
                return reader;
            }

            existing.Data = entity.Data;
            return existing.GetReader();
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelSet entity, Channel channel, IList<IndexMetadataRecord> indexMetadata)
        {
            var uri = channel.GetUri(entity);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = channel.DataType.GetValueOrDefault(EtpDataType.@double).ToString().Replace("@", string.Empty),
                Description = channel.Citation != null ? channel.Citation.Description ?? channel.Mnemonic : channel.Mnemonic,
                Mnemonic = channel.Mnemonic,
                Uom = channel.UoM,
                MeasureClass = channel.CurveClass ?? ObjectTypes.Unknown,
                Source = channel.Source ?? ObjectTypes.Unknown,
                Uuid = channel.Mnemonic,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                Indexes = indexMetadata
            };
        }

        private IndexMetadataRecord ToIndexMetadataRecord(ChannelSet entity, ChannelIndex indexChannel, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = indexChannel.GetUri(entity),
                Mnemonic = indexChannel.Mnemonic,
                Description = indexChannel.Mnemonic,
                Uom = indexChannel.Uom,
                Scale = scale,
                IndexType = indexChannel.IndexType == ChannelIndexType.datetime || indexChannel.IndexType == ChannelIndexType.elapsedtime
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = indexChannel.Direction == IndexDirection.decreasing
                    ? IndexDirections.Decreasing
                    : IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private void UpdateChannelDataAndIndex(EtpUri uri, ChannelDataReader reader)
        {
            // Get Updated ChannelSet
            var current = GetEntity(uri);

            // Merge ChannelDataChunks
            if (reader != null)
            {
                var increasing = current.Index.FirstOrDefault().Direction == IndexDirection.increasing;
                var allMnemonics = reader.Indices.Select(i => i.Mnemonic).Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(current);
                var indexCurve = current.Index[0];

                TimeSpan? offset = null;
                var isTimeLog = indexCurve.IndexType == ChannelIndexType.datetime || indexCurve.IndexType == ChannelIndexType.elapsedtime;
                if (isTimeLog)
                    offset = reader.GetChannelIndexRange(0).Offset;

                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, increasing);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Merge(reader);

                // Update index range
                UpdateIndexRange(uri, current, ranges, allMnemonics, offset);
            }
        }

        private Dictionary<string, List<double?>> GetCurrentIndexRange(ChannelSet entity)
        {
            var ranges = new Dictionary<string, List<double?>>();
            var index = entity.Index.FirstOrDefault();
            AddIndexRange(index.Mnemonic, entity.StartIndex, entity.EndIndex, ranges);

            foreach (var channel in entity.Channel)
            {
                AddIndexRange(channel.Mnemonic, channel.StartIndex, channel.EndIndex, ranges);
            }

            return ranges;
        }

        private void AddIndexRange(string mnemonic, AbstractIndexValue start, AbstractIndexValue end, Dictionary<string, List<double?>> ranges)
        {
            var range = new List<double?> { null, null };
            if (start is TimeIndexValue)
            {
                var startTime = start as TimeIndexValue;
                if (startTime != null && !string.IsNullOrEmpty(startTime.Time))
                    range[0] = DateTimeOffset.Parse(startTime.Time).ToUnixTimeSeconds();
                var endTime = end as TimeIndexValue;
                if (endTime != null && !string.IsNullOrEmpty(endTime.Time))
                    range[1] = DateTimeOffset.Parse(endTime.Time).ToUnixTimeSeconds();
            }
            else if (start is DepthIndexValue)
            {
                var startDepth = start as DepthIndexValue;
                if (startDepth != null && startDepth.Depth.HasValue)
                    range[0] = startDepth.Depth.Value;
                var endDepth = end as DepthIndexValue;
                if (endDepth != null && endDepth.Depth.HasValue)
                    range[1] = endDepth.Depth.Value;
            }
            else
            {
                var startPass = start as PassIndexedDepth;
                if (startPass != null && startPass.Depth.HasValue)
                    range[0] = startPass.Depth.Value;
                var endPass = end as PassIndexedDepth;
                if (endPass != null && endPass.Depth.HasValue)
                    range[1] = endPass.Depth.Value;
            }
            ranges.Add(mnemonic, range);
        }

        private AbstractIndexValue UpdateIndexValue(ChannelIndexType? indexType, AbstractIndexValue current, double value, TimeSpan? offset)
        {
            AbstractIndexValue indexValue = null;

            if (indexType == ChannelIndexType.datetime || indexType == ChannelIndexType.elapsedtime)
            {
                if (current == null)
                    indexValue = new TimeIndexValue();
                else
                    indexValue = current;
                ((TimeIndexValue)indexValue).Time = MongoDbUtility.ToOffsetTime(DateTimeOffset.FromUnixTimeSeconds((long)value), offset).Value.ToString("o");
            }
            else if (indexType == ChannelIndexType.passindexeddepth)
            {
                if (current == null)
                    indexValue = new PassIndexedDepth();
                else
                    indexValue = current;
                ((PassIndexedDepth)indexValue).Depth = (float)value;
            }
            else
            {
                if (current == null)
                    indexValue = new DepthIndexValue();
                else
                    indexValue = current;
                ((DepthIndexValue)indexValue).Depth = (float)value;
            }

            return indexValue;
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

        private void UpdateIndexRange(EtpUri uri, ChannelSet entity, Dictionary<string, List<double?>> ranges, IEnumerable<string> mnemonics, TimeSpan? offset)
        {
            var collection = GetCollection();
            var mongoUpdate = new MongoDbUpdate<ChannelSet>(GetCollection(), null);

            var idField = MongoDbUtility.LookUpIdField(typeof(ChannelSet), "Uuid");

            var filter = MongoDbUtility.GetEntityFilter<ChannelSet>(uri, idField);
            UpdateDefinition<ChannelSet> channelIndexUpdate = null;

            if (entity.Citation != null)
            {
                if (entity.Citation.Creation.HasValue)
                {
                    var creationTime = MongoDbUtility.ToOffsetTime(entity.Citation.Creation.Value, offset);
                    channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "Citation.Creation", creationTime.Value.ToString("o"));
                }
                if (entity.Citation.LastUpdate.HasValue)
                {
                    var updateTime = MongoDbUtility.ToOffsetTime(entity.Citation.LastUpdate.Value, offset);
                    channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "Citation.LastUpdate", updateTime.Value.ToString("o"));
                }
            }

            var indexChannel = entity.Index.FirstOrDefault();
            var indexType = indexChannel.IndexType;
            var indexMnemonic = indexChannel.Mnemonic;
            var startIndex = entity.StartIndex;
            var range = ranges[indexMnemonic];
            if (range[0].HasValue)
            {
                var start = UpdateIndexValue(indexType, startIndex, range[0].Value, offset);
                channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "StartIndex", start);
            }
            if (range[1].HasValue)
            {
                var end = UpdateIndexValue(indexType, entity.EndIndex, range[1].Value, offset);
                channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "EndIndex", end);
            }
            if (channelIndexUpdate != null)
                mongoUpdate.UpdateFields(filter, channelIndexUpdate);

            idField = MongoDbUtility.LookUpIdField(typeof(Channel), "Uuid");

            foreach (var mnemonic in mnemonics)
            {
                var channel = entity.Channel.FirstOrDefault(c => c.Mnemonic.EqualsIgnoreCase(mnemonic));
                if (channel == null)
                    continue;

                var filters = new List<FilterDefinition<ChannelSet>>();
                filters.Add(filter);
                filters.Add(MongoDbUtility.BuildFilter<ChannelSet>("Channel." + idField, channel.Uuid));
                var channelFilter = Builders<ChannelSet>.Filter.And(filters);

                var updateBuilder = Builders<ChannelSet>.Update;
                UpdateDefinition<ChannelSet> updates = null;

                range = ranges[mnemonic];
                if (range[0].HasValue)
                {
                    var start = UpdateIndexValue(indexType, channel.StartIndex, range[0].Value, offset);
                    updates = MongoDbUtility.BuildUpdate(updates, "Channel.$.StartIndex", start);
                }
                if (range[1].HasValue)
                {
                    var end = UpdateIndexValue(indexType, channel.EndIndex, range[1].Value, offset);
                    updates = MongoDbUtility.BuildUpdate(updates, "Channel.$.EndIndex", end);
                }
                if (updates != null)
                    mongoUpdate.UpdateFields(channelFilter, updates);
            }
        }
    }
}
