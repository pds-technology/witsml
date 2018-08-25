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

using System.Collections;
using PDS.WITSMLstudio.Store.IntegrationTest;

namespace PDS.WITSMLstudio.Store
{
    /// <summary>
    /// Defines static fields for the ETP test settings.
    /// </summary>
    public static class TestSettings
    {
        private static IDictionary _settings;

        /// <summary>
        /// The default ETP server URL
        /// </summary>
        public static string FallbackServerUrl = Settings.Default.FallbackServerUrl;

        /// <summary>
        /// The default ETP server URL
        /// </summary>
        public static string EtpServerUrl = Settings.Default.EtpServerUrl;

        /// <summary>
        /// The default username
        /// </summary>
        public static string Username = Settings.Default.Username;

        /// <summary>
        /// The default password
        /// </summary>
        public static string Password = Settings.Default.Password;

        /// <summary>
        /// The default ETP version
        /// </summary>
        public static string EtpVersion = Settings.Default.EtpVersion;

        /// <summary>
        /// The default timeout in milliseconds
        /// </summary>
        public static int DefaultTimeoutInMilliseconds = Settings.Default.DefaultTimeoutInMilliseconds;

        /// <summary>
        /// Initializes the specified settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public static void Init(IDictionary settings)
        {
            // Ignore settings dictionary if EtpServerUrl parameter not specified
            if (!settings.Contains("EtpServerUrl")) return;
            _settings = settings;

            //ServerCapabilitiesUrl = $"{settings["ServerCapabilitiesUrl"]}";
            //AuthTokenUrl = $"{settings["AuthTokenUrl"]}";
            FallbackServerUrl = $"{settings["FallbackServerUrl"]}";
            EtpServerUrl = $"{settings["EtpServerUrl"]}";
            //SoapServerUrl = $"{settings["SoapServerUrl"]}";
            Username = $"{settings["Username"]}";
            Password = $"{settings["Password"]}";
            EtpVersion = $"{settings["EtpVersion"]}";

            //bool loadTestData;
            //if (!bool.TryParse($"{settings["LoadTestData"]}", out loadTestData))
            //    loadTestData = Settings.Default.LoadTestData;
            //LoadTestData = loadTestData;

            int timeout;
            if (!int.TryParse($"{settings["DefaultTimeoutInMilliseconds"]}", out timeout))
                timeout = Settings.Default.DefaultTimeoutInMilliseconds;
            DefaultTimeoutInMilliseconds = timeout;
        }

        /// <summary>
        /// Resets any modified test settings.
        /// </summary>
        public static void Reset()
        {
            // Revert original test settings, if specified
            if (_settings != null)
            {
                Init(_settings);
                return;
            }

            //ServerCapabilitiesUrl = Settings.Default.ServerCapabilitiesUrl;
            //AuthTokenUrl = Settings.Default.AuthTokenUrl;
            FallbackServerUrl = Settings.Default.FallbackServerUrl;
            EtpServerUrl = Settings.Default.EtpServerUrl;
            //SoapServerUrl = Settings.Default.SoapServerUrl;
            Username = Settings.Default.Username;
            Password = Settings.Default.Password;
            EtpVersion = Settings.Default.EtpVersion;
            //LoadTestData = Settings.Default.LoadTestData;
            DefaultTimeoutInMilliseconds = Settings.Default.DefaultTimeoutInMilliseconds;
        }
    }
}
