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

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Defines static helper methods and fields for the Witsml settings.
    /// </summary>
    public static class WitsmlSettings
    {
        /// <summary>
        /// The truncate XML out debug size
        /// </summary>
        public static int TruncateXmlOutDebugSize = Properties.Settings.Default.TruncateXmlOutDebugSize;

        /// <summary>
        /// The stream index value pairs
        /// </summary>
        public static bool StreamIndexValuePairs = Properties.Settings.Default.StreamIndexValuePairs;

        /// <summary>
        /// The depth range size
        /// </summary>
        public static long DepthRangeSize = Properties.Settings.Default.ChannelDataChunkDepthRangeSize;

        /// <summary>
        /// The time range size
        /// </summary>
        public static long TimeRangeSize = Properties.Settings.Default.ChannelDataChunkTimeRangeSize;

        /// <summary>
        /// The maximum data nodes
        /// </summary>
        public static int MaxDataNodes = Properties.Settings.Default.MaxDataNodes;

        /// <summary>
        /// The maximum data points
        /// </summary>
        public static int MaxDataPoints = Properties.Settings.Default.MaxDataPoints;

        /// <summary>
        /// The default server name
        /// </summary>
        public static string DefaultServerName = Properties.Settings.Default.DefaultServerName;

        /// <summary>
        /// The override server version
        /// </summary>
        public static string OverrideServerVersion = Properties.Settings.Default.OverrideServerVersion;

        /// <summary>
        /// The organization name
        /// </summary>
        public static string DefaultVendorName = Properties.Settings.Default.DefaultVendorName;

        /// <summary>
        /// The default contact name
        /// </summary>
        public static string DefaultContactName = Properties.Settings.Default.DefaultContactName;

        /// <summary>
        /// The default contact email
        /// </summary>
        public static string DefaultContactEmail = Properties.Settings.Default.DefaultContactEmail;

        /// <summary>
        /// The default contact phone
        /// </summary>
        public static string DefaultContactPhone = Properties.Settings.Default.DefaultContactPhone;

        /// <summary>
        /// Gets the size of the range.
        /// </summary>
        /// <param name="isTimeIndex"><c>true</c> if is time index.</param>
        /// <returns></returns>
        public static long GetRangeSize(bool isTimeIndex)
        {
            return isTimeIndex ? TimeRangeSize : DepthRangeSize;
        }
    }
}
