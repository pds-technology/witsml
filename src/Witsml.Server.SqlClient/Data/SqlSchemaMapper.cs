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
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SqlSchemaMapper
    {
        public SchemaMapping Schema { get; private set; }

        public void Configure()
        {
            var path = ContainerConfiguration.MapWorkingDirectory("config.json");
            var json = File.ReadAllText(path);

            Schema = Configure(json);
        }

        internal SchemaMapping Configure(string json)
        {
            var schema = !string.IsNullOrWhiteSpace(json)
                ? JsonConvert.DeserializeObject<SchemaMapping>(json)
                : new SchemaMapping();

            if (schema.Database == null)
                schema.Database = new DatabaseMapping();

            return schema;
        }
    }
}
