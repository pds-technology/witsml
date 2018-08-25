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
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="Channel"/>.
    /// </summary>
    public partial class Channel200DataProvider
    {
        /// <summary>
        /// Sets the additional default values.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The URI.</param>
        partial void SetAdditionalDefaultValues(Channel dataObject, EtpUri uri)
        {
            var dataGenerator = new DataGenerator();

            dataObject.ChannelClass = dataObject.ChannelClass ?? dataGenerator.ToPropertyKindReference(QuantityClassKind.unitless.ToString());
            dataObject.DataType = dataObject.DataType ?? EtpDataType.@double;
            dataObject.GrowingStatus = dataObject.GrowingStatus ?? ChannelStatus.inactive;
            dataObject.Index = dataObject.Index ?? new List<ChannelIndex>();
            dataObject.Mnemonic = dataObject.Mnemonic ?? uri.ObjectId;
            dataObject.LoggingCompanyName = dataObject.LoggingCompanyName ?? ObjectTypes.Unknown;
            dataObject.TimeDepth = dataObject.TimeDepth ?? ObjectTypes.Unknown;
            dataObject.Uom = dataObject.Uom ?? UnitOfMeasure.m;
        }
    }
}
