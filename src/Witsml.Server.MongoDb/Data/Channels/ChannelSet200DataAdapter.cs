using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Providers;

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
            var entity = GetEntity(uri.ToDataObjectId());
            var metadata = new List<ChannelMetadataRecord>();
            var index = 0;

            if (entity.Channel == null || !entity.Channel.Any())
                return metadata;

            metadata.AddRange(entity.Index.Select(x =>
            {
                var channel = ToChannelMetadataRecord(entity, x);
                channel.ChannelId = index++;
                return channel;
            }));

            metadata.AddRange(entity.Channel.Select(x =>
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
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override WitsmlResult Put(ChannelSet entity)
        {
            var dataObjectId = entity.GetObjectId();

            if (!string.IsNullOrWhiteSpace(entity.Uuid) && Exists(dataObjectId))
            {
                entity.Citation = entity.Citation.Update();

                Validate(Functions.PutObject, entity);

                // Extract Data
                var saved = GetEntity(dataObjectId);
                var reader = ExtractDataReader(entity, saved);
                var parser = CreateQueryParser(Functions.PutObject, entity);

                UpdateEntity(parser, dataObjectId);

                // Merge ChannelDataChunks
                _channelDataChunkAdapter.Merge(reader);
            }
            else
            {
                entity.Uuid = NewUid(entity.Uuid);
                entity.Citation = entity.Citation.Update(true);

                Validate(Functions.PutObject, entity);

                // Extract Data
                var reader = ExtractDataReader(entity);

                InsertEntity(entity);

                // Add ChannelDataChunks
                _channelDataChunkAdapter.Add(reader);
            }

            return new WitsmlResult(ErrorCodes.Success, entity.Uuid);
        }

        private ChannelDataReader ExtractDataReader(ChannelSet entity, ChannelSet existing = null)
        {
            // TODO: Handle: if (!string.IsNullOrEmpty(entity.Data.FileUri))
            // return null;

            ChannelDataReader reader = null;

            if (existing == null)
            {
                reader = entity.GetReader();
                entity.Data = null;
                return reader;
            }

            var channelData = existing.Data;
            existing.Data = entity.Data;
            entity.Data = null;

            reader = existing.GetReader();
            existing.Data = channelData;

            return reader;
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelSet entity, ChannelIndex index)
        {
            var uri = index.GetUri(entity);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = EtpDataType.@double.ToString().Replace("@", string.Empty),
                Description = index.Mnemonic,
                Mnemonic = index.Mnemonic,
                Uom = index.Uom,
                MeasureClass = ObjectTypes.Unknown,
                Source = ObjectTypes.Unknown,
                Uuid = index.Mnemonic,
                Status = ChannelStatuses.Active,
                ChannelAxes = new List<ChannelAxis>(),
                Indexes = new List<IndexMetadataRecord>()
            };
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelSet entity, Channel channel)
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
                Indexes = new List<IndexMetadataRecord>()
            };
        }
    }
}
