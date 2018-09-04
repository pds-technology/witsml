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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Store.Data.GrowingObjects;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="Trajectory" />
    /// </summary>
    [Export141(ObjectTypes.Trajectory, typeof(IGrowingObjectDataAdapter))]
    public partial class Trajectory141DataAdapter
    {
        /// <summary>
        /// Formats the trajectory station data.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The query parser.</param>
        /// <returns>A collection of formatted trajectory stations.</returns>
        protected override List<TrajectoryStation> FormatStationData(Trajectory entity, WitsmlQueryParser parser)
        {
            entity.TrajectoryStation = base.FormatStationData(entity, parser);
            return entity.TrajectoryStation;
        }

        /// <summary>
        /// Filters the station data based on query parameters.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="context">The query context.</param>
        /// <returns>The count of trajectory stations after filtering.</returns>
        protected override int FilterStationData(Trajectory entity, List<TrajectoryStation> stations, WitsmlQueryParser parser = null, IQueryContext context = null)
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
        protected override bool IsQueryingStationFile(Trajectory entity, Trajectory header)
        {
            return header.MDMin != null && entity.TrajectoryStation == null;
        }

        /// <summary>
        /// Sets the MD index ranges.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="force">if set to <c>true</c> force the index range update.</param>
        protected override void SetIndexRange(Trajectory dataObject, WitsmlQueryParser parser, bool force = true)
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

        /// <summary>
        /// Gets the MD index ranges.
        /// </summary>
        /// <param name="stations">The trajectory stations.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <returns>The start and end index range.</returns>
        protected override Range<double?> GetIndexRange(List<TrajectoryStation> stations, out string uom)
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

        /// <summary>
        /// Sorts the stations by MD.
        /// </summary>
        /// <param name="stations">The trajectory stations.</param>
        protected override void SortStationData(List<TrajectoryStation> stations)
        {
            // Sort stations by MD
            stations.Sort((x, y) => (x.MD?.Value ?? -1).CompareTo(y.MD?.Value ?? -1));
        }

        /// <summary>
        /// Gets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <returns>The trajectory station collection.</returns>
        protected override List<TrajectoryStation> GetTrajectoryStations(Trajectory dataObject)
        {
            return dataObject.TrajectoryStation;
        }

        /// <summary>
        /// Sets the trajectory station.
        /// </summary>
        /// <param name="dataObject">The trajectory data object.</param>
        /// <param name="stations">The trajectory stations.</param>
        /// <returns>The trajectory.</returns>
        protected override Trajectory SetTrajectoryStations(Trajectory dataObject, List<TrajectoryStation> stations)
        {
            dataObject.TrajectoryStation = stations;
            return dataObject;
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
        /// Determines whether the objectGrowing flag is true for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   <c>true</c> if the objectGrowing flag is true for the specified entity; otherwise, <c>false</c>.
        /// </returns>
        protected override bool IsObjectGrowing(Trajectory entity)
        {
            return entity.ObjectGrowing.GetValueOrDefault();
        }
    }
}
