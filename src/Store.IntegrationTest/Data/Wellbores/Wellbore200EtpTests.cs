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

using System.Linq;
using System.Threading.Tasks;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// Wellbore200EtpTests
    /// </summary>
    public partial class Wellbore200EtpTests
    {
        [Ignore]
        [TestMethod]
        public async Task Wellbore200_PutObject_Can_Add_200_Wellbores()
        {
            AddParents();

            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();

            foreach (var i in Enumerable.Range(1, 200))
            {
                Wellbore.Uuid = DevKit.Uid();
                Wellbore.Citation = DevKit.Citation("Wellbore");
                Wellbore.Citation.Title = "Wellbore " + i.ToString().PadLeft(3, '0');

                var dataObject = CreateDataObject(Wellbore.GetUri(), Wellbore);

                // Put Object
                await PutAndAssert(handler, dataObject);
            }
        }
    }
}
