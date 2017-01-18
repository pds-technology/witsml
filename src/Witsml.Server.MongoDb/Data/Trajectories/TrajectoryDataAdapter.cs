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
using System.Text;
using Energistics.DataAccess;
using Energistics.Datatypes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// MongoDb data adapter that encapsulates CRUD functionality for Trajectory objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TChild">The type of the child.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{T}" />
    public abstract class TrajectoryDataAdapter<T, TChild> : MongoDbDataAdapter<T> where T : IWellboreObject where TChild : IUniqueId
    {
        /// <summary>
        /// The field to query Mongo File
        /// </summary>
        private const string FileQueryField = "Uri";

        /// <summary>
        /// The file name
        /// </summary>
        private const string FileName = "FileName";

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryDataAdapter{T, TChild}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        protected TrajectoryDataAdapter(IContainer container, IDatabaseProvider databaseProvider, string dbCollectionName) : base(container, databaseProvider, dbCollectionName)
        {
            Logger.Debug("Instance created.");
        }

        /// <summary>
        /// Retrieves data objects from the data store using the specified parser.
        /// </summary>
        /// <param name="parser">The query template parser.</param>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// A collection of data objects retrieved from the data store.
        /// </returns>
        public override List<T> Query(WitsmlQueryParser parser, ResponseContext context)
        {          
            var entities = QueryEntities(parser);

            if (parser.IncludeTrajectoryStations())
            {
                ValidateGrowingObjectDataRequest(parser, entities);

                var headers = GetEntities(entities.Select(x => x.GetUri()))
                    .ToDictionary(x => x.GetUri());

                entities.ForEach(x =>
                {
                    var header = headers[x.GetUri()];

                    //Query the trajectory stations
                    QueryTrajectoryStations(x, header, parser);
                });
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                entities.ForEach(ClearTrajectoryStations);
            }

            return entities;
        }

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object to be added.</param>
        public override void Add(WitsmlQueryParser parser, T dataObject)
        {
            using (var transaction = GetTransaction())
            {
                transaction.SetContext(dataObject.GetUri());
                SetIndexRange(dataObject, parser);
                UpdateMongoFile(dataObject, false);
                InsertEntity(dataObject);
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

            if (UpdateStations(dataObject))
            {
                UpdateTrajectoryWithStations(parser, dataObject, uri, true);
            }
            else
            {
                using (var transaction = GetTransaction())
                {
                    transaction.SetContext(uri);
                    UpdateEntity(parser, uri);
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
                if (DeleteStations(parser))
                {
                    PartialDeleteTrajectoryWithStations(parser, uri);
                }
                else
                {
                    using (var transaction = GetTransaction())
                    {
                        transaction.SetContext(uri);
                        PartialDeleteEntity(parser, uri);
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
                Logger.DebugFormat($"Deleting Trajectory with uri '{uri}'.");
                transaction.SetContext(uri);

                var current = GetEntity(uri);
                var chunked = QueryStationFile(current, current);
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
                    IdPropertyName, "UidWell", "UidWellbore", "TrajectoryStation.DateTimeStn", "TrajectoryStation.TypeTrajStation",
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
            var ignored = new List<string> {"mdMn", "mdMx"};

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
            return GetIgnoredElementNamesForQuery(parser)
                .Concat(new[]
                {
                    "objectGrowing", "mdMn", "mdMx"
                })
                .ToList();
        }

        /// <summary>
        /// Saves trajectory stations data in mongo file if trajectory stations count exceeds maximun count; removes if not.
        /// </summary>
        /// <param name="entity">The data object.</param>
        /// <param name="deleteFile">if set to <c>true</c> delete file.</param>
        private void UpdateMongoFile(T entity, bool deleteFile = true)
        {
            var uri = entity.GetUri();
            Logger.DebugFormat($"Updating MongoDb Trajectory Stations files: {uri}");

            var bucket = GetMongoFileBucket();
            var stations = GetTrajectoryStation(entity);

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
            Logger.DebugFormat($"Deleting MongoDb Channel Data file: {fileId}");

            var filter = Builders<GridFSFileInfo>.Filter.Eq(fi => fi.Metadata[FileQueryField], fileId);
            var mongoFile = bucket.Find(filter).FirstOrDefault();

            if (mongoFile == null)
                return;

            bucket.Delete(mongoFile.Id);
        }

        private void QueryTrajectoryStations(T entity, T header, WitsmlQueryParser parser)
        {
            var stations = GetTrajectoryStation(entity);

            if (QueryStationFile(entity, header))
            {
                var uri = entity.GetUri();
                stations = GetMongoFileStationData(uri);
            }

            FormatStationData(entity, stations, parser);
            SetIndexRange(entity, parser, false);
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
        /// Formats the station data based on query parameters.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="parser">The parser.</param>
        protected abstract void FormatStationData(T entity, List<TChild> stations, WitsmlQueryParser parser = null);

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
        protected abstract bool QueryStationFile(T entity, T header);

        /// <summary>
        /// Sets the MD index ranges.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="force">if set to <c>true</c> force the index range update.</param>
        protected abstract void SetIndexRange(T dataObject, WitsmlQueryParser parser, bool force = true);

        /// <summary>
        /// Sorts the stations by MD.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected abstract void SortStationData(T dataObject);

        /// <summary>
        /// Gets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <returns>The trajectory station collection.</returns>
        protected abstract List<TChild> GetTrajectoryStation(T dataObject);

        private bool UpdateStations(T dataObject)
        {
            var stations = GetTrajectoryStation(dataObject);
            return stations.Any();
        }

        private bool DeleteStations(WitsmlQueryParser parser)
        {
            var range = GetQueryIndexRange(parser);
            if (range.Start.HasValue || range.End.HasValue)
                return true;

            var element = parser.Element();
            return element.Elements().Any(e => e.Name.LocalName == "trajectoryStation");
        }

        private void UpdateTrajectoryWithStations(WitsmlQueryParser parser, T dataObject, EtpUri uri, bool merge = false)
        {
            var current = GetEntity(uri);
            var chunked = QueryStationFile(current, current);

            if (merge)
            {
                if (chunked)
                {
                    var stations = GetMongoFileStationData(uri);
                    FormatStationData(current, stations);
                }

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
                ReplaceEntity(dataObject, uri);
                transaction.Commit();
            }
        }

        private void PartialDeleteTrajectoryWithStations(WitsmlQueryParser parser, EtpUri uri)
        {
            var current = GetEntity(uri);
            var chunked = QueryStationFile(current, current);

            if (chunked)
            {
                var stations = GetMongoFileStationData(uri);
                FormatStationData(current, stations);
            }

            FilterStationData(current, parser);
            MergeEntity(current, parser, true);

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
                ReplaceEntity(current, uri);
                transaction.Commit();
            }
        }
    }
}
