//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

namespace PDS.Witsml.Data.Channels
{
    /// <summary>
    /// Encapsulates the index information for unified log data chunk
    /// </summary>
    [Serializable]
    public class ChannelIndexInfo
    {
        public string Mnemonic { get; set; }

        public string Unit { get; set; }

        public bool Increasing { get; set; }

        public bool IsTimeIndex { get; set; }

        public double Start { get; set; }

        public double End { get; set; }

        public ChannelIndexInfo Clone()
        {
            return (ChannelIndexInfo)MemberwiseClone();
        }
    }
}
