//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Encapsulates log data from either a WITSML 1.4.1.1 or WITSML 1.3.1.1 log
    /// </summary>
    [Serializable]
    public sealed class LogData
    {
        /// <summary>
        /// Gets the mnemonics.
        /// </summary>
        public string[] MnemonicList { get; }

        /// <summary>
        /// Gets the units of measure.
        /// </summary>
        public string[] UnitList { get; }

        /// <summary>
        /// Gets the data rows.
        /// </summary>
        public IReadOnlyList<string> Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogData"/> class.
        /// </summary>
        /// <param name="mnemonicList">The mnemonics list.</param>
        /// <param name="unitList">The units of measure list.</param>
        /// <param name="data">The data.</param>
        public LogData(string[] mnemonicList, string[] unitList, IReadOnlyList<string> data)
        {
            mnemonicList.NotNull(nameof(mnemonicList));
            unitList.NotNull(nameof(unitList));
            data.NotNull(nameof(data));

            MnemonicList = mnemonicList;
            UnitList = unitList;
            Data = data;
        }
    }
}
