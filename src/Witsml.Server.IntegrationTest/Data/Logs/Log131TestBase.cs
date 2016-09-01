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

using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log131TestBase
    /// </summary>
    public partial class Log131TestBase
    {
        partial void BeforeEachTest()
        {
            Log.IndexType = LogIndexType.measureddepth;
            Log.IndexCurve = new IndexCurve
            {
                ColumnIndex = 0,
                Value = "MD"
            };
        }

        partial void AfterEachTest()
        {
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.MaxDataPoints = DevKitAspect.DefaultMaxDataPoints;
            WitsmlSettings.MaxDataNodes = DevKitAspect.DefaultMaxDataNodes;
            WitsmlOperationContext.Current = null;
        }
    }
}