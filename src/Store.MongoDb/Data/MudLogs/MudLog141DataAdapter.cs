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


namespace PDS.WITSMLstudio.Store.Data.MudLogs
{
    /// <summary>
    /// Data adapter that encapsulates CRUD functionality for <see cref="MudLog" />
    /// </summary>
    [Export141(ObjectTypes.MudLog, typeof(IGrowingObjectDataAdapter))]
    public partial class MudLog141DataAdapter
    {
        /// <summary>
        /// Formats the mudlog geology interval data.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The query parser.</param>
        /// <returns>A collection of formatted mudlog geology intervals.</returns>
        protected override List<GeologyInterval> FormatGeologyIntervalData(MudLog entity, WitsmlQueryParser parser)
        {
            entity.GeologyInterval = base.FormatGeologyIntervalData(entity, parser);
            return entity.GeologyInterval;
        }


        /// <summary>
        /// Filters the geology interval data based on query parameters.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="geologyIntervals">The mudlog geology intervals.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="context">The query context.</param>
        /// <returns>The count of mudlog geology intervals after filtering.</returns>
        protected override int FilterGeologyIntervalData(MudLog entity, List<GeologyInterval> geologyIntervals, WitsmlQueryParser parser = null, IQueryContext context = null)
        {
            if (geologyIntervals == null || geologyIntervals.Count == 0)
                return 0;

            var range = GetQueryIndexRange(parser);
            var maxDataNodes = context?.MaxDataNodes;

            switch (parser?.IntervalRangeInclusion())
            {
                default:
                    entity.GeologyInterval = range.Start.HasValue
                ? range.End.HasValue
                    ? geologyIntervals.Where(s => s.MDTop.Value >= range.Start.Value && s.MDTop.Value <= range.End.Value).ToList()
                    : geologyIntervals.Where(s => s.MDTop.Value >= range.Start.Value).ToList()
                : range.End.HasValue
                    ? geologyIntervals.Where(s => s.MDTop.Value <= range.End.Value).ToList()
                    : geologyIntervals;
                    break;
                case "whole-interval":
                    entity.GeologyInterval = range.Start.HasValue
                ? range.End.HasValue
                    ? geologyIntervals.Where(s => s.MDTop.Value >= range.Start.Value && s.MDBottom.Value <= range.End.Value).ToList()
                    : geologyIntervals.Where(s => s.MDTop.Value >= range.Start.Value).ToList()
                : range.End.HasValue
                    ? geologyIntervals.Where(s => s.MDBottom.Value <= range.End.Value).ToList()
                    : geologyIntervals;
                    break;
                case "any-part":
                    entity.GeologyInterval = range.Start.HasValue
                ? range.End.HasValue
                    ? geologyIntervals.Where(s => s.MDBottom.Value >= range.Start.Value && s.MDTop.Value <= range.End.Value).ToList()
                    : geologyIntervals.Where(s => s.MDBottom.Value >= range.Start.Value).ToList()
                : range.End.HasValue
                    ? geologyIntervals.Where(s => s.MDBottom.Value <= range.End.Value).ToList()
                    : geologyIntervals;
                    break;
            }

            SortGeologyIntervalData(entity.GeologyInterval);

            if (maxDataNodes != null && entity.GeologyInterval.Count > maxDataNodes.Value)
            {
                Logger.Debug($"Truncating mudlog geology intervals with {entity.GeologyInterval.Count}.");
                entity.GeologyInterval = entity.GeologyInterval.GetRange(0, maxDataNodes.Value);
                context.DataTruncated = true;
            }

            return entity.GeologyInterval.Count;
        }

        /// <summary>
        /// Filters the geology interval data with the query structural range.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="parser">The parser.</param>
        protected override void FilterGeologyIntervalData(MudLog entity, WitsmlQueryParser parser)
        {
            if (!entity.GeologyInterval.Any())
                return;

            var range = GetQueryIndexRange(parser);
            entity.GeologyInterval.RemoveAll(s => WithinRange(s.MDTop.Value, range));
        }

