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
using Energistics.Etp.Common.Datatypes;

namespace PDS.WITSMLstudio.Store.Data.ChannelSets
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions for <see cref="ChannelSet"/>.
    /// </summary>
    public partial class ChannelSet200DataProvider
    {
        /// <summary>
        /// Sets additional default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        partial void SetAdditionalDefaultValues(ChannelSet dataObject)
        {
            dataObject.Channel.ForEach(c => c.Uuid = c.NewUuid());
        }

        /// <summary>
        /// Sets additional default values for the specified data object and URI.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The data object URI.</param>
        partial void SetAdditionalDefaultValues(ChannelSet dataObject, EtpUri uri)
        {
            if (dataObject.Index == null)
                dataObject.Index = new List<ChannelIndex>();

            if (dataObject.Channel == null)
                dataObject.Channel = new List<Channel>();
        }
    }
}
