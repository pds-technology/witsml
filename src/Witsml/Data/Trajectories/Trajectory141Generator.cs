//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;

namespace PDS.Witsml.Data.Trajectories
{
    /// <summary>
    /// Generates data for a 141 Trajectory.
    /// </summary>
    /// <seealso cref="PDS.Witsml.Data.DataGenerator" />
    public class Trajectory141Generator : DataGenerator
    {
        private const string StationUidPrefix = "sta-";
        private const MeasuredDepthUom MdUom = MeasuredDepthUom.m;
        private const WellVerticalCoordinateUom TvdUom = WellVerticalCoordinateUom.m;
        private const PlaneAngleUom AngleUom = PlaneAngleUom.dega;

        /// <summary>
        /// Generations trajectory station data.
        /// </summary>
        /// <param name="numOfStations">The number of stations.</param>
        /// <param name="startMd">The start md.</param>
        /// <param name="mdUom">The MD index uom.</param>
        /// <param name="tvdUom">The Tvd uom.</param>
        /// <param name="angleUom">The angle uom.</param>
        /// <returns>The trajectoryStation collection.</returns>
        public List<TrajectoryStation> GenerationStations(int numOfStations, double startMd, MeasuredDepthUom mdUom = MdUom, WellVerticalCoordinateUom tvdUom = TvdUom, PlaneAngleUom angleUom = AngleUom)
        {
            var stations = new List<TrajectoryStation>();
            var random = new Random(numOfStations*2);

            for (var i = 0; i < numOfStations; i++)
            {
                var station = new TrajectoryStation
                {
                    Uid = StationUidPrefix + (i + 1),
                    TypeTrajStation = TrajStationType.tieinpoint,
                    MD = new MeasuredDepthCoord {Uom = mdUom, Value = startMd},
                    Tvd = new WellVerticalDepthCoord() {Uom = tvdUom, Value = startMd + 0.5},
                    Azi = new PlaneAngleMeasure {Uom = angleUom, Value = random.NextDouble()},
                    Incl = new PlaneAngleMeasure {Uom = angleUom, Value = random.NextDouble()}
                };
                stations.Add(station);
                startMd++;
            }

            return stations;
        }
    }
}
