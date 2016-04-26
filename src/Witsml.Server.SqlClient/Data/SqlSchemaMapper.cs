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

using System.ComponentModel.Composition;
using System.IO;
using Newtonsoft.Json;
using PDS.Framework.Web;
using PDS.Witsml.Server.Models;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Manages SQL database schema mapping configuration settings.
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SqlSchemaMapper
    {
        /// <summary>
        /// Gets the schema mapping.
        /// </summary>
        /// <value>The schema mapping.</value>
        public SchemaMapping Schema { get; private set; }

        /// <summary>
        /// Determines whether an object mapping is available for the specified <see cref="ObjectName"/>.
        /// </summary>
        /// <param name="objectName">The name and version of the data object.</param>
        /// <returns><c>true</c> if an object mapping is available; otherwise, <c>false</c>.</returns>
        public bool IsAvailable(ObjectName objectName)
        {
            if (Schema == null) return false;

            return Schema.Version == objectName.Version &&
                   Schema.Mappings.ContainsKey(objectName.Name);
        }

        /// <summary>
        /// Configures the current instance.
        /// </summary>
        public void Configure()
        {
            var path = ContainerConfiguration.MapWorkingDirectory("config.json");
            var json = File.ReadAllText(path);

            Configure(json);
        }

        /// <summary>
        /// Configures a <see cref="SchemaMapping"/> using the specified json.
        /// </summary>
        /// <param name="json">The json configuration.</param>
        /// <returns>A <see cref="SchemaMapping"/> instance.</returns>
        internal SchemaMapping Configure(string json)
        {
            Schema = !string.IsNullOrWhiteSpace(json)
                ? JsonConvert.DeserializeObject<SchemaMapping>(json)
                : new SchemaMapping();

            if (Schema.Database == null)
                Schema.Database = new DatabaseMapping();

            return Schema;
        }
    }
}
