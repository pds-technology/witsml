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
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Data.FluidsReports
{
    /// <summary>
    /// FluidsReport200DataProvider
    /// </summary>
    public partial class FluidsReport200DataProvider
    {
        /// <summary>
        /// Sets additional default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The URI.</param>
        partial void SetAdditionalDefaultValues(FluidsReport dataObject, EtpUri uri)
        {
            dataObject.DateTime = new Timestamp(DateTimeOffset.UtcNow);
            dataObject.MD = new MeasuredDepthCoord(0, LengthUom.ft) { Datum = "SL" };
        }
    }
}
