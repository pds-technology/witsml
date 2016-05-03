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
using System.Linq;
using PDS.Witsml.Server.Data;
using PDS.Witsml.Server.Properties;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Encapsulates data for responses to WITSML Store API methods.
    /// </summary>
    public class ResponseContext
    {
        private readonly int _maxDataNodes = Settings.Default.MaxDataNodes;
        private readonly int _maxDataPoints = Settings.Default.MaxDataPoints;
        private readonly WitsmlQueryParser _parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseContext"/> class.
        /// </summary>
        /// <param name="parser">The Witsml query parser.</param>
        public ResponseContext(WitsmlQueryParser parser)
        {
            _parser = parser;

            var queryCount = _parser.Elements().ToArray().Length;

            if (IsGrowingDataObject())
            {
                int? maxReturnNodes = _parser.MaxReturnNodes();
                MaxReturnNodes = maxReturnNodes;

                QueryMaxDataNodes = MaxReturnNodes.HasValue
                    ? Math.Min(MaxReturnNodes.Value, _maxDataNodes)
                    : _maxDataNodes;

                QueryMaxDataPoints = _maxDataPoints;

                ResponseMaxDataNodes = Math.Min(QueryMaxDataNodes * queryCount, _maxDataNodes);
                ResponseMaxDataPoints = _maxDataPoints;

                ResponseDataNodeTotal = 0;
                ResponseDataPointTotal = 0;
            }
        }

        /// <summary>
        /// Gets the maximum return nodes OptionsIn from the parser.
        /// </summary>
        /// <value>
        /// The maximum return nodes.
        /// </value>
        public int? MaxReturnNodes { get; private set; }

        /// <summary>
        /// Gets the current query maximum data nodes and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The query maximum data nodes.
        /// </value>
        public int QueryMaxDataNodes { get; private set; }

        /// <summary>
        /// Gets the current query maximum data points and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The query maximum data points.
        /// </value>
        public int QueryMaxDataPoints { get; private set; }

        /// <summary>
        /// Gets the response maximum data nodes.
        /// </summary>
        /// <value>
        /// The response maximum data nodes.
        /// </value>
        public int ResponseMaxDataNodes { get; private set; }

        /// <summary>
        /// Gets the response maximum data points.
        /// </summary>
        /// <value>
        /// The response maximum data points.
        /// </value>
        public int ResponseMaxDataPoints { get; private set; }

        /// <summary>
        /// Gets the current response data node total and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The response data node total.
        /// </value>
        public int ResponseDataNodeTotal { get; private set; }

        /// <summary>
        /// Gets the current response data point total and is 
        /// updated by the UpdateGrowingObjectTotals method.
        /// </summary>
        /// <value>
        /// The response data point total.
        /// </value>
        public int ResponseDataPointTotal { get; private set; }

        /// <summary>
        /// Updates the growing object totals.
        /// </summary>
        /// <param name="queryNodeCount">The number data nodes returned by the last query.</param>
        /// <param name="channelCount">The channel count returned by the last query.</param>
        public void UpdateGrowingObjectTotals(int queryNodeCount, int channelCount)
        {
            if (IsGrowingDataObject())
            {
                int queryPointCount = queryNodeCount * channelCount;

                // Update response totals
                ResponseDataNodeTotal += queryNodeCount;
                ResponseDataPointTotal += queryPointCount;

                // Update query maximums for the next query
                QueryMaxDataNodes = Math.Min(QueryMaxDataNodes, ResponseMaxDataNodes - ResponseDataNodeTotal);
                QueryMaxDataPoints = _maxDataPoints - ResponseDataPointTotal;
            }
        }

        /// <summary>
        /// Determines whether data object for the parser is a growing data object.
        /// </summary>
        /// <returns>True if the data object for the parser is a growing data object, false otherwise.</returns>
        private bool IsGrowingDataObject()
        {
            return ObjectTypes.IsGrowingDataObject(_parser.Context.ObjectType);
        }
    }
}
