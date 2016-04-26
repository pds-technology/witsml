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

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PDS.Witsml.Server.Models
{
    [DataContract]
    public class SchemaMapping
    {
        public SchemaMapping()
        {
            Mappings = new Dictionary<string, ObjectMapping>();
        }

        [DataMember]
        public DatabaseMapping Database { get; set; }

        [DataMember]
        public Dictionary<string, ObjectMapping> Mappings { get; set; }

        [DataMember]
        public string Version { get; set; }
    }
}
