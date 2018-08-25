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

using log4net;

namespace PDS.WITSMLstudio.Store.Data.MudLogs
{
    /// <summary>
    /// Provides common helper methods for Mudlog data objects.
    /// </summary>
    public static class MudLogExtensions
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MudLogExtensions));

        /// <summary>
        /// Determines whether mudlog geology intervals should be included in the query response.
        /// </summary>
        /// <param name="parser">The query parser.</param>
        /// <returns><c>true</c> if mudlog geology intervals should be included; otherwise, <c>false</c>.</returns>
        public static bool IncludeGeologyIntervals(this WitsmlQueryParser parser)
        {
            var returnElements = parser.ReturnElements();

            _log.DebugFormat("Checking if mudlog geology intervals should be included. Return Elements: {0};", returnElements);

            return OptionsIn.ReturnElements.All.Equals(returnElements) ||
                   OptionsIn.ReturnElements.DataOnly.Equals(returnElements) ||
                   (OptionsIn.ReturnElements.Requested.Equals(returnElements) && parser.Contains("geologyInterval"));
        }
    }
}
