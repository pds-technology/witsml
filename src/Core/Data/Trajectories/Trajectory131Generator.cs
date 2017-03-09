//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;


namespace PDS.WITSMLstudio.Data.Trajectories
{
    /// <summary>
    /// Generates data for a 131 Trajectory.
    /// </summary>
    public class Trajectory131Generator
    {
        /// <summary>
        /// The default Md uom
        /// </summary>
        public const MeasuredDepthUom MdUom = MeasuredDepthUom.m;

        private const string StationUidPrefix = "sta-";       
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
        /// <param name="includeExtra">True if to generate extra information for trajectory station.</param>
        /// <returns>The trajectoryStation collection.</returns>
        public List<TrajectoryStation> GenerationStations(int numOfStations, double startMd, MeasuredDepthUom mdUom = MdUom, WellVerticalCoordinateUom tvdUom = TvdUom, PlaneAngleUom angleUom = AngleUom, bool includeExtra = false)
        {
            var stations = new List<TrajectoryStation>();
            var random = new Random(numOfStations * 2);
            var now = DateTimeOffset.Now.AddDays(-1);

            for (var i = 0; i < numOfStations; i++)
            {
                var station = new TrajectoryStation
                {
                    Uid = StationUidPrefix + (i + 1),
                    TypeTrajStation = i == 0 ? TrajStationType.tieinpoint : TrajStationType.magneticMWD,
                    MD = new MeasuredDepthCoord { Uom = mdUom, Value = startMd },
                    Tvd = new WellVerticalDepthCoord() { Uom = tvdUom, Value = startMd == 0 ? 0 : startMd - 0.1 },
                    Azi = new PlaneAngleMeasure { Uom = angleUom, Value = startMd == 0 ? 0 : random.NextDouble() },
                    Incl = new PlaneAngleMeasure { Uom = angleUom, Value = startMd == 0 ? 0 : random.NextDouble() },
                    DateTimeStn = now.AddMinutes(i)
                };

                if (includeExtra)
                {
                    station.Mtf = new PlaneAngleMeasure { Uom = angleUom, Value = random.NextDouble() };
                    station.MDDelta = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 0 };
                    station.StatusTrajStation = TrajStationStatus.position;
                }

                stations.Add(station);
                startMd++;
            }

            return stations;
        }
    }
}
