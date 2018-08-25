//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;

namespace PDS.WITSMLstudio.Data.Trajectories
{
    /// <summary>
    /// Generates data for a 141 Trajectory.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataGenerator" />
    public class Trajectory141Generator : DataGenerator
    {
        /// <summary>
        /// The default Md uom
        /// </summary>
        public const MeasuredDepthUom MdUom = MeasuredDepthUom.m;
        /// <summary>
        /// The default Tvd uom
        /// </summary>
        public const WellVerticalCoordinateUom TvdUom = WellVerticalCoordinateUom.m;
        /// <summary>
        /// The default Angle uom
        /// </summary>
        public const PlaneAngleUom AngleUom = PlaneAngleUom.dega;

        private const string StationUidPrefix = "sta-";
        private const string LocationUidPrefix = "loc-";

        /// <summary>
        /// Generations trajectory station data.
        /// </summary>
        /// <param name="numOfStations">The number of stations.</param>
        /// <param name="startMd">The start md.</param>
        /// <param name="mdUom">The MD index uom.</param>
        /// <param name="tvdUom">The Tvd uom.</param>
        /// <param name="angleUom">The angle uom.</param>
        /// <param name="inCludeExtra">True if to generate extra information for trajectory station.</param>
        /// <returns>The trajectoryStation collection.</returns>
        public List<TrajectoryStation> GenerationStations(int numOfStations, double startMd, MeasuredDepthUom mdUom = MdUom, WellVerticalCoordinateUom tvdUom = TvdUom, PlaneAngleUom angleUom = AngleUom, bool inCludeExtra = false)
        {
            var stations = new List<TrajectoryStation>();
            var random = new Random(numOfStations * 2);

            for (var i = 0; i < numOfStations; i++)
            {
                string uidPrefix = (i + 1).ToString();
                var station = new TrajectoryStation
                {
                    Uid = StationUidPrefix + uidPrefix,
                    TypeTrajStation = i == 0 ? TrajStationType.tieinpoint : TrajStationType.magneticMWD,
                    MD = new MeasuredDepthCoord { Uom = mdUom, Value = startMd },
                    Tvd = new WellVerticalDepthCoord() { Uom = tvdUom, Value = startMd == 0 ? 0 : startMd - 0.1 },
                    Azi = new PlaneAngleMeasure { Uom = angleUom, Value = startMd == 0 ? 0 : random.NextDouble() },
                    Incl = new PlaneAngleMeasure { Uom = angleUom, Value = startMd == 0 ? 0 : random.NextDouble() },
                    DateTimeStn = DateTimeOffset.UtcNow,
                    Location = new List<Location> { Location(LocationUidPrefix + 1, random.NextDouble(), "ED" + uidPrefix) }
                };

                if (inCludeExtra)
                {
                    station.Mtf = new PlaneAngleMeasure { Uom = angleUom, Value = random.NextDouble() };
                    station.MDDelta = new LengthMeasure { Uom = LengthUom.m, Value = 0 };
                    station.StatusTrajStation = TrajStationStatus.position;
                }
                stations.Add(station);
                startMd++;
            }

            return stations;
        }

        /// <summary>
        /// Trajectory station location.
        /// </summary>
        /// <returns></returns>
        public Location Location(string uid, double coordinateValue, string wellCrsValue)
        {
            return new Location
            {
                Uid = uid,
                WellCRS = new RefNameString { UidRef = "proj1", Value = wellCrsValue },
                Easting = new LengthMeasure(coordinateValue * 5, LengthUom.m),
                Northing = new LengthMeasure(coordinateValue * 12, LengthUom.m),
            };
        }
    }
}
