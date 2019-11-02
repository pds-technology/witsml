//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
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
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Providers;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Channel" />
    /// </summary>
    [Export(typeof(IChannelDataProvider))]
    [Export200(ObjectTypes.Channel, typeof(IChannelDataProvider))]
    public partial class Channel200DataAdapter : IChannelDataProvider
    {
        /// <summary>
        /// Gets the channels metadata.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uris">The collection of URI to describe.</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<IChannelMetadataRecord> GetChannelMetadata(IEtpAdapter etpAdapter, params EtpUri[] uris)
        {
            var metadatas = new List<IChannelMetadataRecord>();

            var channels = GetChannelsByUris(uris);
            foreach (var channel in channels)
            {
                Logger.Debug($"Getting channel metadata for URI: {channel.GetUri()}");
                var indexMetadata = channel.Index
                    .Select(x => ToIndexMetadataRecord(etpAdapter, channel, x))
                    .ToList();

                metadatas.Add(ToChannelMetadataRecord(etpAdapter, channel, indexMetadata));
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

        private IChannelMetadataRecord ToChannelMetadataRecord(IEtpAdapter etpAdapter, Channel channel, IList<IIndexMetadataRecord> indexMetadata)
        {
            var uri = channel.GetUri();
            var channelIndex = GetIndexRange(channel);
            var lastChanged = channel.Citation.LastUpdate.ToUnixTimeMicroseconds().GetValueOrDefault();

            var primaryIndex = indexMetadata.FirstOrDefault();
            var isTimeIndex = etpAdapter.IsTimeIndex(primaryIndex);

            var dataObject = etpAdapter.CreateDataObject();
            etpAdapter.SetDataObject(dataObject, channel, uri, channel.Mnemonic, 0, lastChanged);

            var metadata = etpAdapter.CreateChannelMetadata(uri);
            metadata.DataType = channel.DataType.GetValueOrDefault(EtpDataType.@double).ToString().Replace("@", string.Empty);
            metadata.Description = channel.Citation != null ? channel.Citation.Description ?? channel.Mnemonic : channel.Mnemonic;
            metadata.ChannelName = channel.Mnemonic;
            metadata.Uom = Units.GetUnit(channel.Uom);
            metadata.MeasureClass = channel.ChannelClass?.Title ?? ObjectTypes.Unknown;
            metadata.Source = channel.Source ?? ObjectTypes.Unknown;
            metadata.Uuid = channel.Mnemonic;
            metadata.DomainObject = dataObject;
            //metadata.Status = (ChannelStatuses)(int)channel.GrowingStatus.GetValueOrDefault(ChannelStatus.inactive);
            metadata.StartIndex = primaryIndex == null ? null : channelIndex.Start.IndexToScale(primaryIndex.Scale, isTimeIndex);
            metadata.EndIndex = primaryIndex == null ? null : channelIndex.End.IndexToScale(primaryIndex.Scale, isTimeIndex);
            metadata.Indexes = etpAdapter.ToList(indexMetadata);

            return metadata;
        }

        private IIndexMetadataRecord ToIndexMetadataRecord(IEtpAdapter etpAdapter, Channel entity, ChannelIndex indexChannel, int scale = 3)
        {
            var metadata = etpAdapter.CreateIndexMetadata(
                uri: indexChannel.GetUri(entity),
                isTimeIndex: indexChannel.IsTimeIndex(true),
                isIncreasing: indexChannel.IsIncreasing());

            metadata.Mnemonic = indexChannel.Mnemonic;
            metadata.Description = indexChannel.Mnemonic;
            metadata.Uom = Units.GetUnit(indexChannel.Uom);
            metadata.Scale = scale;

            return metadata;
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
                if (startTime.Time.HasValue)
                    startValue = startTime.Time.ToUnixTimeMicroseconds();
                var endTime = end as TimeIndexValue;
                if (endTime?.Time.HasValue ?? false)
                    endValue = endTime.Time.ToUnixTimeMicroseconds();
            }
            else if (start is DepthIndexValue)
            {
                var startDepth = start as DepthIndexValue;
                if (startDepth.Depth.HasValue)
                    startValue = startDepth.Depth.Value;
                var endDepth = end as DepthIndexValue;
                if (endDepth?.Depth.HasValue ?? false)
                    endValue = endDepth.Depth.Value;
            }
            else
            {
                var startPass = start as PassIndexedDepth;
                if (startPass?.Depth.HasValue ?? false)
                    startValue = startPass.Depth.Value;
                var endPass = end as PassIndexedDepth;
                if (endPass?.Depth.HasValue ?? false)
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
