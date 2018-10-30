//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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

using System.Collections.Generic;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    /// <summary>
    /// Channel200TestBase
    /// </summary>
    public partial class Channel200TestBase
    {
        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            Channel.ChannelClass = DevKit.ToPropertyKindReference("length");
            Channel.DataType = EtpDataType.@double;
            Channel.GrowingStatus = ChannelStatus.inactive;
            Channel.Index = new List<ChannelIndex>();
            Channel.Mnemonic = "CH1";
            Channel.LoggingCompanyName = "PDS";
            Channel.TimeDepth = "Depth";
            Channel.Uom = UnitOfMeasure.m;
        }
    }
}
