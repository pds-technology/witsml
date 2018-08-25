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
using PDS.WITSMLstudio.Store.Data;

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Defines static helper methods for <see cref="ResponseContext"/>
    /// </summary>
    public static class ResponseExtensions
    {
        /// <summary>
        /// Initializes the ResponseContext with the specified parser.
        /// </summary>
        /// <param name="parser">The parser.</param>
        public static ResponseContext ToContext(this WitsmlQueryParser parser)
        {
            var context = new ResponseContext() { ObjectType = parser.ObjectType };

            if (IsGrowingDataObject(context))
            {
                context.MaxReturnNodes = parser.MaxReturnNodes();
                context.RequestLatestValues = parser.RequestLatestValues();

                var objectMaxDataNodesGet = 0;
                switch (context.ObjectType)
                {
                    case ObjectTypes.Log:
                        objectMaxDataNodesGet = WitsmlSettings.LogMaxDataNodesGet;
                        break;
                    case ObjectTypes.Trajectory:
                        objectMaxDataNodesGet = WitsmlSettings.TrajectoryMaxDataNodesGet;
                        break;
                    case ObjectTypes.MudLog:
                        objectMaxDataNodesGet = WitsmlSettings.MudLogMaxDataNodesGet;
                        break;
                }

                context.MaxDataNodes = context.MaxReturnNodes.HasValue
                    ? Math.Min(context.MaxReturnNodes.Value, objectMaxDataNodesGet)
                    : objectMaxDataNodesGet;
                context.MaxDataPoints = WitsmlSettings.LogMaxDataPointsGet;
                context.TotalMaxDataNodes = Math.Min(context.MaxDataNodes * parser.QueryCount, WitsmlSettings.LogMaxDataNodesGet);

                context.TotalDataNodes = 0;
                context.TotalDataPoints = 0;
                context.DataTruncated = false;
            }

            return context;
        }

        /// <summary>
        /// Updates the growing object totals in the response context.
        /// </summary>
        /// <param name="context">The response context.</param>
        /// <param name="queryNodeCount">The number data nodes returned by the last query.</param>
        /// <param name="channelCount">The channel count returned by the last query.</param>
        public static void UpdateGrowingObjectTotals(this ResponseContext context, int queryNodeCount, int channelCount)
        {
            if (IsGrowingDataObject(context))
            {
                int queryPointCount = queryNodeCount * channelCount;

                // Update response totals
                context.TotalDataNodes += queryNodeCount;
                context.TotalDataPoints += queryPointCount;

                // Update query maximums for the next query
                context.MaxDataNodes = Math.Min(context.MaxDataNodes, context.TotalMaxDataNodes - context.TotalDataNodes);
                context.MaxDataPoints = WitsmlSettings.LogMaxDataPointsGet - context.TotalDataPoints;
            }
        }

        /// <summary>
        /// Determines whether data object for the parser is a growing data object.
        /// </summary>
        /// <param name="context">The response context.</param>
        /// <returns>
        /// True if the data object for the parser is a growing data object, false otherwise.
        /// </returns>
        private static bool IsGrowingDataObject(ResponseContext context)
        {
            return ObjectTypes.IsGrowingDataObject(context.ObjectType);
        }
    }
}
