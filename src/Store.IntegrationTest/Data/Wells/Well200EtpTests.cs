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
using Energistics.DataAccess.WITSML200;
using Energistics.Etp;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Well200EtpTests
    /// </summary>
    public partial class Well200EtpTests
    {
        [TestMethod]
        public async Task Well200_PutObject_Add_Well_Without_Citation_Returns_Protocol_Exception()
        {
            AddParents();
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Well.GetUri();

            // Try adding minimal Well without Citation
            Well = new Well
            {
                Uuid = DevKit.Uid(),
                SchemaVersion = "2.0",
                Block = "Test"
            };

            var dataObject = CreateDataObject(uri, Well);
            dataObject.Resource.Name = DevKit.Name("Well 20");

            // Get Object Expecting it Not to Exist
            await GetAndAssert(handler, uri, EtpErrorCodes.NotFound);

            // Put Object
            await PutAndAssert(handler, dataObject, EtpErrorCodes.InvalidObject);
        }
    }
}
