//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using Energistics.Datatypes;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Data provider that implements support for ETP API functions for <see cref="Log"/>.
    /// </summary>
    public partial class Log200DataProvider
    {
        /// <summary>
        /// Sets additional default values for the specified data object and URI.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The data object URI.</param>
        partial void SetAdditionalDefaultValues(Log dataObject, EtpUri uri)
        {
            if (dataObject.ChannelSet == null)
                dataObject.ChannelSet = new List<ChannelSet>();
        }
    }
}
