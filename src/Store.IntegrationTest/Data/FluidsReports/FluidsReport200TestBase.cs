//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;

namespace PDS.WITSMLstudio.Store.Data.FluidsReports
{
    /// <summary>
    /// FluidsReport200TestBase
    /// </summary>
    public partial class FluidsReport200TestBase
    {
        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            FluidsReport.DateTime = new Timestamp(DateTimeOffset.UtcNow);
            FluidsReport.MD = new MeasuredDepthCoord(0, LengthUom.ft) { Datum = "SL" };
            FluidsReport.Wellbore = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = "Wellbore",
                Uuid = DevKit.Uid()
            };
        }
    }
}
