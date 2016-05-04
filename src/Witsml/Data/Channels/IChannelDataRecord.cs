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
using System.Collections.Generic;
using System.Data;
using PDS.Framework;

namespace PDS.Witsml.Data.Channels
{
    public interface IChannelDataRecord : IDataRecord
    {
        string Id { get; set; }

        string Uri { get; set; }

        string[] Mnemonics { get; }

        string[] Units { get; }

        string[] NullValues { get; }

        int Depth { get; }

        List<ChannelIndexInfo> Indices { get; }

        ChannelIndexInfo GetIndex(int index = 0);

        double GetIndexValue(int index = 0, int scale = 0);

        Range<double?> GetIndexRange(int index = 0);

        Range<double?> GetChannelIndexRange(int i);

        DateTimeOffset GetDateTimeOffset(int i);

        long GetUnixTimeSeconds(int i);

        bool HasValues();

        string GetJson();

        void SetValue(int i, object value);
    }
}
