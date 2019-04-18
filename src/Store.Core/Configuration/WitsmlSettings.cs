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

namespace PDS.WITSMLstudio.Store.Configuration
{
    /// <summary>
    /// Defines static helper methods and fields for the Witsml settings.
    /// </summary>
    public static class WitsmlSettings
    {
        /// <summary>
        /// The IsUserAuthorizationEnabled setting.
        /// </summary>
        public static bool IsUserAuthorizationEnabled = Properties.Settings.Default.IsUserAuthorizationEnabled;

        /// <summary>
        /// The IsCascadeDeleteEnabled setting.
        /// </summary>
        public static bool IsCascadeDeleteEnabled = Properties.Settings.Default.IsCascadeDeleteEnabled;

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
        /// The depth range step size
        /// </summary>
        public static long DepthRangeStepSize = Properties.Settings.Default.ChannelDataDepthRangeStepSize;

        /// <summary>
        /// The time range step size
        /// </summary>
        public static long TimeRangeStepSize = Properties.Settings.Default.ChannelDataTimeRangeStepSize;

        /// <summary>
        /// The log GetFromStore maximum data nodes
        /// </summary>
        public static int MaxRequestLatestValues = Properties.Settings.Default.MaxRequestLatestValues;

        /// <summary>
        /// The log GetFromStore maximum data nodes
        /// </summary>
        public static int LogMaxDataNodesGet = Properties.Settings.Default.LogMaxDataNodesGet;

        /// <summary>
        /// The log AddToStore maximum data nodes
        /// </summary>
        public static int LogMaxDataNodesAdd = Properties.Settings.Default.LogMaxDataNodesAdd;

        /// <summary>
        /// The log UpdateInStore maximum data nodes
        /// </summary>
        public static int LogMaxDataNodesUpdate = Properties.Settings.Default.LogMaxDataNodesUpdate;

        /// <summary>
        /// The log DeleteFromStore maximum data nodes
        /// </summary>
        public static int LogMaxDataNodesDelete = Properties.Settings.Default.LogMaxDataNodesDelete;

        /// <summary>
        /// The log GetFromStore maximum data points
        /// </summary>
        public static int LogMaxDataPointsGet = Properties.Settings.Default.LogMaxDataPointsGet;

        /// <summary>
        /// The log AddToStore maximum data points
        /// </summary>
        public static int LogMaxDataPointsAdd = Properties.Settings.Default.LogMaxDataPointsAdd;

        /// <summary>
        /// The log UpdateInStore maximum data points
        /// </summary>
        public static int LogMaxDataPointsUpdate = Properties.Settings.Default.LogMaxDataPointsUpdate;

        /// <summary>
        /// The log DeleteFromStore maximum data points
        /// </summary>
        public static int LogMaxDataPointsDelete = Properties.Settings.Default.LogMaxDataPointsDelete;

        /// <summary>
        /// The Trajectory GetFromStore maximum data nodes
        /// </summary>
        public static int TrajectoryMaxDataNodesGet = Properties.Settings.Default.TrajectoryMaxDataNodesGet;

        /// <summary>
        /// The Trajectory AddToStore maximum data nodes
        /// </summary>
        public static int TrajectoryMaxDataNodesAdd = Properties.Settings.Default.TrajectoryMaxDataNodesAdd;

        /// <summary>
        /// The Trajectory UpdateInStore maximum data nodes
        /// </summary>
        public static int TrajectoryMaxDataNodesUpdate = Properties.Settings.Default.TrajectoryMaxDataNodesUpdate;

        /// <summary>
        /// The Trajectory DeleteFromStore maximum data nodes
        /// </summary>
        public static int TrajectoryMaxDataNodesDelete = Properties.Settings.Default.TrajectoryMaxDataNodesDelete;

        /// <summary>
        /// The MudLog GetFromStore maximum data nodes
        /// </summary>
        public static int MudLogMaxDataNodesGet = Properties.Settings.Default.MudlogMaxDataNodesGet;

        /// <summary>
        /// The MudLog AddToStore maximum data nodes
        /// </summary>
        public static int MudLogMaxDataNodesAdd = Properties.Settings.Default.MudlogMaxDataNodesAdd;

        /// <summary>
        /// The MudLog UpdateInStore maximum data nodes
        /// </summary>
        public static int MudLogMaxDataNodesUpdate = Properties.Settings.Default.MudlogMaxDataNodesUpdate;

