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

using System.Threading.Tasks;
using Energistics.DataAccess.WITSML131;
using Energistics.Etp;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Well131EtpTests
    /// </summary>
    public partial class Well131EtpTests
    {

        [TestMethod]
        public async Task Well131_DeleteObject_Cannot_Delete_Well_With_Child_Wellbore()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Well.GetUri();

            var dataObject = CreateDataObject<WellList, Well>(uri, Well);

            // Put Object
            await PutAndAssert(handler, dataObject);

            var wellbore = new Wellbore()
            {
                UidWell = Well.Uid,
                Uid = DevKit.Uid(),
                NameWell = Well.Name,
                Name = DevKit.Name("Wellbore")
            };

            var wellboreObject = CreateDataObject<WellboreList, Wellbore>(wellbore.GetUri(), wellbore);

            // Put Wellbore
            await PutAndAssert(handler, wellboreObject);

            // Delete Well
            await DeleteAndAssert(handler, uri, EtpErrorCodes.NoCascadeDelete);

            // Delete Wellbore
            await DeleteAndAssert(handler, wellbore.GetUri());

            // Delete Well
            await DeleteAndAssert(handler, uri);
        }
    }
}
