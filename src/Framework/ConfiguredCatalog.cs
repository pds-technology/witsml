//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.IO;
using System.Linq;
using log4net;
using Newtonsoft.Json;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// A catalog that is filtered based on a configuration file.
    /// </summary>
    public class ConfiguredCatalog : FilteredCatalog
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ConfiguredCatalog));
        private static ContainerConfig _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredCatalog" /> class.
        /// </summary>
        /// <param name="catalog">The catalog.</param>
        /// <param name="configurationFilePath">The configuration file path.</param>
        public ConfiguredCatalog(ComposablePartCatalog catalog, string configurationFilePath) : base(catalog, GetPartFilter(configurationFilePath))
        {
            if (_log.IsDebugEnabled)
            {
                var assemblies = catalog.Parts.Select(p => ReflectionModelServices.GetPartType(p).Value.Assembly.GetName().Name).Distinct();
                _log.Debug($"Loaded Assemblies:{Environment.NewLine}{string.Join(Environment.NewLine, assemblies)}");
            }
        }

        /// <summary>
        /// Gets the composition container configuration.
        /// </summary>
        public ContainerConfig Configuration => _config;

        private static Func<ComposablePartDefinition, bool> GetPartFilter(string configurationFilePath)
        {
            var config = GetContainerConfig(configurationFilePath);

            if (config.ExcludedAssemblies.Count < 1 && config.ExcludedTypes.Count < 1)
            {
                return part => true;
            }

            return part =>
            {
                var type = ReflectionModelServices.GetPartType(part).Value;
                var assemblyName = type.Assembly.GetName().Name;
                var typeName = type.FullName ?? string.Empty;

                if (typeName.Contains("`"))
                {
                    typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.InvariantCultureIgnoreCase));
                }

                return (!config.ExcludedAssemblies.ContainsIgnoreCase(assemblyName) && !config.ExcludedTypes.ContainsIgnoreCase(typeName))
                    || config.IncludedTypes.ContainsIgnoreCase(typeName);
            };
        }

        private static ContainerConfig GetContainerConfig(string configurationFilePath)
        {
            if (_config != null) return _config;

            try
            {
                _config = File.Exists(configurationFilePath)
                    ? JsonConvert.DeserializeObject<ContainerConfig>(File.ReadAllText(configurationFilePath)).Verify()
                    : new ContainerConfig();
            }
            catch
            {
                _log.Warn($"Error reading container configuration file: {configurationFilePath}");
                _config = new ContainerConfig();
            }

            return _config;
        }
    }
}
