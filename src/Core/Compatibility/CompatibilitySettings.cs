//----------------------------------------------------------------------- 
// PDS.Witsml, 2017.1
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
using PDS.Witsml.Properties;

namespace PDS.Witsml.Compatibility
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
            Enum.TryParse(Settings.Default.InvalidDataRowSetting, out InvalidDataRowSetting);
            Enum.TryParse(Settings.Default.UnknownElementSetting, out UnknownElementSetting);
        }
    }
}
