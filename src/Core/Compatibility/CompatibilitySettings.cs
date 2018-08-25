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
using PDS.WITSMLstudio.Properties;

namespace PDS.WITSMLstudio.Compatibility
{
    /// <summary>
    /// Provides central access to all compatibility settings.
    /// </summary>
    public static class CompatibilitySettings
    {
        /// <summary>
        /// The allow duplicate non recurring elements setting.
        /// </summary>
        public static bool AllowDuplicateNonRecurringElements;

        /// <summary>
        /// Allow Log data to be saved during the put of a data object.
        /// </summary>
        public static bool LogAllowPutObjectWithData;

        /// <summary>
        /// Allow Trajectory stations to be saved during the put of a data object.
        /// </summary>
        public static bool TrajectoryAllowPutObjectWithData;

        /// <summary>
        /// Allow Mud Log data to be saved during the put of a data object.
        /// </summary>
        public static bool MudLogAllowPutObjectWithData;

        /// <summary>
        /// The invalid data row setting.
        /// </summary>
        public static InvalidDataRowSetting InvalidDataRowSetting;

        /// <summary>
        /// The unknown element setting.
        /// </summary>
        public static UnknownElementSetting UnknownElementSetting;

        /// <summary>
        /// Initializes the <see cref="CompatibilitySettings"/> class.
        /// </summary>
        static CompatibilitySettings()
        {
            AllowDuplicateNonRecurringElements = Settings.Default.AllowDuplicateNonRecurringElements;
            LogAllowPutObjectWithData = Settings.Default.LogAllowPutObjectWithData;
            TrajectoryAllowPutObjectWithData = Settings.Default.TrajectoryAllowPutObjectWithData;
            MudLogAllowPutObjectWithData = Settings.Default.MudLogAllowPutObjectWithData;

            // TODO: Use hard coded settings until we can find out why we're getting an error accessing 
            // TODO: ...InvalidDataRowSetting and UnknownElementSetting.
            InvalidDataRowSetting = InvalidDataRowSetting.Ignore;
            UnknownElementSetting = UnknownElementSetting.Ignore;
            //Enum.TryParse(Settings.Default.InvalidDataRowSetting, out InvalidDataRowSetting);
            //Enum.TryParse(Settings.Default.UnknownElementSetting, out UnknownElementSetting);
        }
    }
}
