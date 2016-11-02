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
using Energistics.DataAccess.WITSML200;
using PDS.Witsml.Data.Logs;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log200TestBase
    /// </summary>
    public partial class Log200TestBase
    {
        public Log200Generator LogGenerator { get; set; }

        partial void BeforeEachTest()
        {
            LogGenerator = new Log200Generator();
            Log.ChannelSet = new List<ChannelSet>();
        }
    }
}