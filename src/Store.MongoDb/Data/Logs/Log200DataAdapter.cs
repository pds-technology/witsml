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
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using MongoDB.Driver;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Data.Channels;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Log" />
    /// </summary>
    [Export(typeof(IChannelDataProvider))]
    [Export200(ObjectTypes.Log, typeof(IChannelDataProvider))]
    public partial class Log200DataAdapter : IChannelDataProvider
    {
        /// <summary>
        /// Gets the channels metadata.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uris">The collection of URI to describe.</param>
        /// <returns>A collection of channel metadata.</returns>
        /// <exception cref="WitsmlException">IChannelDataProvider not configured.</exception>
        public IList<IChannelMetadataRecord> GetChannelMetadata(IEtpAdapter etpAdapter, params EtpUri[] uris)
        {
            var adapter = ChannelSetDataAdapter as IChannelDataProvider;

            if (adapter == null)
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, "IChannelDataProvider not configured.");

            var channelUris = new List<EtpUri>();
            channelUris.AddRange(
                uris.Where(
                    u =>
                        u.ObjectType.EqualsIgnoreCase(ObjectTypes.ChannelSet) ||
                        u.ObjectType.EqualsIgnoreCase(ObjectTypes.Channel)).ToList());

            var logs = GetLogsByUris(uris.ToList());

            if (!logs.Any())
                return adapter.GetChannelMetadata(etpAdapter, channelUris.Any() ? channelUris.ToArray() : null);

            foreach (var log in logs)
            {
                Logger.Debug($"Getting channel metadata for URI: {log.GetUri()}");
                channelUris.AddRange(log.ChannelSet
                    .Select(x => x.GetUri())
                    .Where(u => !channelUris.Contains(u))
                    .ToList());
            }

            return adapter.GetChannelMetadata(etpAdapter, channelUris.ToArray());
        }

        /// <summary>
        /// Gets the channel data records for the specified data object URI and range.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="range">The data range to retrieve.</param>
        /// <returns>A collection of channel data.</returns>
        /// <exception cref="WitsmlException">IChannelDataProvider not configured.</exception>
        public IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range)
        {
            Logger.Debug($"Getting channel data for URI: {uri}");
            var adapter = ChannelSetDataAdapter as IChannelDataProvider;

            if (adapter == null)
                throw new WitsmlException(ErrorCodes.ErrorReadingFromDataStore, "IChannelDataProvider not configured.");

            var entity = GetEntity(uri);

            return entity.ChannelSet
                .SelectMany(x => adapter.GetChannelData(x.GetUri(), range));
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
        /// <returns>A collection of channel data.</returns>
        public List<List<List<object>>> GetChannelData(EtpUri uri, Range<double?> range, List<string> mnemonics, int? requestLatestValues, bool optimizeStart = false)
        {
            return new List<List<List<object>>>();
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, Log dataObject)
        {
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(dataObject.GetUri());

                // Add ChannelSets + data via the ChannelSet data adapter
                foreach (var childParser in parser.ForkProperties("ChannelSet", ObjectTypes.ChannelSet))
                {
                    var channelSet = WitsmlParser.Parse<ChannelSet>(childParser.Root);
                    ChannelSetDataAdapter.Add(childParser, channelSet);
                }

                // Clear ChannelSet data properties
                foreach (var channelSet in dataObject.ChannelSet)
                {
                    channelSet.Data = null;
                }

                InsertEntity(dataObject);

                // Commit transaction
                transaction.Commit();
            }
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, Log dataObject)
        {
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(dataObject.GetUri());

                // Update ChannelSets + data via the ChannelSet data adapter
                foreach (var childParser in parser.ForkProperties("ChannelSet", ObjectTypes.ChannelSet))
                {
                    var channelSet = WitsmlParser.Parse<ChannelSet>(childParser.Root);
                    ChannelSetDataAdapter.Update(childParser, channelSet);
                }

                var uri = GetUri(dataObject);
                UpdateEntity(parser, uri);

                // Commit transaction
                transaction.Commit();
            }
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForQuery(WitsmlQueryParser parser)
        {
            return new List<string> { "Data" };
        }

        /// <summary>
        /// Gets a list of the element names to ignore during an update.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForUpdate(WitsmlQueryParser parser)
        {
            return GetIgnoredElementNamesForQuery(parser);
        }

        private List<Log> GetLogsByUris(List<EtpUri> uris)
        {
            if (uris.Any(u => u.IsBaseUri))
                return GetAll(null);

            var logUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Log);
            var wellboreUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Wellbore);
            var wellUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Well);
            if (wellUris.Any())
            {
                var wellboreFilters = wellUris.Select(wellUri => MongoDbUtility.BuildFilter<Wellbore>("Well.Uuid", wellUri.ObjectId)).ToList();
                var wellbores = GetCollection<Wellbore>(ObjectNames.Wellbore200)
                    .Find(Builders<Wellbore>.Filter.Or(wellboreFilters)).ToList();
                wellboreUris.AddRange(wellbores.Select(w => w.GetUri()).Where(u => !wellboreUris.Contains(u)));
            }

            var logFilters = wellboreUris.Select(wellboreUri => MongoDbUtility.BuildFilter<Log>("Wellbore.Uuid", wellboreUri.ObjectId)).ToList();
            logFilters.AddRange(logUris.Select(logUri => MongoDbUtility.GetEntityFilter<Log>(logUri, IdPropertyName)));

            return logFilters.Any() ? GetCollection().Find(Builders<Log>.Filter.Or(logFilters)).ToList() : new List<Log>();
        }
    }
}
