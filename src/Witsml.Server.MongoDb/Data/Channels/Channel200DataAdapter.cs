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
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using Energistics.Datatypes.Object;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Providers.Store;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Channel" />
    /// </summary>
    [Export200(ObjectTypes.Channel, typeof(IChannelDataProvider))]
    public partial class Channel200DataAdapter : IChannelDataProvider
    {
        /// <summary>
        /// Gets the channels metadata.
        /// </summary>
        /// <param name="uris">The collection of URI to describe.</param>
        /// <returns>
        /// A collection of channel metadata.
        /// </returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(params EtpUri[] uris)
        {
            var metadatas = new List<ChannelMetadataRecord>();

            var channels = GetChannelsByUris(uris);
            foreach (var channel in channels)
            {
                var indexMetadata = channel.Index
                    .Select(x => ToIndexMetadataRecord(channel, x))
                    .ToList();

                metadatas.Add(ToChannelMetadataRecord(channel, indexMetadata));
            }

            return metadatas;
        }

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <returns>
        /// A collection of channel data.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <param name="mnemonics">The mnemonics to fetch channel data for.
        /// This list will be modified to contain only those mnemonics that data was returned for.</param>
        /// <param name="requestLatestValues">The total number of requested latest values.</param>
        /// <param name="optimizeStart">if set to <c>true</c> start range can be optimized.</param>
        /// <returns>
        /// A collection of channel data.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public List<List<List<object>>> GetChannelData(EtpUri uri, Range<double?> range, List<string> mnemonics, int? requestLatestValues, bool optimizeStart = false)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            throw new NotImplementedException();
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(Channel channel, IList<IndexMetadataRecord> indexMetadata)
        {
            var uri = channel.GetUri();
            var primaryIndex = indexMetadata.FirstOrDefault();
            var isTimeLog = primaryIndex != null && primaryIndex.IndexType == ChannelIndexTypes.Time;
            var channelIndex = GetIndexRange(channel);
            var dataObject = new DataObject();

            StoreStoreProvider.SetDataObject(dataObject, channel, uri, channel.Mnemonic, 0);

            return new ChannelMetadataRecord()
            {
                ChannelUri = uri,
                ContentType = uri.ContentType,
                DataType = channel.DataType.GetValueOrDefault(EtpDataType.@double).ToString().Replace("@", string.Empty),
                Description = channel.Citation != null ? channel.Citation.Description ?? channel.Mnemonic : channel.Mnemonic,
                ChannelName = channel.Mnemonic,
                Uom = Units.GetUnit(channel.Uom),
                MeasureClass = channel.CurveClass ?? ObjectTypes.Unknown,
                Source = channel.Source ?? ObjectTypes.Unknown,
                Uuid = channel.Mnemonic,
                DomainObject = dataObject,
                Status = ChannelStatuses.Active,
                StartIndex = primaryIndex == null ? null : channelIndex.Start.IndexToScale(primaryIndex.Scale, isTimeLog),
                EndIndex = primaryIndex == null ? null : channelIndex.End.IndexToScale(primaryIndex.Scale, isTimeLog),
                Indexes = indexMetadata,
                CustomData = new Dictionary<string, DataValue>()
            };
        }

        private IndexMetadataRecord ToIndexMetadataRecord(Channel entity, ChannelIndex indexChannel, int scale = 3)
        {
            return new IndexMetadataRecord()
            {
                Uri = indexChannel.GetUri(entity),
                Mnemonic = indexChannel.Mnemonic,
                Description = indexChannel.Mnemonic,
                Uom = Units.GetUnit(indexChannel.Uom),
                Scale = scale,
                IndexType = indexChannel.IsTimeIndex(true)
                    ? ChannelIndexTypes.Time
                    : ChannelIndexTypes.Depth,
                Direction = indexChannel.IsIncreasing()
                    ? IndexDirections.Increasing
                    : IndexDirections.Decreasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };
        }

        private Range<double?> GetIndexRange(Channel entity)
        {
            var start = entity.StartIndex;
            var end = entity.EndIndex;
            double? startValue = null;
            double? endValue = null;

            if (start is TimeIndexValue)
            {
                var startTime = start as TimeIndexValue;
                if (startTime != null && !string.IsNullOrEmpty(startTime.Time))
                    startValue = DateTimeOffset.Parse(startTime.Time).ToUnixTimeMicroseconds();
                var endTime = end as TimeIndexValue;
                if (endTime != null && !string.IsNullOrEmpty(endTime.Time))
                    endValue = DateTimeOffset.Parse(endTime.Time).ToUnixTimeMicroseconds();
            }
            else if (start is DepthIndexValue)
            {
                var startDepth = start as DepthIndexValue;
                if (startDepth != null && startDepth.Depth.HasValue)
                    startValue = startDepth.Depth.Value;
                var endDepth = end as DepthIndexValue;
                if (endDepth != null && endDepth.Depth.HasValue)
                    endValue = endDepth.Depth.Value;
            }
            else
            {
                var startPass = start as PassIndexedDepth;
                if (startPass != null && startPass.Depth.HasValue)
                    startValue = startPass.Depth.Value;
                var endPass = end as PassIndexedDepth;
                if (endPass != null && endPass.Depth.HasValue)
                    endValue = endPass.Depth.Value;
            }

            return new Range<double?>(startValue, endValue);
        }

        private List<Channel> GetChannelsByUris(params EtpUri[] uris)
        {
            if (uris.Any(u => u.IsBaseUri))
                return GetAll(null);

            var channelUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Channel);
            var wellboreUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Wellbore);
            var wellUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Well);
            if (wellUris.Any())
            {
                var wellboreFilters = wellUris.Select(wellUri => MongoDbUtility.BuildFilter<Wellbore>("Well.Uuid", wellUri.ObjectId)).ToList();
                var wellbores = GetCollection<Wellbore>(ObjectNames.Wellbore200)
                    .Find(Builders<Wellbore>.Filter.Or(wellboreFilters)).ToList();
                wellboreUris.AddRange(wellbores.Select(w => w.GetUri()).Where(u => !wellboreUris.Contains(u)));
            }

            var channelFilters = wellboreUris.Select(wellboreUri => MongoDbUtility.BuildFilter<Channel>("Wellbore.Uuid", wellboreUri.ObjectId)).ToList();
            channelFilters.AddRange(channelUris.Select(u => MongoDbUtility.GetEntityFilter<Channel>(u, IdPropertyName)));

            return channelFilters.Any() ? GetCollection().Find(Builders<Channel>.Filter.Or(channelFilters)).ToList() : new List<Channel>();
        }
    }
}