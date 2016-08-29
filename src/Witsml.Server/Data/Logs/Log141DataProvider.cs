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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using PDS.Framework;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="Log"/>.
    /// </summary>
    public partial class Log141DataProvider
    {
        /// <summary>
        /// Sets additional default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        partial void SetAdditionalDefaultValues(Log dataObject)
        {
            // Ensure ObjectGrowing is false during AddToStore
            dataObject.ObjectGrowing = false;

            // Ensure Direction
            if (!dataObject.Direction.HasValue)
                dataObject.Direction = LogIndexDirection.increasing;

            if (dataObject.LogCurveInfo != null)
            {
                // Ensure UID
                dataObject.LogCurveInfo
                    .Where(x => string.IsNullOrWhiteSpace(x.Uid))
                    .ForEach(x => x.Uid = x.Mnemonic?.Value);

                // Ensure index curve is first
                dataObject.LogCurveInfo.MoveToFirst(dataObject.IndexCurve);
            }
        }
    }
}
