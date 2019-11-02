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
using System.Linq;
using System.Text;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.ChannelData;
using Energistics.Etp.Common.Datatypes.Object;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Channels;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;
using PDS.WITSMLstudio.Store.Providers;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Trajectory" />
    /// </summary>
    [Export200(ObjectTypes.Trajectory, typeof(IChannelDataProvider))]
    [Export200(ObjectTypes.Trajectory, typeof(IGrowingObjectDataAdapter))]    
    public partial class Trajectory200DataAdapter : IChannelDataProvider
    {
        private const string FileQueryField = "Uri";
        private const string FileName = "FileName";

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        public override Trajectory Get(EtpUri uri, params string[] fields)
        {
            var entity = GetEntity(uri, fields);

            if (entity != null)
            {
                ClearTrajectoryStations(entity);
            }

            return entity;
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, Trajectory dataObject)
        {
            var uri = dataObject.GetUri();

            using (var transaction = GetTransaction())
            {
                transaction.SetContext(uri);

                if (!CanSaveData())
                {
                    ClearTrajectoryStations(dataObject);
                }

                SetIndexRange(dataObject, parser);
                UpdateMongoFile(dataObject, false);
                InsertEntity(dataObject);
                UpdateGrowingObject(uri);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        public override void Replace(WitsmlQueryParser parser, Trajectory dataObject)
        {
            var uri = dataObject.GetUri();

            if (!CanSaveData())
            {
                ClearTrajectoryStations(dataObject);
            }

            UpdateTrajectoryWithStations(parser, dataObject, uri);
        }

        /// <summary>
        /// Gets the channel metadata.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uris">The uris to retrieve metadata for</param>
        /// <returns>A collection of channel metadata.</returns>
        public IList<IChannelMetadataRecord> GetChannelMetadata(IEtpAdapter etpAdapter, params EtpUri[] uris)
        {
            var metadata = new List<IChannelMetadataRecord>();
            var entities = GetChannelsByUris(uris);

            foreach (var entity in entities)
            {
                Logger.Debug($"Getting channel metadata for URI: {entity.GetUri()}");
                metadata.AddRange(GetChannelMetadataForAnEntity(etpAdapter, entity, uris));
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
            var entity = GetEntity(uri);
            var stations = GetStations(entity, range.Start, range.End);

            if (requestLatestValues.HasValue)
            {
                stations = stations.AsEnumerable().Reverse() // Pick from the bottom
                    .Take(requestLatestValues.Value).Reverse() // Revert to original sort
                    .ToList();
            }

            return stations
                .Select(x => new List<List<object>>
                {
                    new List<object> { x.MD.Value },
                    new List<object> { WitsmlParser.ToXml(x) }
                })
                .ToList();
        }

        /// <summary>
        /// Updates the channel data for the specified data object URI.
        /// </summary>
        /// <param name="uri">The URI of the data object</param>
        /// <param name="reader">A reader for the channel data</param>
        public void UpdateChannelData(EtpUri uri, ChannelDataReader reader)
        {
        }

        /// <summary>
        /// Determines whether this instance can save the data portion of the growing object.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instance can save the data portion of the growing object; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanSaveData()
        {
            return WitsmlOperationContext.Current.Request.Function != Functions.PutObject || CompatibilitySettings.TrajectoryAllowPutObjectWithData;
        }

        /// <summary>
        /// Gets the growing part having the specified UID for a growing object.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="uid">The growing part's uid.</param>
        /// <returns></returns>
        public override IDataObject GetGrowingPart(IEtpAdapter etpAdapter, EtpUri uri, string uid)
        {
            var entity = GetEntity(uri);
            var trajectoryStation = GetStation(entity, uid);

            return ToDataObject(etpAdapter, entity, trajectoryStation);
        }

        /// <summary>
        /// Gets the growing parts for a growing object within the specified index range.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns></returns>
        public override List<IDataObject> GetGrowingParts(IEtpAdapter etpAdapter, EtpUri uri, object startIndex, object endIndex)
        {
            var entity = GetEntity(uri);
            var trajectoryStations = GetStations(entity, ToTrajectoryIndex(startIndex), ToTrajectoryIndex(endIndex));

            return trajectoryStations
                .Select(ts => ToDataObject(etpAdapter, entity, ts))
                .ToList();
        }

        /// <summary>
        /// Puts the growing part for a growing object.
        /// </summary>
        /// <param name="etpAdapter">The ETP adapter.</param>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="data">The data.</param>
        public override void PutGrowingPart(IEtpAdapter etpAdapter, EtpUri uri, string contentType, byte[] data)
        {
            var dataObject = etpAdapter.CreateDataObject();
            dataObject.Data = data;

            // Convert byte array to TrajectoryStation
            var trajectoryStationXml = dataObject.GetString();
            var tsDocument = WitsmlParser.Parse(trajectoryStationXml);
            var trajectoryStation = WitsmlParser.Parse<TrajectoryStation>(tsDocument.Root);

            // Merge TrajectoryStation into the Trajectory if it is not null
            if (trajectoryStation != null)
            {
                // Get the Trajectory for the uri
                var entity = GetEntity(uri);
                entity.TrajectoryStation = trajectoryStation.AsList();

                var document = WitsmlParser.Parse(WitsmlParser.ToXml(entity));
                var parser = new WitsmlQueryParser(document.Root, ObjectTypes.GetObjectType<Trajectory>(), null);
                UpdateTrajectoryWithStations(parser, entity, uri, true);
            }
        }

        /// <summary>
        /// Deletes the growing part having the specified UID for a growing object.
        /// </summary>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="uid">The growing part's uid.</param>
        public override void DeleteGrowingPart(EtpUri uri, string uid)
        {
            base.DeleteGrowingPart(uri, uid);
        }

        /// <summary>
        /// Deletes the growing parts for a growing object within the specified index range.
        /// </summary>
        /// <param name="uri">The growing obejct's URI.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public override void DeleteGrowingParts(EtpUri uri, object startIndex, object endIndex)
        {
            base.DeleteGrowingParts(uri, startIndex, endIndex);
        }

        /// <summary>
        /// Determines whether the objectGrowing flag is true for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        /// <c>true</c> if the objectGrowing flag is true for the specified entity; otherwise, <c>false</c>.
        /// </returns>
        protected override bool IsObjectGrowing(Trajectory entity)
        {
            return entity.GrowingStatus.HasValue && entity.GrowingStatus.Value == ChannelStatus.active;
        }

        /// <summary>
        /// Gets the wellbore URI from the specified childUri
        /// </summary>
        /// <param name="childUri">The child URI.</param>
        /// <returns>The wellbore uri from a specified childUri</returns>
        protected override EtpUri GetWellboreUri(EtpUri childUri)
        {
            var childEntity = GetEntity(childUri);
            return childEntity.Wellbore.GetUri();
        }

        private void ClearTrajectoryStations(Trajectory entity)
        {
            entity.TrajectoryStation = null;
        }

        private void SetIndexRange(Trajectory dataObject, WitsmlQueryParser parser, bool force = true)
        {
            Logger.Debug("Set trajectory MD ranges.");

            if (dataObject.TrajectoryStation == null || dataObject.TrajectoryStation.Count <= 0)
            {
                dataObject.MDMin = null;
                dataObject.MDMax = null;
                return;
            }

            SortStationData(dataObject.TrajectoryStation);

            var returnElements = parser.ReturnElements();
            var alwaysInclude = force ||
                                OptionsIn.ReturnElements.All.Equals(returnElements) ||
                                OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements);

            if (alwaysInclude || parser.Contains("mdMn"))
            {
                dataObject.MDMin = dataObject.TrajectoryStation.First().MD;
            }

            if (alwaysInclude || parser.Contains("mdMx"))
            {
                dataObject.MDMax = dataObject.TrajectoryStation.Last().MD;
            }
        }

        private void SortStationData(List<TrajectoryStation> stations)
        {
            // Sort stations by MD
            stations.Sort((x, y) => (x.MD?.Value ?? -1).CompareTo(y.MD?.Value ?? -1));
        }

        private void UpdateMongoFile(Trajectory entity, bool deleteFile = true)
        {
            var uri = entity.GetUri();
            Logger.Debug($"Updating MongoDb Trajectory Stations files: {uri}");

            var bucket = GetMongoFileBucket();
            var stations = entity.TrajectoryStation; //GetTrajectoryStations(entity);

            if (stations != null && stations.Count >= WitsmlSettings.MaxStationCount)
            {
                var bytes = Encoding.UTF8.GetBytes(stations.ToJson());

                var loadOptions = new GridFSUploadOptions
                {
                    Metadata = new BsonDocument
                    {
                        { FileName, Guid.NewGuid().ToString() },
                        { FileQueryField, uri.ToString() },
                        { "DataBytes", bytes.Length }
                    }
                };

                if (deleteFile)
                    DeleteMongoFile(bucket, uri);

                bucket.UploadFromBytes(uri, bytes, loadOptions);
                ClearTrajectoryStations(entity);
            }
            else
            {
                if (deleteFile)
                    DeleteMongoFile(bucket, uri);
            }
        }

        private IGridFSBucket GetMongoFileBucket()
        {
            var db = DatabaseProvider.GetDatabase();
            return new GridFSBucket(db, new GridFSBucketOptions
            {
                BucketName = DbCollectionName,
                ChunkSizeBytes = WitsmlSettings.ChunkSizeBytes
            });
        }

        private void DeleteMongoFile(IGridFSBucket bucket, string fileId)
        {
            Logger.Debug($"Deleting MongoDb Channel Data file: {fileId}");

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileQueryField], fileId);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return;

            bucket.Delete(mongoFile.Id);
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

        private IList<IChannelMetadataRecord> GetChannelMetadataForAnEntity(IEtpAdapter etpAdapter, Trajectory entity, params EtpUri[] uris)
        {
            var metadata = new List<IChannelMetadataRecord>();

            // Get Index Metadata
            var indexMetadata = ToIndexMetadataRecord(etpAdapter, entity);

            // Get Channel Metadata
            var channel = ToChannelMetadataRecord(etpAdapter, entity, indexMetadata);
            metadata.Add(channel);

            return metadata;
        }

        private IIndexMetadataRecord ToIndexMetadataRecord(IEtpAdapter etpAdapter, Trajectory entity, int scale = 3)
        {
            var metadata = etpAdapter.CreateIndexMetadata(
                uri: entity.GetUri().Append("md"),
                isTimeIndex: false,
                isIncreasing: true);

            metadata.Mnemonic = "MD";
            metadata.Description = LogIndexType.measureddepth.GetName();
            metadata.Uom = Units.GetUnit(entity.MDMin?.Uom.ToString());
            metadata.Scale = scale;

            return metadata;
        }

        private IChannelMetadataRecord ToChannelMetadataRecord(IEtpAdapter etpAdapter, Trajectory entity, IIndexMetadataRecord indexMetadata)
        {
            var uri = entity.GetUri();
            var lastChanged = entity.Citation.LastUpdate.ToUnixTimeMicroseconds().GetValueOrDefault();

            var dataObject = etpAdapter.CreateDataObject();
            etpAdapter.SetDataObject(dataObject, entity, uri, entity.Citation.Title, 0, lastChanged);

            var metadata = etpAdapter.CreateChannelMetadata(uri);
            metadata.DataType = EtpDataType.@string.GetName();
            metadata.Description = entity.Citation.Description ?? entity.Citation.Title;
            metadata.ChannelName = entity.Citation.Title;
            metadata.Uom = Units.GetUnit(entity.MDMin?.Uom.ToString());
            metadata.MeasureClass = ObjectTypes.Unknown;
            metadata.Source = ObjectTypes.Unknown;
            metadata.Uuid = entity.Uuid;
            metadata.DomainObject = dataObject;
            metadata.Status = etpAdapter.GetChannelStatus(entity.GrowingStatus == ChannelStatus.active);
            metadata.StartIndex = entity.MDMin?.Value.IndexToScale(indexMetadata.Scale);
            metadata.EndIndex = entity.MDMax?.Value.IndexToScale(indexMetadata.Scale);
            metadata.Indexes = etpAdapter.ToList(new List<IIndexMetadataRecord> { indexMetadata });

            return metadata;
        }

        private TrajectoryStation GetStation(Trajectory entity, string uid)
        {
            GetSavedStations(entity);

            return entity.TrajectoryStation.FirstOrDefault(ts => ts.Uid.Equals(uid, StringComparison.InvariantCultureIgnoreCase));
        }

        private List<TrajectoryStation> GetStations(Trajectory entity, double? startIndex, double? endIndex)
        {
            GetSavedStations(entity);

            // Allow for open ended ranges
            startIndex = startIndex ?? double.MinValue;
            endIndex = endIndex ?? double.MaxValue;

            return entity.TrajectoryStation
                .Where(ts => ts.MD.Value >= startIndex && ts.MD.Value <= endIndex)
                .ToList();
        }

        private double? ToTrajectoryIndex(object index)
        {
            var convertible = index as IConvertible;
            return convertible?.ToDouble(null);
        }

        private static IDataObject ToDataObject(IEtpAdapter etpAdapter, Trajectory entity, TrajectoryStation trajectoryStation)
        {
            var dataObject = etpAdapter.CreateDataObject();

            if (entity == null || trajectoryStation == null)
                return dataObject;

            var uri = entity.GetUri();
            var childUri = uri.Append(ObjectTypes.TrajectoryStation, trajectoryStation.Uid);
            etpAdapter.SetDataObject(dataObject, trajectoryStation, childUri, entity.Citation.Title, 0, compress: false);

            return dataObject;
        }

        private void GetSavedStations(Trajectory entity)
        {
            var chunked = IsQueryingStationFile(entity);

            if (chunked)
            {
                var stations = GetMongoFileStationData(entity.GetUri());
                FilterStationData(entity, stations);
            }
        }

        private void UpdateTrajectoryWithStations(WitsmlQueryParser parser, Trajectory dataObject, EtpUri uri, bool merge = false)
        {
            var current = GetEntity(uri);
            var chunked = IsQueryingStationFile(current);
            var stations = dataObject.TrajectoryStation;
            var isAppending = false;

            string uomIndex;
            var rangeIn = GetIndexRange(stations, out uomIndex);

            if (merge)
            {
                if (chunked)
                {
                    stations = GetMongoFileStationData(uri);
                    FilterStationData(current, stations);
                }

                var savedStations = current.TrajectoryStation
                    .Select(x => x.Uid)
                    .ToArray();

                isAppending = dataObject.TrajectoryStation
                    .Any(x => !savedStations.ContainsIgnoreCase(x.Uid));

                MergeEntity(current, parser);
                dataObject = current;
            }

            using (var transaction = GetTransaction())
            {
                transaction.SetContext(uri);

                if (chunked)
                {
                    var bucket = GetMongoFileBucket();
                    DeleteMongoFile(bucket, uri);
                }

                SetIndexRange(dataObject, parser);
                UpdateMongoFile(dataObject, false);
                ReplaceEntity(dataObject, uri, false);
                UpdateGrowingObject(dataObject, false, isAppending, rangeIn.Start, rangeIn.End, uomIndex);
                transaction.Commit();
            }
        }

        private bool IsQueryingStationFile(Trajectory entity)
        {
            return entity.MDMin != null && entity.TrajectoryStation == null;
        }

        private Range<double?> GetIndexRange(List<TrajectoryStation> stations, out string uom)
        {
            uom = string.Empty;

            if (stations == null || stations.Count == 0)
                return new Range<double?>(null, null);

            SortStationData(stations);

            var mdMin = stations.First().MD;
            var mdMax = stations.Last().MD;
            uom = mdMin?.Uom.ToString() ?? string.Empty;

            return new Range<double?>(mdMin?.Value, mdMax?.Value);
        }

        private List<TrajectoryStation> GetMongoFileStationData(string uri)
        {
            Logger.Debug("Getting MongoDb Trajectory Station files.");

            var bucket = GetMongoFileBucket();

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileQueryField], uri);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return null;

            var bytes = bucket.DownloadAsBytes(mongoFile.Id);
            var json = Encoding.UTF8.GetString(bytes);

            return BsonSerializer.Deserialize<List<TrajectoryStation>>(json);
        }

        private int FilterStationData(Trajectory entity, List<TrajectoryStation> stations, WitsmlQueryParser parser = null, IQueryContext context = null)
        {
            if (stations == null || stations.Count == 0)
                return 0;

            var range = GetQueryIndexRange(parser);
            var maxDataNodes = context?.MaxDataNodes;

            entity.TrajectoryStation = range.Start.HasValue
                ? range.End.HasValue
                    ? stations.Where(s => s.MD.Value >= range.Start.Value && s.MD.Value <= range.End.Value).ToList()
                    : stations.Where(s => s.MD.Value >= range.Start.Value).ToList()
                : range.End.HasValue
                    ? stations.Where(s => s.MD.Value <= range.End.Value).ToList()
                    : stations;

            SortStationData(entity.TrajectoryStation);

            if (maxDataNodes != null && entity.TrajectoryStation.Count > maxDataNodes.Value)
            {
                Logger.Debug($"Truncating trajectory stations with {entity.TrajectoryStation.Count}.");
                entity.TrajectoryStation = entity.TrajectoryStation.GetRange(0, maxDataNodes.Value);
                context.DataTruncated = true;
            }

            return entity.TrajectoryStation.Count;
        }

        private Range<double?> GetQueryIndexRange(WitsmlQueryParser parser)
        {
            if (parser == null)
                return new Range<double?>(null, null);

            var mdMn = parser.Properties("mdMn").FirstOrDefault()?.Value;
            var mdMx = parser.Properties("mdMx").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(mdMn) && string.IsNullOrEmpty(mdMx))
                return new Range<double?>(null, null);

            if (string.IsNullOrEmpty(mdMn))
                return new Range<double?>(null, double.Parse(mdMx));

            return string.IsNullOrEmpty(mdMx)
                ? new Range<double?>(double.Parse(mdMn), null)
                : new Range<double?>(double.Parse(mdMn), double.Parse(mdMx));
        }

        private void UpdateGrowingObject(Trajectory current, bool isHeaderUpdateOnly, bool? isAppending = null, double? startIndex = null, double? endIndex = null, string indexUom = null)
        {
            // Currently growing
            if (IsObjectGrowing(current))
            {
                return;
            }

            var changeHistory = AuditHistoryAdapter.GetCurrentChangeHistory();
            changeHistory.UpdatedHeader = true;

            // Currently not growing with header only update
            if (isHeaderUpdateOnly)
            {
                UpdateGrowingObject(current.GetUri());
                return;
            }

            // Currently not growing with start/end indexes changed
            AuditHistoryAdapter.SetChangeHistoryIndexes(changeHistory, startIndex, endIndex, indexUom);

            // Currently not growing with stations updated/appended/deleted
            var isObjectGrowingToggled = isAppending.GetValueOrDefault() ? true : (bool?)null;
            UpdateGrowingObject(current, null, isObjectGrowingToggled);
        }
    }
}
