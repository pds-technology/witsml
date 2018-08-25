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

using System;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Data.DataAssuranceRecords
{
    /// <summary>
    /// DataAssuranceRecord200DataProvider
    /// </summary>
    public partial class DataAssuranceRecord200DataProvider
    {
        partial void SetAdditionalDefaultValues(DataAssuranceRecord dataObject, EtpUri uri)
        {
            dataObject.PolicyId = dataObject.PolicyId ?? uri.ObjectId;
            dataObject.Conformance = dataObject.Conformance ?? false;
            dataObject.Date = dataObject.Date ?? DateTimeOffset.UtcNow;
            dataObject.Origin = dataObject.Origin ?? ObjectTypes.Unknown;
            dataObject.ReferencedData = dataObject.ReferencedData ??
                new DataObjectReference
                {
                    ContentType = EtpContentTypes.Witsml200,
                    Uuid = Guid.Empty.ToString(),
                    Title = ObjectTypes.Unknown
                };
        }
    }
}
