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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Trajectory" />
    /// </summary>
    public partial class Trajectory141DataAdapter
    {
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
        protected override void FormatStationData(Trajectory entity, List<TrajectoryStation> stations, WitsmlQueryParser parser = null)
        {
            if (stations == null || stations.Count == 0)
                return;

            var range = GetQueryIndexRange(parser);

            entity.TrajectoryStation = range.Start.HasValue
                ? range.End.HasValue
                    ? stations.Where(s => s.MD.Value >= range.Start.Value && s.MD.Value <= range.End.Value).ToList()
                    : stations.Where(s => s.MD.Value >= range.Start.Value).ToList()
                : range.End.HasValue
                    ? stations.Where(s => s.MD.Value <= range.End.Value).ToList()
                    : stations;
        }

        /// <summary>
        /// Filters the station data with the query structural range.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The parser.</param>
        protected override void FilterStationData(Trajectory entity, WitsmlQueryParser parser)
        {
            if (!entity.TrajectoryStation.Any())
                return;

            var range = GetQueryIndexRange(parser);
            entity.TrajectoryStation.RemoveAll(s => WithinRange(s.MD.Value, range));
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

        /// <summary>
        /// Sets the MD index ranges.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SetIndexRange(Trajectory dataObject)
        {
            Logger.Debug("Set trajectory MD ranges.");

            if (dataObject.TrajectoryStation == null || dataObject.TrajectoryStation.Count <= 0)
            {
                dataObject.MDMin = null;
                dataObject.MDMax = null;
                return;
            }

            SortStationData(dataObject);

            dataObject.MDMin = dataObject.TrajectoryStation.FirstOrDefault()?.MD;
            dataObject.MDMax = dataObject.TrajectoryStation.LastOrDefault()?.MD;
        }

        /// <summary>
        /// Sorts the stations by MD.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected override void SortStationData(Trajectory dataObject)
        {
            // Sort stations by MD
            dataObject.TrajectoryStation = dataObject.TrajectoryStation.OrderBy(x => x.MD.Value).ToList();
        }

        /// <summary>
        /// Gets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <returns>The trajectory station collection.</returns>
        protected override List<TrajectoryStation> GetTrajectoryStation(Trajectory dataObject)
        {
            return dataObject.TrajectoryStation;
        }
    }
}
