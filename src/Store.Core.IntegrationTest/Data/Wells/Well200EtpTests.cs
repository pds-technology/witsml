//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Energistics;
using Energistics.DataAccess.WITSML200;
using Energistics.Protocol;
using Energistics.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
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
                SchemaVersion = "2.0"
            };

            var dataObject = CreateDataObject(uri, Well);
            dataObject.Resource.Name = DevKit.Name("Well 20");

            // Get Object
            var args = await GetAndAssert(handler, uri);

            // Check for message flag indicating No Data
            Assert.IsNotNull(args?.Header);
            Assert.AreEqual((int)MessageFlags.NoData, args.Header.MessageFlags);

            // Put Object
            await PutAndAssert(handler, dataObject, EtpErrorCodes.InvalidObject);
        }
    }
}
