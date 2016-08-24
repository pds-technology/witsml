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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.Datatypes;
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
        /// Initializes a new instance of the <see cref="TrajectoryDataAdapter{T, TChild}"/> class.
        /// </summary>
        /// <param name="databaseProvider">The database provider.</param>
        /// <param name="dbCollectionName">Name of the database collection.</param>
        protected TrajectoryDataAdapter(IDatabaseProvider databaseProvider, string dbCollectionName) : base(databaseProvider, dbCollectionName)
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
                    // TODO: Implement trajectory station range query, if requested
                    //var header = headers[x.GetUri()];

                    // Query the trajectory stations
                    //QueryTrajectoryStations(x, header, parser, context);
                });
            }
            else if (!OptionsIn.RequestObjectSelectionCapability.True.Equals(parser.RequestObjectSelectionCapability()))
            {
                entities.ForEach(ClearTrajectoryStations);
            }

            return entities;
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public override List<T> GetAll(EtpUri? parentUri = null)
        {
            Logger.DebugFormat("Fetching all Trajectorys; Parent URI: {0}", parentUri);

            return GetAllQuery(parentUri)
                .OrderBy(x => x.Name)
                .ToList();
        }

        /// <summary>
        /// Gets an <see cref="IQueryable{T}" /> instance to by used by the GetAll method.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>An executable query.</returns>
        protected override IQueryable<T> GetAllQuery(EtpUri? parentUri)
        {
            var query = GetQuery().AsQueryable();

            if (parentUri != null)
            {
                var ids = parentUri.Value.GetObjectIds().ToDictionary(x => x.ObjectType, y => y.ObjectId);
                var uidWellbore = ids[ObjectTypes.Wellbore];
                var uidWell = ids[ObjectTypes.Well];

                query = query.Where(x => x.UidWell == uidWell && x.UidWellbore == uidWellbore);
            }

            return query;
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
            return new List<string> { "mdMn", "mdMx" };
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
                    "objectGrowing"
                })
                .ToList();
        }

        /// <summary>
        /// Clears the trajectory stations.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected abstract void ClearTrajectoryStations(T entity);
    }
}
