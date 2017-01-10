//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
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
using Energistics.DataAccess.WITSML141;
using Energistics.Datatypes;
using Energistics.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Well141EtpTests
    /// </summary>
    public partial class Well141EtpTests
    {
        [TestMethod]
        public async Task Well141_GetResources_Can_Get_Root_Level_Resources()
        {
            AddParents();
            DevKit.AddAndAssert<WellList, Well>(Well);

            await RequestSessionAndAssert();
            await GetResourcesAndAssert(new EtpUri(EtpUri.RootUri));
        }

        [TestMethod]
        public async Task Well141_GetResources_Can_Detect_Invalid_Uris()
        {
            await RequestSessionAndAssert();

            await GetResourcesAndAssert(new EtpUri("eml://unknown141"), EtpErrorCodes.InvalidUri);
            await GetResourcesAndAssert(new EtpUri("eml://witsml141"));
            await GetResourcesAndAssert(new EtpUri("eml://witsml141/ChannelSet"));
        }

        [TestMethod]
        public async Task Well141_GetObject_Can_Detect_Invalid_Uris()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
        
            // Get Invalid Object
            await GetAndAssert(handler, new EtpUri("eml://unknown141/wellz(123)"), EtpErrorCodes.InvalidUri);
            await GetAndAssert(handler, new EtpUri("eml://witsml141"), EtpErrorCodes.UnsupportedObject);
        }

        [TestMethod]
        public async Task Well141_DeleteObject_Can_Detect_Invalid_Uris()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();

            // Delete Invalid Object
            await DeleteAndAssert(handler, new EtpUri("eml://unknown141/wellz(123)"), EtpErrorCodes.InvalidUri);
            await DeleteAndAssert(handler, new EtpUri("eml://witsml141"), EtpErrorCodes.UnsupportedObject);
        }
    }
}