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
        public async Task EtpClient_Open_Sends_RequestSession_And_Receives_OpenSession_Successfully()
        {
            var auth = Authorization.Basic("witsml.user", "P@$$^0rd!");

            using (var client = new EtpClient(Uri, AppName, AppVersion, auth))
            {
                client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                client.Register<IStoreCustomer, StoreCustomerHandler>();

                await client.OpenAsync();
            }
        }
    }
}
