//----------------------------------------------------------------------- 
// ETP DevKit, 1.0
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
using Energistics.Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Energistics.Protocol.Discovery
{
    [TestClass]
    public class DiscoveryProtocolTests : IntegrationTestBase
    {
        private EtpClient _client;

        [TestInitialize]
        public void TestSetUp()
        {
            _client = CreateClient();
        }

        [TestCleanup]
        public void TestTearDown()
        {
            _client.Dispose();
        }

        [TestMethod]
        public async Task IDiscoveryCustomer_GetResource_Request_Default_Uri()
        {
            // Register protocol handler
            _client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
            var handler = _client.Handler<IDiscoveryCustomer>();

            // Register event handlers
            var onGetResourcesResponse = HandleAsync<GetResourcesResponse, string>(x => handler.OnGetResourcesResponse += x);

            // Wait for Open connection
            var isOpen = await _client.OpenAsync();
            Assert.IsTrue(isOpen);

            // Send GetResources message for root URI
            handler.GetResources(EtpUri.RootUri);

            // Wait for GetResourcesResponse for top level resources
            var args = await onGetResourcesResponse.WaitAsync();

            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message.Resource);
            Assert.IsNotNull(args.Message.Resource.Uri);

            // Send GetResources message for child resources
            var resource = args.Message.Resource;
            handler.GetResources(resource.Uri);

            // Wait for GetResourcesResponse for child resources
            args = await onGetResourcesResponse.WaitAsync();

            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message.Resource);
            Assert.AreNotEqual(resource.Uri, args.Message.Resource.Uri);
        }
    }
}
