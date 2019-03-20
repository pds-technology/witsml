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
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides common functionality for all <see cref="IDataObjectMessageProducer"/> implementations.
    /// </summary>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectMessageProducer" />
    public abstract class MessageProducerBase : IDataObjectMessageProducer
    {
        /// <summary>
        /// Gets the working directory.
        /// </summary>
        protected string WorkingDirectory => HttpContext.Current?.Server.MapPath("~/bin") ?? Environment.CurrentDirectory;

        /// <summary>
        /// Sends the message asynchronously.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="key">The key.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>An awaitable task.</returns>
        public abstract Task SendMessageAsync(string topic, string key, string payload);

        /// <summary>
        /// Gets the full configuration file path.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The full path to the configuration file.</returns>
        protected virtual string GetConfigFilePath(string fileName)
        {
            return Path.Combine(WorkingDirectory, $@"{ContainerFactory.ConfigurationPath}\{fileName}");
        }

        /// <summary>
        /// Loads the configuration file.
        /// </summary>
        /// <typeparam name="TSettings">The settings type.</typeparam>
        /// <param name="fileName">The file name.</param>
        /// <returns>A new <see cref="TSettings"/> instance.</returns>
        protected virtual TSettings LoadConfigFile<TSettings>(string fileName) where TSettings : new()
        {
            var configFilePath = GetConfigFilePath(fileName);

            if (!File.Exists(configFilePath))
                return new TSettings();

            var json = File.ReadAllText(configFilePath);

            return string.IsNullOrWhiteSpace(json)
                ? new TSettings()
                : JsonConvert.DeserializeObject<TSettings>(json);
        }
    }
}
