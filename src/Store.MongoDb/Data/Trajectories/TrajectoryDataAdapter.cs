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
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for Trajectory objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TChild">The type of the child.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.MongoDbDataAdapter{T}" />
    public abstract class TrajectoryDataAdapter<T, TChild> : GrowingObjectDataAdapterBase<T> where T : IWellboreObject where TChild : IUniqueId
    {
        /// <summary>The field to query Mongo File</summary>
        private const string FileQueryField = "Uri";

        /// <summary>The file name</summary>
        private const string FileName = "FileName";

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryDataAdapter{T, TChild}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        protected TrajectoryDataAdapter(IContainer container, IDatabaseProvider databaseProvider, string dbCollectionName) : base(container, databaseProvider, dbCollectionName)
        {
            Logger.Verbose("Instance created.");
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>A collection of data objects retrieved from the data store.</returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {
            var isRequestingData = parser.IncludeTrajectoryStations();

            // If requesting data limit selection to Ids only for validation
            var entities = isRequestingData
                ? QueryEntities(parser.Clone(OptionsIn.ReturnElements.IdOnly))
                : QueryEntities(parser);

            if (isRequestingData)
            {
                ValidateGrowingObjectDataRequest(parser, entities);

                // Fetch using the projection fields
                entities = QueryEntities(parser);

                var headers = GetEntities(entities.Select(x => x.GetUri()))
                    .ToDictionary(x => x.GetUri());

                var isRangeQuery = parser.IsStructuralRangeQuery();
                var filtered = new List<T>();

                entities.ForEach(x =>
                {
                    var header = headers[x.GetUri()];

                    // Query the trajectory stations
                    var count = QueryTrajectoryStations(x, header, parser, context);

                    // Check for data being returned
                    if (isRangeQuery && count <= 0)
                    {
                        filtered.Add(x);
                    }
                });

                // Remove headers with no data
                filtered.ForEach(x => entities.Remove(x));
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                entities.ForEach(ClearTrajectoryStations);
            }

            return entities;
        }

        /// <summary>
        /// Gets a data object by the specified UUID.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <param name="fields">The requested fields.</param>
        /// <returns>The data object instance.</returns>
        public override T Get(EtpUri uri, params string[] fields)
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
        public override void Add(WitsmlQueryParser parser, T dataObject)
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
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be updated.</param>
        public override void Update(WitsmlQueryParser parser, T dataObject)
        {
            var uri = dataObject.GetUri();

            if (IsUpdatingStations(dataObject))
            {
                UpdateTrajectoryWithStations(parser, dataObject, uri, true);
            }
            else
            {
                using (var transaction = GetTransaction())
                {
                    transaction.SetContext(uri);
                    UpdateEntity(parser, uri);
                    UpdateGrowingObject(GetEntity(uri), true);               
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be replaced.</param>
        public override void Replace(WitsmlQueryParser parser, T dataObject)
        {
            var uri = dataObject.GetUri();

            if (!CanSaveData())
            {
                ClearTrajectoryStations(dataObject);
            }

            UpdateTrajectoryWithStations(parser, dataObject, uri);
        }

        /// <summary>
        /// Deletes or partially updates the specified object by uid.
        /// </summary>
        /// <param name="parser">The query parser that specifies the object.</param>
        public override void Delete(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<T>();

            if (parser.HasElements())
            {
                if (IsDeletingStations(parser))
                {
                    PartialDeleteTrajectoryWithStations(parser, uri);
                }
                else
                {
                    using (var transaction = GetTransaction())
                    {
                        transaction.SetContext(uri);
                        PartialDeleteEntity(parser, uri);
                        UpdateGrowingObject(GetEntity(uri), true);
                        transaction.Commit();
                    }                   
                }               
            }
            else
            {
                Delete(uri);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified identifier.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public override void Delete(EtpUri uri)
        {
            using (var transaction = GetTransaction())
            {
                Logger.Debug($"Deleting Trajectory with uri '{uri}'.");
                transaction.SetContext(uri);

                var current = GetEntity(uri);
                var chunked = IsQueryingStationFile(current, current);
                DeleteEntity(uri);

                if (chunked)
                {
                    var bucket = GetMongoFileBucket();
                    DeleteMongoFile(bucket, uri);
                }
   
                transaction.Commit();
            }
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
        /// Formats the trajectory station data.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The query parser.</param>
        /// <returns>A collection of formatted trajectory stations.</returns>
        protected virtual List<TChild> FormatStationData(T entity, WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();
            var stations = GetTrajectoryStations(entity);

            if (OptionsIn.ReturnElements.All.Equals(returnElements) ||
                OptionsIn.ReturnElements.DataOnly.Equals(returnElements) ||
                !parser.IncludeTrajectoryStations()) 
                return stations;

            var stationParser = parser
                .ForkProperties(ObjectTypes.TrajectoryStation, ObjectTypes.TrajectoryStation)
                .FirstOrDefault();

            if ((stationParser == null && !OptionsIn.ReturnElements.StationLocationOnly.Equals(returnElements)) ||
                (stationParser != null && !stationParser.HasElements() && !stationParser.Element().HasAttributes))
                return stations;

            const string prefix = "TrajectoryStation.";

            var fields = GetProjectionPropertyNames(parser)
                .Where(x => x.StartsWith(prefix))
                .Select(x => x.Substring(prefix.Length))
                .ToList();

            var mapper = new DataObjectMapper<TChild>(Container, stationParser, fields);
            return mapper.Map(stations);
        }

        /// <summary>
        /// Gets a list of the property names to project during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of property names.</returns>
        protected override List<string> GetProjectionPropertyNames(WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();

            return OptionsIn.ReturnElements.IdOnly.Equals(returnElements)
                ? new List<string> { IdPropertyName, NamePropertyName, "UidWell", "NameWell", "UidWellbore", "NameWellbore" }
                : OptionsIn.ReturnElements.StationLocationOnly.Equals(returnElements)
                ? new List<string>
                {
                    IdPropertyName, "UidWell", "UidWellbore", "TrajectoryStation.Uid",
                    "TrajectoryStation.DateTimeStn", "TrajectoryStation.TypeTrajStation",
                    "TrajectoryStation.MD", "TrajectoryStation.Tvd", "TrajectoryStation.Incl", "TrajectoryStation.Azi",
                    "TrajectoryStation.Location"
                }
                : parser.IncludeTrajectoryStations()
                ? new List<string> { IdPropertyName, "UidWell", "UidWellbore", "TrajectoryStation" }
                : OptionsIn.ReturnElements.Requested.Equals(returnElements)
                ? new List<string>()
                : null;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during a query.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForQuery(WitsmlQueryParser parser)
        {
            var ignored = new List<string> { "mdMn", "mdMx" };

            if (parser.IncludeTrajectoryStations())
                ignored.Add("trajectoryStation");

            return ignored;
        }

        /// <summary>
        /// Gets a list of the element names to ignore during an update.
        /// </summary>
        /// <param name="parser">The WITSML parser.</param>
        /// <returns>A list of element names.</returns>
        protected override List<string> GetIgnoredElementNamesForUpdate(WitsmlQueryParser parser)
        {
            return new List<string> { "mdMn", "mdMx", "objectGrowing" };
        }

        /// <summary>
        /// Gets the query index range.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <returns>the index range for the query.</returns>
        protected Range<double?> GetQueryIndexRange(WitsmlQueryParser parser)
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

        /// <summary>
        /// check if md is within the range.
        /// </summary>
        /// <param name="md">The md.</param>
        /// <param name="range">The range.</param>
        /// <returns>True is md is within the range; false otherwise.</returns>
        protected bool WithinRange(double md, Range<double?> range)
        {
            if (!range.Start.HasValue && !range.End.HasValue)
                return false;

            if (range.Start.HasValue)
            {
                return range.End.HasValue
                    ? md >= range.Start.Value && md <= range.End.Value
                    : md >= range.Start.Value;
            }

            return md <= range.End.Value;
        }

        /// <summary>
        /// Clears the trajectory stations.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected abstract void ClearTrajectoryStations(T entity);

        /// <summary>
        /// Filters the station data based on query parameters.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="context">The query context.</param>
        /// <returns>The count of trajectory stations after filtering.</returns>
        protected abstract int FilterStationData(T entity, List<TChild> stations, WitsmlQueryParser parser = null, IQueryContext context = null);

        /// <summary>
        /// Filters the station data with the query structural range.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The parser.</param>
        protected abstract void FilterStationData(T entity, WitsmlQueryParser parser);

        /// <summary>
        /// Check if need to query mongo file for station data.
        /// </summary>
        /// <param name="entity">The result data object.</param>
        /// <param name="header">The full header object.</param>
        /// <returns><c>true</c> if needs to query mongo file; otherwise, <c>false</c>.</returns>
        protected abstract bool IsQueryingStationFile(T entity, T header);

        /// <summary>
        /// Sets the MD index ranges.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="force">if set to <c>true</c> force the index range update.</param>
        protected abstract void SetIndexRange(T dataObject, WitsmlQueryParser parser, bool force = true);

        /// <summary>
        /// Gets the MD index ranges.
        /// </summary>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <returns>The start and end index range.</returns>
        protected abstract Range<double?> GetIndexRange(List<TChild> stations, out string uom);

        /// <summary>
        /// Sorts the stations by MD.
        /// </summary>
        /// <param name="stations">The trajectory stations.</param>
        protected abstract void SortStationData(List<TChild> stations);        

        /// <summary>
        /// Gets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <returns>The trajectory station collection.</returns>
        protected abstract List<TChild> GetTrajectoryStations(T dataObject);

        /// <summary>
        /// Sets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <returns>The trajectory.</returns>
        protected abstract T SetTrajectoryStations(T dataObject, List<TChild> stations);

        private bool IsUpdatingStations(T dataObject)
        {
            var stations = GetTrajectoryStations(dataObject);
            return stations.Any();
        }

        private bool IsDeletingStations(WitsmlQueryParser parser)
        {
            var range = GetQueryIndexRange(parser);

            if (range.Start.HasValue || range.End.HasValue)
                return true;

            var element = parser.Element();
            return element.Elements().Any(e => e.Name.LocalName == "trajectoryStation");
        }

        /// <summary>
        /// Saves trajectory stations data in mongo file if trajectory stations count exceeds maximun count; removes if not.
        /// </summary>
        /// <param name="entity">The data object.</param>
        /// <param name="deleteFile">if set to <c>true</c> delete file.</param>
        private void UpdateMongoFile(T entity, bool deleteFile = true)
        {
            var uri = entity.GetUri();
            Logger.Debug($"Updating MongoDb Trajectory Stations files: {uri}");

            var bucket = GetMongoFileBucket();
            var stations = GetTrajectoryStations(entity);

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

        private List<TChild> GetMongoFileStationData(string uri)
        {
            Logger.Debug("Getting MongoDb Trajectory Station files.");

            var bucket = GetMongoFileBucket();

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileQueryField], uri);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return null;

            var bytes = bucket.DownloadAsBytes(mongoFile.Id);
            var json = Encoding.UTF8.GetString(bytes);

            return BsonSerializer.Deserialize<List<TChild>>(json);
        }

        private int QueryTrajectoryStations(T entity, T header, WitsmlQueryParser parser, IQueryContext context)
        {
            var stations = GetTrajectoryStations(entity);
            var chunked = IsQueryingStationFile(entity, header);

            if (chunked)
            {
                var uri = entity.GetUri();
                stations = GetMongoFileStationData(uri);
            }

            SetTrajectoryStations(entity, stations);

            var ignored = parser.IsStructuralRangeQuery() ? new List<string> {"md"} : null;
            var query = new MongoDbQuery<T>(Container, GetCollection(), parser, null, ignored);
            query.Navigate(OptionsIn.ReturnElements.IdOnly.Value);
            query.FilterRecurringElements(entity.AsList());

            var count = FilterStationData(entity, stations, parser, context);
            SetIndexRange(entity, parser, false);
            FormatStationData(entity, parser);

            return count;
        }

        private void UpdateTrajectoryWithStations(WitsmlQueryParser parser, T dataObject, EtpUri uri, bool merge = false)
        {
            var current = GetEntity(uri);
            var chunked = IsQueryingStationFile(current, current);
            var stations = GetTrajectoryStations(dataObject);
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

                var savedStations = GetTrajectoryStations(current)
                    .Select(x => x.Uid)
                    .ToArray();

                isAppending = GetTrajectoryStations(dataObject)
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

        private void PartialDeleteTrajectoryWithStations(WitsmlQueryParser parser, EtpUri uri)
        {
            var current = GetEntity(uri);
            var chunked = IsQueryingStationFile(current, current);

            if (chunked)
            {
                var stations = GetMongoFileStationData(uri);
                FilterStationData(current, stations);
            }

            var stationsCurrent = new List<TChild>(GetTrajectoryStations(current));

            FilterStationData(current, parser);
            MergeEntity(current, parser, true);

            var stationsAfterMerge = GetTrajectoryStations(current).Select(s => s.Uid).ToArray();
            var stationsDeleted = stationsCurrent.FindAll(s => !stationsAfterMerge.ContainsIgnoreCase(s.Uid));

            string uomIndex;
            var rangeIn = GetIndexRange(stationsDeleted, out uomIndex);
            
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(uri);

                if (chunked)
                {
                    var bucket = GetMongoFileBucket();
                    DeleteMongoFile(bucket, uri);
                }

                SetIndexRange(current, parser);
                UpdateMongoFile(current, false);
                ReplaceEntity(current, uri, false);
                UpdateGrowingObject(current, false, null, rangeIn.Start, rangeIn.End, uomIndex);
                transaction.Commit();
            }
        }

        private void UpdateGrowingObject(T current, bool isHeaderUpdateOnly, bool? isAppending = null, double? startIndex = null, double? endIndex = null, string indexUom = null)
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
