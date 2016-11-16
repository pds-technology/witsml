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
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;

namespace PDS.Witsml.Server.Data.Channels
{
    /// <summary>
    /// Channel200TestBase
    /// </summary>
    public partial class Channel200TestBase
    {
        partial void BeforeEachTest()
        {
            Channel.ChannelClass = DevKit.ToPropertyKind("length");
            Channel.ChannelClass.Citation = DevKit.Citation("ChannelClass");
            Channel.ChannelClass.SchemaVersion = "2.0";
            Channel.ChannelClass.Uuid = DevKit.Uid();
            Channel.DataType = EtpDataType.@double;
            Channel.GrowingStatus = ChannelStatus.inactive;
            Channel.Index = new List<ChannelIndex>();
            Channel.Mnemonic = "CH1";
            Channel.LoggingCompanyName = "PDS";
            Channel.TimeDepth = "Depth";
            Channel.Uom = "m";
        }
    }
}