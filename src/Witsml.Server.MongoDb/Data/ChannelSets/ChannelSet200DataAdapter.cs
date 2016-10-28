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
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Models;
using PDS.Witsml.Server.Providers.Store;

namespace PDS.Witsml.Server.Data.ChannelSets
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="ChannelSet" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.Channels.IChannelDataProvider" />
    [Export200(ObjectTypes.ChannelSet, typeof(IChannelDataProvider))]
    public partial class ChannelSet200DataAdapter : IChannelDataProvider
    {
        private readonly string _utcFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK";

        private List<EtpUri> _wellboreUris;
        private List<EtpUri> _channelSetUris;

        /// <summary>
        /// Gets the channel metadata for the specified data object URI.
        /// </summary>
        /// <param name="uris">The parent data object URI.</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<ChannelMetadataRecord> GetChannelMetadata(params EtpUri[] uris)
        {
            var metaDatas = new List<ChannelMetadataRecord>();
            if (uris == null)
                return metaDatas;

            var entities = GetChannelSetByUris(uris.ToList());
            foreach (var entity in entities)
            {
                metaDatas.AddRange(GetChannelMetadataForAnEntity(entity, uris));
            }

            return metaDatas;
        }       

        private IList<ChannelMetadataRecord> GetChannelMetadataForAnEntity(ChannelSet entity, params EtpUri[] uris)
        {
            var metadata = new List<ChannelMetadataRecord>();
            var index = 0;

            if (entity.Channel == null || !entity.Channel.Any())
                return metadata;

            var indexMetadata = entity.Index
                .Select(x => ToIndexMetadataRecord(entity, x))
                .ToList();

            metadata.AddRange(entity.Channel.Where(c => IsChannelMetaDataRequested(c.GetUri(entity), uris)).Select(x =>
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
            return GetChannelData(uri, range, null);
        }

        private IEnumerable<IChannelDataRecord> GetChannelData(EtpUri uri, Range<double?> range, int? requestLatestValues)
        {
            var entity = GetEntity(uri);
            var indexChannel = entity.Index.FirstOrDefault();
            var increasing = indexChannel.IsIncreasing();
            var chunks = ChannelDataChunkAdapter.GetData(
                uri, indexChannel.Mnemonic, range,
                requestLatestValues.HasValue
                    ? !increasing
                    : increasing,
                reverse: requestLatestValues.HasValue);
            return chunks.GetRecords(range, increasing);
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
            List<List<List<object>>> logData;

            var entity = GetEntity(uri);
            var queryMnemonics = mnemonics.ToArray();
            var allMnemonics = GetAllMnemonics(entity);
            var mnemonicIndexes = ComputeMnemonicIndexes(entity, allMnemonics, queryMnemonics);
            var keys = mnemonicIndexes.Keys.ToArray();
            var units = GetUnitList(entity, keys);
            var nullValues = GetNullValueList(entity, keys);

            // Create a context to pass information required by the ChannelDataReader.
            var context = new ResponseContext()
            {
                RequestLatestValues = requestLatestValues,
                MaxDataNodes = WitsmlSettings.MaxDataNodes,
                MaxDataPoints = WitsmlSettings.MaxDataPoints
            };

            // Get the ranges for the query mnemonics
            var curveRanges = 
                GetCurrentIndexRange(entity)
                .Where(c => queryMnemonics.Contains(c.Key))
                .Select(r => r.Value).ToList(); 
            
            var indexChannel = entity.Index.FirstOrDefault();
            var increasing = indexChannel.IsIncreasing();
            var isTimeIndex = entity.IsTimeIndex();
            var rangeStart = curveRanges.GetMinRangeStart(increasing);
            var optimizeRangeStart = curveRanges.GetOptimizeRangeStart(increasing);
            var rangeEnd = curveRanges.GetMaxRangeEnd(increasing);
            var rangeStepSize = WitsmlSettings.GetRangeStepSize(isTimeIndex);

            bool finished;
            const int maxRequestFactor = 3;
            var requestFactor = 1;

            // Try an initial optimization for non-latest values and latest values.
            if (!requestLatestValues.HasValue && optimizeStart)
            {
                // Reset the start if specified start is before the minStart
                if (rangeStart.HasValue && range.StartsBefore(rangeStart.Value, increasing))
                {
                    range = new Range<double?>(rangeStart, range.End);
                }
            }
            else if (requestLatestValues.HasValue)
            {
                range = range.OptimizeLatestValuesRange(requestLatestValues, isTimeIndex, increasing, rangeStart, optimizeRangeStart, rangeEnd, requestFactor, rangeStepSize);
            }

            do // until finished
            {
                // Retrieve the data from the database
                var records = GetChannelData(uri, range, requestLatestValues);

                // Get a reader to process the log's channel data records
                var reader = records.GetReader(mnemonicIndexes.Values.ToArray(), units, nullValues);

                // Get the data from the reader based on the context and mnemonicIndexes (slices)
                Dictionary<string, Range<double?>> ranges;
                logData = reader.GetData(context, mnemonicIndexes, units, nullValues, out ranges);


                // Test if we're finished reading data
                finished =                              // Finished if...
                    !requestLatestValues.HasValue ||        // not request latest values
                    context.HasAllRequestedValues ||         // request latest values and all values returned
                    (rangeStart.HasValue &&                 // query range is at start of all channel data
                    range.StartsBefore(rangeStart.Value, increasing, true)) ||
                    !range.Start.HasValue;                  // query was for all data

                // If we're not finished try a bigger range
                if (!finished)
                {
                    requestFactor += 1;
                    if (requestFactor < maxRequestFactor)
                    {
                        range = range.OptimizeLatestValuesRange(requestLatestValues, isTimeIndex, increasing, rangeStart, optimizeRangeStart, rangeEnd, requestFactor, rangeStepSize);
                    }
                    else
                    {
                        // This is the final optimization and will stop the iterations after the next pass
                        range = new Range<double?>(null, null);
                    }
                }

            } while (!finished);

            return logData;
        }

        /// <summary>
        /// Updates the specified <see cref="Log" /> instance in the store.
        /// </summary>
        /// <param name="parser">The update parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, ChannelSet dataObject)
        {
            var uri = dataObject.GetUri();
            UpdateEntity(parser, uri);

            // Extract Data
            var reader = ExtractDataReader(dataObject, GetEntity(uri));
            UpdateChannelDataAndIndexRange(uri, reader);
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The parent data object URI.</param>
        /// <param name="reader">The update reader.</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
            // Capture primary index info when auto-creating data object
            var indexInfos = Exists(uri) ? null : reader.Indices;
            var offset = reader.Indices.Take(1).Select(x => x.IsTimeIndex).FirstOrDefault()
                ? reader.GetChannelIndexRange(0).Offset
                : null;

            // Ensure data object and parent data objects exist
            var dataProvider = Container.Resolve<IEtpDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
            dataProvider.Ensure(uri);

            // Only use the URI of the channelSet as it is a top level object
            uri = EtpUris.Witsml200.Append(ObjectTypes.ChannelSet, uri.ObjectId);
            reader.Uri = uri.Uri;

            if (indexInfos != null)
            {
                // Update data object with primary index info after it has been auto-created
                UpdateIndexInfo(uri, indexInfos, offset);
            }

            // Ensure all logCurveInfo elements exist
            UpdateChannels(uri, reader, offset);

            // Update channel data and index range
            UpdateChannelDataAndIndexRange(uri, reader);
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            base.Delete(uri);
            ChannelDataChunkAdapter.Delete(uri);
        }

        /// <summary>
        /// Adds <see cref="ChannelSet" /> to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The <see cref="ChannelSet" /> to be added.</param>
        public override void Add(WitsmlQueryParser parser, ChannelSet dataObject)
        {
            // Extract Data
            var reader = ExtractDataReader(dataObject);

            InsertEntity(dataObject);

            if (reader != null)
            {
                Logger.DebugFormat("Adding ChannelSet data with uid '{0}' and name '{1}'", dataObject.Uuid, dataObject.Citation.Title);
                var increasing = dataObject.IsIncreasing();
                var allMnemonics = reader.Indices.Select(i => i.Mnemonic).Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(dataObject);
                var indexCurve = dataObject.Index[0];
                Logger.DebugFormat("Index curve mnemonic: {0}.", indexCurve.Mnemonic);

                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, increasing);

                // Only use the URI of the channelSet as it is a top level object
                var uri = new EtpUri(reader.Uri);
                reader.Uri = EtpUris.Witsml200.Append(ObjectTypes.ChannelSet, uri.ObjectId);

                // Add ChannelDataChunks
                ChannelDataChunkAdapter.Add(reader);

                // Update index range
                UpdateIndexRange(dataObject.GetUri(), dataObject, ranges, allMnemonics);
            }
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

        private string[] GetAllMnemonics(ChannelSet entity)
        {
            return entity.Index.Select(i => i.Mnemonic).Concat(entity.Channel.Select(c => c.Mnemonic)).ToArray();
        }

        private IDictionary<int, string> ComputeMnemonicIndexes(ChannelSet entity, string[] allMnemonics, string[] queryMnemonics)
        {
            Logger.DebugFormat("Computing mnemonic indexes for ChannelSet.");

            // Start with all mnemonics
            var mnemonicIndexes = allMnemonics
                .Select((mn, index) => new { Mnemonic = mn, Index = index });

            // Check if mnemonics need to be filtered
            if (queryMnemonics.Any())
            {
                // always return the index channel
                mnemonicIndexes = mnemonicIndexes
                    .Where(x => x.Index < entity.Index.Count || queryMnemonics.Contains(x.Mnemonic));
            }

            // create an index-to-mnemonic map
            return new SortedDictionary<int, string>(mnemonicIndexes
                .ToDictionary(x => x.Index, x => x.Mnemonic));
        }

        private IDictionary<int, string> GetUnitsByColumnIndex(ChannelSet entity)
        {
            Logger.Debug("Getting ChannelSet Channel units by column index.");

            return new SortedDictionary<int, string>(entity.Index.Select(i => i.Uom)
                .Concat(entity.Channel.Select(c => c.Uom))
                .ToArray()
                .Select((unit, index) => new { Unit = unit, Index = index })
                .ToDictionary(x => x.Index, x => x.Unit));
        }

        private IDictionary<int, string> GetNullValuesByColumnIndex(ChannelSet entity)
        {
            Logger.Debug("Getting ChannelSet Channel null values by column index.");

            return new SortedDictionary<int, string>(entity.Index.Select(i => "null")
                .Concat(entity.Channel.Select(c => "null"))
                .ToArray()
                .Select((unit, index) => new { Unit = unit, Index = index })
                .ToDictionary(x => x.Index, x => x.Unit));
        }

        // TODO: See if this can be refactored to be common with LogDataAdapter.GetUnitList
        private IDictionary<int, string> GetUnitList(ChannelSet entity, int[] slices)
        {
            Logger.Debug("Getting unit list for log.");

            // Get a list of all of the units
            var allUnits = GetUnitsByColumnIndex(entity);

            // Start with all units
            var unitIndexes = allUnits
                .Select((unit, index) => new { Unit = unit.Value, Index = index });

            // Get indexes for each slice
            if (slices.Any())
            {
                // always return the index channel
                unitIndexes = unitIndexes
                    .Where(x => x.Index == 0 || slices.Contains(x.Index));
            }

            return new SortedDictionary<int, string>(unitIndexes.ToDictionary(x => x.Index, x => x.Unit));
        }

        // TODO: See if this can be refactored to be common with LogDataAdapter.GetNullValueList
        private IDictionary<int, string> GetNullValueList(ChannelSet entity, int[] slices)
        {
            Logger.Debug("Getting null value list for log.");

            // Get a list of all of the null values
            var allNullValues = GetNullValuesByColumnIndex(entity);

            // Start with all units
            var nullValuesIndexes = allNullValues
                .Select((nullValue, index) => new { NullValue = nullValue.Value, Index = index });

            // Get indexes for each slice
            if (slices.Any())
            {
                // always return the index channel
                nullValuesIndexes = nullValuesIndexes
                    .Where(x => x.Index == 0 || slices.Contains(x.Index));
            }

            return new SortedDictionary<int, string>(nullValuesIndexes.ToDictionary(x => x.Index, x => x.NullValue));
        }

        private ChannelMetadataRecord ToChannelMetadataRecord(ChannelSet entity, Channel channel, IList<IndexMetadataRecord> indexMetadata)
        {
            var uri = channel.GetUri(entity);
            var primaryIndex = indexMetadata.FirstOrDefault();
            var isTimeLog = primaryIndex != null && primaryIndex.IndexType == ChannelIndexTypes.Time;
            var curveIndexes = GetCurrentIndexRange(entity);
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
                StartIndex = primaryIndex == null ? null : curveIndexes[channel.Mnemonic].Start.IndexToScale(primaryIndex.Scale, isTimeLog),
                EndIndex = primaryIndex == null ? null : curveIndexes[channel.Mnemonic].End.IndexToScale(primaryIndex.Scale, isTimeLog),
                Indexes = indexMetadata,
                CustomData = new Dictionary<string, DataValue>()
            };
        }

        private IndexMetadataRecord ToIndexMetadataRecord(ChannelSet entity, ChannelIndex indexChannel, int scale = 3)
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

        private void UpdateChannelDataAndIndexRange(EtpUri uri, ChannelDataReader reader)
        {
            // Get Updated ChannelSet
            var current = GetEntity(uri);

            // Merge ChannelDataChunks
            if (reader != null)
            {
                var increasing = current.IsIncreasing();
                var allMnemonics = reader.Indices.Select(i => i.Mnemonic).Concat(reader.Mnemonics).ToArray();

                // Get current index information
                var ranges = GetCurrentIndexRange(current);
                GetUpdatedLogHeaderIndexRange(reader, allMnemonics, ranges, increasing);

                // Add ChannelDataChunks
                ChannelDataChunkAdapter.Merge(reader);

                // Update index range
                UpdateIndexRange(uri, current, ranges, allMnemonics);
            }
        }

        private Dictionary<string, Range<double?>> GetCurrentIndexRange(ChannelSet entity)
        {
            var ranges = new Dictionary<string, Range<double?>>();
            var index = entity.Index.FirstOrDefault();

            AddIndexRange(index.Mnemonic, entity.StartIndex, entity.EndIndex, ranges);

            foreach (var channel in entity.Channel)
            {
                AddIndexRange(channel.Mnemonic, channel.StartIndex, channel.EndIndex, ranges);
            }

            return ranges;
        }

        private void AddIndexRange(string mnemonic, AbstractIndexValue start, AbstractIndexValue end, Dictionary<string, Range<double?>> ranges)
        {
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

            ranges.Add(mnemonic, new Range<double?>(startValue, endValue));
        }

        private AbstractIndexValue UpdateIndexValue(ChannelIndexType? indexType, AbstractIndexValue current, double value)
        {
            AbstractIndexValue indexValue = null;

            if (indexType == ChannelIndexType.datetime || indexType == ChannelIndexType.elapsedtime)
            {
                if (current == null)
                    indexValue = new TimeIndexValue();
                else
                    indexValue = current;
                ((TimeIndexValue)indexValue).Time = DateTimeExtensions.FromUnixTimeMicroseconds((long)value).ToUniversalTime().ToString(_utcFormat);
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

        private void GetUpdatedLogHeaderIndexRange(ChannelDataReader reader, string[] mnemonics, Dictionary<string, Range<double?>> ranges, bool increasing = true)
        {
            for (var i = 0; i < mnemonics.Length; i++)
            {
                var mnemonic = mnemonics[i];
                Range<double?> current;

                if (!ranges.TryGetValue(mnemonic, out current))
                    current = new Range<double?>(null, null);

                var update = reader.GetChannelIndexRange(i);
                var start = current.Start;
                var end = current.End;

                if (!current.Start.HasValue || !update.StartsAfter(current.Start.Value, increasing))
                    start = update.Start;
                if (!current.End.HasValue || !update.EndsBefore(current.End.Value, increasing))
                    end = update.End;

                ranges[mnemonic] = new Range<double?>(start, end);
            }
        }

        private void UpdateIndexRange(EtpUri uri, ChannelSet entity, Dictionary<string, Range<double?>> ranges, IEnumerable<string> mnemonics)
        {
            var mongoUpdate = new MongoDbUpdate<ChannelSet>(Container, GetCollection(), null);
            var channelIndexUpdate = default(UpdateDefinition<ChannelSet>);
            var filter = GetEntityFilter(uri);

            if (entity.Citation != null)
            {
                if (entity.Citation.Creation.HasValue)
                {
                    var creationTime = entity.Citation.Creation;
                    channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "Citation.Creation", creationTime.Value.ToUniversalTime().ToString(_utcFormat));
                }
                if (entity.Citation.LastUpdate.HasValue)
                {
                    var updateTime = entity.Citation.LastUpdate;
                    channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "Citation.LastUpdate", updateTime.Value.ToUniversalTime().ToString(_utcFormat));
                }
            }

            var indexChannel = entity.Index.FirstOrDefault();
            var indexType = indexChannel.IndexType;
            var indexMnemonic = indexChannel.Mnemonic;
            var range = ranges[indexMnemonic];

            if (range.Start.HasValue)
            {
                var start = UpdateIndexValue(indexType, entity.StartIndex, range.Start.Value);
                channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "StartIndex", start);
            }

            if (range.End.HasValue)
            {
                var end = UpdateIndexValue(indexType, entity.EndIndex, range.End.Value);
                channelIndexUpdate = MongoDbUtility.BuildUpdate(channelIndexUpdate, "EndIndex", end);
            }

            if (channelIndexUpdate != null)
                mongoUpdate.UpdateFields(filter, channelIndexUpdate);

            var idField = MongoDbUtility.LookUpIdField(typeof(Channel), "Uuid");

            foreach (var mnemonic in mnemonics)
            {
                var channel = entity.Channel.FirstOrDefault(c => c.Mnemonic.EqualsIgnoreCase(mnemonic));
                if (channel == null)
                    continue;

                var channelFilter = Builders<ChannelSet>.Filter.And(filter,
                    MongoDbUtility.BuildFilter<ChannelSet>("Channel." + idField, channel.Uuid));

                UpdateDefinition<ChannelSet> updates = null;
                range = ranges[mnemonic];

                if (range.Start.HasValue)
                {
                    var start = UpdateIndexValue(indexType, channel.StartIndex, range.Start.Value);
                    updates = MongoDbUtility.BuildUpdate(updates, "Channel.$.StartIndex", start);
                }

                if (range.End.HasValue)
                {
                    var end = UpdateIndexValue(indexType, channel.EndIndex, range.End.Value);
                    updates = MongoDbUtility.BuildUpdate(updates, "Channel.$.EndIndex", end);
                }

                if (updates != null)
                {
                    mongoUpdate.UpdateFields(channelFilter, updates);
                }
            }
        }

        private void UpdateChannels(EtpUri uri, ChannelDataReader reader, TimeSpan? offset)
        {
            var entity = GetEntity(uri);
            Logger.DebugFormat("Updating channels for uid '{0}' and name '{1}'.", entity.Uuid, entity.Citation.Title);

            var isTimeIndex = reader.Indices.Take(1).Select(x => x.IsTimeIndex).FirstOrDefault();

            var channels = reader.Mnemonics
                .Select((x, i) => new { Mnemonic = x, Index = i })
                .Where(x => entity.Channel.GetByMnemonic(x.Mnemonic) == null)
                .Select(x => CreateChannel(uri, x.Mnemonic, reader.Units[x.Index], isTimeIndex, entity.Index));

            var mongoUpdate = new MongoDbUpdate<ChannelSet>(Container, GetCollection(), null);
            var headerUpdate = MongoDbUtility.BuildPushEach<ChannelSet, Channel>(null, "Channel", channels);
            var filter = GetEntityFilter(uri);

            mongoUpdate.UpdateFields(filter, headerUpdate);
        }

        private void UpdateIndexInfo(EtpUri uri, IList<ChannelIndexInfo> indexInfos, TimeSpan? offset)
        {
            var entity = GetEntity(uri);
            Logger.DebugFormat("Updating index info for uid '{0}' and name '{1}'.", entity.Uuid, entity.Citation.Title);

            // Add ChannelIndex for each index
            var headerUpdate = MongoDbUtility.BuildPushEach<ChannelSet, ChannelIndex>(null, "Index", indexInfos.Select(CreateChannelIndex));
            // TODO: Update Citation
            //headerUpdate = UpdateCommonData(headerUpdate, entity, offset);

            var mongoUpdate = new MongoDbUpdate<ChannelSet>(Container, GetCollection(), null);
            var filter = GetEntityFilter(uri);

            mongoUpdate.UpdateFields(filter, headerUpdate);
        }

        private ChannelIndex CreateChannelIndex(ChannelIndexInfo indexInfo)
        {
            return new ChannelIndex
            {
                Mnemonic = indexInfo.Mnemonic,
                Uom = indexInfo.Unit,
                IndexType = indexInfo.IsTimeIndex ? ChannelIndexType.datetime : ChannelIndexType.measureddepth,
                Direction = indexInfo.Increasing ? IndexDirection.increasing : IndexDirection.decreasing,
                DatumReference = "MSL"
            };
        }

        private Channel CreateChannel(EtpUri uri, string mnemonic, string unit, bool isTimeIndex, List<ChannelIndex> indexes)
        {
            var channel = new Channel
            {
                Mnemonic = mnemonic,
                DataType = EtpDataType.@double,
                GrowingStatus = ChannelStatus.active,
                Uom = unit,
                TimeDepth = isTimeIndex ? "time" : "depth",
                LoggingCompanyName = "unknown",
                CurveClass = "unknown",
                Index = indexes
            };

            channel.Uuid = channel.NewUuid();
            channel.Citation = channel.Citation.Create();
            channel.Citation.Title = mnemonic;
            channel.SchemaVersion = uri.Version;

            return channel;
        }

        private List<ChannelSet> GetChannelSetByUris(List<EtpUri> uris)
        {
            if (uris.Any(u => u.IsBaseUri))
                return GetAll(null);

            _wellboreUris = new List<EtpUri>();
            _channelSetUris = new List<EtpUri>();

            var channelSetUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.ChannelSet);
            var wellboreUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Wellbore);
            var wellUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Well);

            if (wellUris.Any())
            {
                var wellboreFilters = wellUris.Select(wellUri => MongoDbUtility.BuildFilter<Wellbore>("Well.Uuid", wellUri.ObjectId)).ToList();
                var wellbores = GetCollection<Wellbore>(ObjectNames.Wellbore200)
                    .Find(Builders<Wellbore>.Filter.Or(wellboreFilters)).ToList();
                wellboreUris.AddRange(wellbores.Select(w => w.GetUri()).Where(u => !wellboreUris.Contains(u)));
            }

            _wellboreUris.AddRange(wellboreUris);
            var channelSetFilters = wellboreUris.Select(wellboreUri => MongoDbUtility.BuildFilter<ChannelSet>("Wellbore.Uuid", wellboreUri.ObjectId)).ToList();

            _channelSetUris.AddRange(channelSetUris);
            channelSetFilters.AddRange(channelSetUris.Select(GetEntityFilter));

            var channelUris = MongoDbUtility.GetObjectUris(uris, ObjectTypes.Channel).Where(u => u.Parent.ObjectType == ObjectTypes.ChannelSet);
            foreach (var channelUri in channelUris)
            {
                channelSetFilters.Add(MongoDbUtility.BuildFilter<ChannelSet>(IdPropertyName, channelUri.Parent.ObjectId));
            }

            return channelSetFilters.Any() ? GetCollection().Find(Builders<ChannelSet>.Filter.Or(channelSetFilters)).ToList() : new List<ChannelSet>();
        }

        private bool IsChannelMetaDataRequested(EtpUri channelUri, params EtpUri[] uris)
        {
            // e.g. eml://witsml20 or eml://witsml20/channelSet(channelSet_uuid)/channel(GR)
            if (uris.Any(u => u.IsBaseUri) || uris.Contains(channelUri))
                return true;

            // e.g. eml://witsml20/channelSet(channelSet_uuid)
            var parent = channelUri.Parent;
            if (uris.Contains(parent)) return true;

            // e.g. eml://witsml20/channelSet(channelSet_uuid)/channel
            var folder = parent.Append(channelUri.ObjectType);
            if (uris.Contains(folder)) return true;

            if (_channelSetUris != null &&
                _channelSetUris.Any(u => u.ObjectId.EqualsIgnoreCase(channelUri.Parent.ObjectId)))
                return true;

            if (_wellboreUris != null &&
                _wellboreUris.Any(u => u.ObjectId.EqualsIgnoreCase(channelUri.Parent.Parent.ObjectId)))
                return true;

            return false;
        }
    }
}
