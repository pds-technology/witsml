//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Datatypes.Object;
using MongoDB.Driver;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Channels;
using PDS.WITSMLstudio.Store.Providers.Store;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Trajectory" />
    /// </summary>
    [Export200(ObjectTypes.Trajectory, typeof(IChannelDataProvider))]
    public partial class Trajectory200DataAdapter : IChannelDataProvider
    {
        /// <summary>
        /// Gets the channel metadata.
        /// </summary>
        /// <param name="uris">The uris to retrieve metadata for</param>
        /// <returns></returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(params EtpUri[] uris)
        {
            var metadata = new List<ChannelMetadataRecord>();
            var entities = GetChannelsByUris(uris);

            foreach (var entity in entities)
            {
                Logger.Debug($"Getting channel metadata for URI: {entity.GetUri()}");
                metadata.AddRange(GetChannelMetadataForAnEntity(entity, uris));
            }

            return metadata;
        }

        /// <summary>
        /// Gets the channel data records for the specified URI and range.
        /// </summary>
        /// <param name="uri">The URI of the data channel</param>
        /// <param name="range">The specified range of the channel data</param>
        /// <returns></returns>
        public IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range)
        {
            return new List<IChannelDataRecord>();
        }

        /// <summary>
        /// Gets the channel data records for the specified URI and range.
        /// </summary>
        /// <param name="uri">The uri of the data object</param>
        /// <param name="range">The range of the channel data</param>
        /// <param name="mnemonics">The channel mnemonics</param>
        /// <param name="requestLatestValues">true if only the latest values are requested, false otherwise</param>
        /// <param name="optimizeStart"></param>
        /// <returns></returns>
        public List<List<List<object>>> GetChannelData(EtpUri uri, Range<double?> range, List<string> mnemonics, int? requestLatestValues, bool optimizeStart = false)
        {
            return new List<List<List<object>>>();
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The URI of the data object</param>
        /// <param name="reader">A reader for the channel data</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
        }

        private List<Trajectory> GetChannelsByUris(params EtpUri[] uris)
        {
            if (uris.Any(u => u.IsBaseUri))
                return GetAll(null);

            var channelUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Trajectory);
            var wellboreUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Wellbore);
            var wellUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Well);
            if (wellUris.Any())
            {
                var wellboreFilters = wellUris.Select(wellUri => MongoDbUtility.BuildFilter<Wellbore>("Well.Uuid", wellUri.ObjectId)).ToList();
                var wellbores = GetCollection<Wellbore>(ObjectNames.Wellbore200)
                    .Find(Builders<Wellbore>.Filter.Or(wellboreFilters)).ToList();
                wellboreUris.AddRange(wellbores.Select(w => w.GetUri()).Where(u => !wellboreUris.Contains(u)));
            }

            var channelFilters = wellboreUris.Select(wellboreUri => MongoDbUtility.BuildFilter<Trajectory>("Wellbore.Uuid", wellboreUri.ObjectId)).ToList();
            channelFilters.AddRange(channelUris.Select(u => MongoDbUtility.GetEntityFilter<Trajectory>(u, IdPropertyName)));

            return channelFilters.Any() ? GetCollection().Find(Builders<Trajectory>.Filter.Or(channelFilters)).ToList() : new List<Trajectory>();
        }

        private IList<ChannelMetadataRecord> GetChannelMetadataForAnEntity(Trajectory entity, params EtpUri[] uris)
        {
            var metadata = new List<ChannelMetadataRecord>();

            // Get Index Metadata
            var indexMetadata = ToIndexMetadataRecord(entity);

            // Get Channel Metadata
            var channel = ToChannelMetadataRecord(entity, indexMetadata);
            metadata.Add(channel);

            return metadata;
        }

        private IndexMetadataRecord ToIndexMetadataRecord(Trajectory entity, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = entity.GetUri().Append("md"),
                Mnemonic = "MD",
                Description = LogIndexType.measureddepth.GetName(),
                Uom = Units.GetUnit(entity.MDMin?.Uom.ToString()),
                Scale = scale,
                IndexType = ChannelIndexTypes.Depth,
                Direction = IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(Trajectory entity, IndexMetadataRecord indexMetadata)
        {
            var uri = entity.GetUri();
            var dataObject = new DataObject();
            var lastChanged = entity.Citation.LastUpdate.ToUnixTimeMicroseconds().GetValueOrDefault();

            StoreStoreProvider.SetDataObject(dataObject, entity, uri, entity.Citation.Title, 0, lastChanged);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = EtpDataType.bytes.GetName(),
                Description = entity.Citation.Description ?? entity.Citation.Title,
                ChannelName = entity.Citation.Title,
                Uom = Units.GetUnit(entity.MDMin?.Uom.ToString()),
                MeasureClass = ObjectTypes.Unknown,
                Source = ObjectTypes.Unknown,
                Uuid = entity.Uuid,
                DomainObject = dataObject,
                Status = GetStatus(entity.GrowingStatus),
                StartIndex = entity.MDMin?.Value.IndexToScale(indexMetadata.Scale),
                EndIndex = entity.MDMax?.Value.IndexToScale(indexMetadata.Scale),
                Indexes = new List<IndexMetadataRecord>()
                {
                    indexMetadata
                },
                CustomData = new Dictionary<string, DataValue>()
            };
        }

        private ChannelStatuses GetStatus(ChannelStatus? entityGrowingStatus)
        {
            switch (entityGrowingStatus)
            {
                case ChannelStatus.active:
                    return ChannelStatuses.Active;
                case ChannelStatus.inactive:
                    return ChannelStatuses.Inactive;
                default:
                    return ChannelStatuses.Closed;
            }
        }
    }
}
