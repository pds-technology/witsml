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

using System;
using System.Collections.Generic;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Models
{
    /// <summary>
    /// Model class for unified log data
    /// </summary>
    [Serializable]
    public class ChannelDataValues
    {
        public string Id { get; set; }

        public string Uid { get; set; }

        public string UidLog { get; set; }

        public string UidChannelSet { get; set; }
        
        /// <summary>
        /// Gets or sets the indices.
        /// </summary>
        /// <value>
        /// The indices to handle multiple indices in a channel set for 2.0 log.
        /// </value>
        public List<ChannelIndexInfo> Indices { get; set; }

        public string Data { get; set; }

        public string MnemonicList { get; set; }

        public string UnitList { get; set; }
    }
}