        /// <summary>
        /// Check if need to query mongo file for geology interval data.
        /// </summary>
        /// <param name="entity">The result data object.</param>
        /// <param name="header">The full header object.</param>
        /// <returns><c>true</c> if needs to query mongo file; otherwise, <c>false</c>.</returns>
        protected override bool IsQueryingGeologyIntervalFile(MudLog entity, MudLog header)
        {
            return header.StartMD != null && entity.GeologyInterval == null;
        }

        /// <summary>
        /// Sets the md index ranges.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="parser">The parser.</param>
        /// <param name="force">if set to <c>true</c> force the index range update.</param>
        protected override void SetIndexRange(MudLog dataObject, WitsmlQueryParser parser, bool force = true)
        {
            Logger.Debug("Set mudlog MD ranges.");

            if (dataObject.GeologyInterval == null || dataObject.GeologyInterval.Count <= 0)
            {
                dataObject.StartMD = null;
                dataObject.EndMD = null;
                return;
            }

            SortGeologyIntervalData(dataObject.GeologyInterval);

            var returnElements = parser.ReturnElements();
            var alwaysInclude = force ||
                                OptionsIn.ReturnElements.All.Equals(returnElements) ||
                                OptionsIn.ReturnElements.HeaderOnly.Equals(returnElements);

            if (alwaysInclude || parser.Contains("startMd"))
            {
                dataObject.StartMD = dataObject.GeologyInterval.First().MDTop;
            }

            if (alwaysInclude || parser.Contains("endMd"))
            {
                dataObject.EndMD = dataObject.GeologyInterval.Last().MDBottom;
            }
        }

        /// <summary>
        /// Gets the MD index ranges.
        /// </summary>
        /// <param name="geologyIntervals">The mudlog geology intervals.</param>
        /// <param name="uom">The unit of measure.</param>
        /// <returns>The start and end index range.</returns>
        protected override Range<double?> GetIndexRange(List<GeologyInterval> geologyIntervals, out string uom)
        {
            uom = string.Empty;

            if (geologyIntervals == null || geologyIntervals.Count == 0)
                return new Range<double?>(null, null);

            SortGeologyIntervalData(geologyIntervals);

            var mdTop = geologyIntervals.First().MDTop;
            var mdBottom = geologyIntervals.Last().MDBottom;
            uom = mdTop?.Uom.ToString() ?? string.Empty;

            return new Range<double?>(mdTop?.Value, mdBottom?.Value);
        }

        /// <summary>
        /// Sorts the geology intervals by MdTop.
        /// </summary>
        /// <param name="geologyIntervals">The mudlog geology intervals.</param>
        protected override void SortGeologyIntervalData(List<GeologyInterval> geologyIntervals)
        {
            // Sort geology intervals by MD
            geologyIntervals.Sort((x, y) => (x.MDTop?.Value ?? -1).CompareTo(y.MDTop?.Value ?? -1));
        }

        /// <summary>
        /// Gets the mudlog geology intervals.
        /// </summary>
        /// <param name="dataObject">The mudlog data object.</param>
        /// <returns>The mudlog geology intervals collection.</returns>
        protected override List<GeologyInterval> GetGeologyIntervals(MudLog dataObject)
        {
            return dataObject.GeologyInterval;
        }

        /// <summary>
        /// Sets the mudlog geology interval.
        /// </summary>
        /// <param name="dataObject">The mudlog data object.</param>
        /// <param name="geologyIntervals">The mudlog geology intervals.</param>
        /// <returns>The mudlog.</returns>
        protected override MudLog SetGeologyIntervals(MudLog dataObject, List<GeologyInterval> geologyIntervals)
        {
            dataObject.GeologyInterval = geologyIntervals;
            return dataObject;
        }

        /// <summary>
        /// Clears the mudlog geology intervals.
        /// </summary>
        /// <param name="entity">The entity.</param>
        protected override void ClearGeologyIntervals(MudLog entity)
        {
            entity.GeologyInterval = null;
        }

        /// <summary>
        /// Determines whether the objectGrowing flag is true for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   <c>true</c> if the objectGrowing flag is true for the specified entity; otherwise, <c>false</c>.
        /// </returns>
        protected override bool IsObjectGrowing(MudLog entity)
        {
            return entity.ObjectGrowing.GetValueOrDefault();
        }
    }
}