        /// <summary>
        /// The MudLog DeleteFromStore maximum data nodes
        /// </summary>
        public static int MudLogMaxDataNodesDelete = Properties.Settings.Default.MudlogMaxDataNodesDelete;

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
        /// The maximum data length
        /// </summary>
        public static int MaxDataLength = Properties.Settings.Default.MaxDataLength;

        /// <summary>
        /// The chunk size bytes
        /// </summary>
        public static int ChunkSizeBytes = Properties.Settings.Default.ChunkSizeBytes;

        /// <summary>
        /// The data delimiter error message
        /// </summary>
        public static string DataDelimiterErrorMessage = Properties.Settings.Default.DataDelimiterErrorMessage;

        /// <summary>
        /// The maximum station count
        /// </summary>
        public static int MaxStationCount = Properties.Settings.Default.MaxStationCount;

        /// <summary>
        /// The maximum geology interval count
        /// </summary>
        public static int MaxGeologyIntervalCount = Properties.Settings.Default.MaxGeologyIntervalCount;

        /// <summary>
        /// The time in milliseconds to wait during the StreamChannelData loop
        /// </summary>
        public static int StreamChannelDataDelayMilliseconds = Properties.Settings.Default.StreamChannelDataDelayMilliseconds;

        /// <summary>
        /// The change detection period in seconds
        /// </summary>
        public static int ChangeDetectionPeriod = Properties.Settings.Default.ChangeDetectionPeriod;

        /// <summary>
        /// The Log growing timeout period in seconds
        /// </summary>
        public static int LogGrowingTimeoutPeriod = Properties.Settings.Default.LogGrowingTimeoutPeriod;

        /// <summary>
        /// The MudLog growing timeout period in seconds
        /// </summary>
        public static int MudLogGrowingTimeoutPeriod = Properties.Settings.Default.MudLogGrowingTimeoutPeriod;

        /// <summary>
        /// The Trajectory growing timeout period in seconds
        /// </summary>
        public static int TrajectoryGrowingTimeoutPeriod = Properties.Settings.Default.TrajectoryGrowingTimeoutPeriod;

        /// <summary>
        /// The default time zone.
        /// </summary>
        public static string DefaultTimeZone = Properties.Settings.Default.DefaultTimeZone;

        /// <summary>
        /// The maximum number of GetResourcesResponse messages to return.
        /// </summary>
        public static int MaxGetResourcesResponse = Properties.Settings.Default.MaxGetResourcesResponse;

        /// <summary>
        /// The channel data adapter enabled setting.
        /// </summary>
        public static bool IsChannelDataAdapterEnabled = Properties.Settings.Default.IsChannelDataAdapterEnabled;

        /// <summary>
        /// The default describe URI.
        /// </summary>
        public static string DefaultDescribeUri = Properties.Settings.Default.DefaultDescribeUri;

        /// <summary>
        /// Whether or not to request compression is supported.
        /// </summary>
        public static bool IsRequestCompressionEnabled = Properties.Settings.Default.IsRequestCompressionEnabled;

        /// <summary>
        /// The global insert topic name.
        /// </summary>
        public static string GlobalInsertTopicName = Properties.Settings.Default.GlobalInsertTopicName;

        /// <summary>
        /// The global update topic name.
        /// </summary>
        public static string GlobalUpdateTopicName = Properties.Settings.Default.GlobalUpdateTopicName;
        
        /// <summary>
        /// The global replace topic name.
        /// </summary>
        public static string GlobalReplaceTopicName = Properties.Settings.Default.GlobalReplaceTopicName;
        
        /// <summary>
        /// The global delete topic name.
        /// </summary>
        public static string GlobalDeleteTopicName = Properties.Settings.Default.GlobalDeleteTopicName;

        /// <summary>
        /// The ThrowForUnsupportedOptionsIn setting. 
        /// </summary>
        public static bool ThrowForUnsupportedOptionsIn = Properties.Settings.Default.ThrowForUnsupportedOptionsIn;

        /// <summary>
        /// Gets the size of the range.
        /// </summary>
        /// <param name="isTimeIndex"><c>true</c> if is time index.</param>
        /// <returns></returns>
        public static long GetRangeSize(bool isTimeIndex)
        {
            return isTimeIndex ? TimeRangeSize : DepthRangeSize;
        }

        /// <summary>
        /// Gets the step size of the range.
        /// </summary>
        /// <param name="isTimeIndex"><c>true</c> if is time index.</param>
        /// <returns></returns>
        public static long GetRangeStepSize(bool isTimeIndex)
        {
            return isTimeIndex ? TimeRangeStepSize : DepthRangeStepSize;
        }
    }
}
