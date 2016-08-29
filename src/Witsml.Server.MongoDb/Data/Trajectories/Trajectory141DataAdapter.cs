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
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Trajectory" />
    /// </summary>
    /// <seealso cref="PDS.Witsml.Server.Data.MongoDbDataAdapter{Trajectory}" />
    [Export(typeof(IWitsmlDataAdapter<Trajectory>))]
    [Export(typeof(IWitsml141Configuration))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Trajectory141DataAdapter : TrajectoryDataAdapter<Trajectory, TrajectoryStation>, IWitsml141Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Trajectory141DataAdapter" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="databaseProvider">The database provider.</param>
        [ImportingConstructor]
        public Trajectory141DataAdapter(IContainer container, IDatabaseProvider databaseProvider) : base(container, databaseProvider, ObjectNames.Trajectory141)
        {
        }

        /// <summary>
        /// Gets the supported capabilities for the <see cref="Trajectory"/> object.
        /// </summary>
        /// <param name="capServer">The capServer object.</param>
        public void GetCapabilities(CapServer capServer)
        {
            Logger.DebugFormat("Getting the supported capabilities for Trajectory data version {0}.", capServer.Version);

            capServer.Add(Functions.GetFromStore, ObjectTypes.Trajectory);
            capServer.Add(Functions.AddToStore, ObjectTypes.Trajectory);
            capServer.Add(Functions.UpdateInStore, ObjectTypes.Trajectory);
            capServer.Add(Functions.DeleteFromStore, ObjectTypes.Trajectory);
        }

        /// <summary>
        /// Adds a <see cref="Trajectory"/> object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The <see cref="Log" /> to be added.</param>
        public override void Add(WitsmlQueryParser parser, Trajectory dataObject)
        {
            using (var transaction = DatabaseProvider.BeginTransaction())
            {
                SetMdValues(dataObject);
                UpdateMongoFile(dataObject, dataObject.TrajectoryStation, false);
                InsertEntity(dataObject, transaction);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Clears the trajectory stations.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected override void ClearTrajectoryStations(Trajectory entity)
        {
            entity.TrajectoryStation = null;
        }

        /// <summary>
        /// Formats the station data based on query parameters.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="parser">The parser.</param>
        protected override void FormatStationData(Trajectory entity, List<TrajectoryStation> stations, WitsmlQueryParser parser)
        {
            if (stations.Count > 0)
                entity.TrajectoryStation = stations;
        }

        /// <summary>
        /// Determines whether the current trajectory has station data.
        /// </summary>
        /// <param name="header">The trajectory.</param>
        /// <returns>
        ///   <c>true</c> if the specified trajectory has data; otherwise, <c>false</c>.
        /// </returns>
        protected override bool HasData(Trajectory header)
        {
            return header.MDMax != null;
        }

        /// <summary>
        /// Check if need to query mongo file for station data.
        /// </summary>
        /// <param name="entity">The result data object.</param>
        /// <param name="header">The full header object.</param>
        /// <returns><c>true</c> if needs to query mongo file; otherwise, <c>false</c>.</returns>
        protected override bool QueryStationFile(Trajectory entity, Trajectory header)
        {
            return header.MDMin != null && entity.TrajectoryStation == null;
        }

        private void SetMdValues(Trajectory dataObject)
        {
            Logger.Debug("Set trajectory MD ranges.");

            if (dataObject.TrajectoryStation.Count <= 0)
                return;

            var mds = dataObject.TrajectoryStation.Select(t => t.MD).OrderBy(m => m.Value).ToList();

            dataObject.MDMin = mds.FirstOrDefault();
            dataObject.MDMax = mds.LastOrDefault();
        }
    }
}
