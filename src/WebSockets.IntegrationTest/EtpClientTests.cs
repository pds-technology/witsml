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
using Energistics.Protocol.ChannelStreaming;
using Energistics.Protocol.Discovery;
using Energistics.Protocol.Store;
using Energistics.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Energistics
{
    [TestClass]
    public class EtpClientTests
    {
        private const string Uri = "wss://pds-witsml.azurewebsites.net/api/etp";
        private const string AppName = "EtpClientTests";
        private const string AppVersion = "1.0";

        [TestMethod]
        public async Task EtpClient_Opens_WebSocket_Connection()
        {
            // Create a Basic authorization header dictionary
            var auth = Authorization.Basic("witsml.user", "Pd$@meric@$");

            // Initialize an EtpClient with a valid Uri, app name and version, and auth header
            using (var client = new EtpClient(Uri, AppName, AppVersion, auth))
            {
                // Register protocol handlers to be used in later tests
                client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                client.Register<IStoreCustomer, StoreCustomerHandler>();

                // Open the connection (uses an async extension method)
                await client.OpenAsync();

                // Assert the current state of the connection
                Assert.IsTrue(client.IsOpen);

                // Explicit Close not needed as the WebSocket connection will be closed
                // automatically after leaving the scope of the using statement
                //client.Close("reason");
            }
        }
    }
}
