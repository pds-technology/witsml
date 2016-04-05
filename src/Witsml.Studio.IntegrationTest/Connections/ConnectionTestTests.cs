//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Studio.Core.Connections
{
    [TestClass]
    public class ConnectionTestTests
    {
        const string _validWitsmlUri = "http://localhost/Witsml.Web/WitsmlStore.svc";
        const string _validEtpUri = "ws://localhost/witsml.web/api/etp";

        [TestMethod]
        public async Task TestValidWitsmlConnectionTestEndpoint()
        {
            var witsmlConnectionTest = new WitsmlConnectionTest();
            var result = await witsmlConnectionTest.CanConnect(new Connection() { Uri = _validWitsmlUri });

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestInvalidWitsmlConnectionTestEndpoint()
        {
            var witsmlConnectionTest = new WitsmlConnectionTest();
            var result = await witsmlConnectionTest.CanConnect(new Connection() { Uri = _validWitsmlUri + "x" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestValidEtpConnectionTestEndpoint()
        {
            var etpConnectionTest = new EtpConnectionTest();
            var result = await etpConnectionTest.CanConnect(new Connection() { Uri = _validEtpUri });

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestInvalidEtpConnectionTestEndpoint()
        {
            var etpConnectionTest = new EtpConnectionTest();
            var result = await etpConnectionTest.CanConnect(new Connection() { Uri = _validEtpUri + "x" });

            Assert.IsFalse(result);
        }


        [TestMethod]
        public async Task TestInvalidEtpConnectionBadFormat()
        {
            var etpConnectionTest = new EtpConnectionTest();
            var result = await etpConnectionTest.CanConnect(new Connection() { Uri = "xxxxxxxx" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestInvalidWitsmlConnectionBadFormat()
        {
            var witsmlConnectionTest = new WitsmlConnectionTest();
            var result = await witsmlConnectionTest.CanConnect(new Connection() { Uri = "xxxxxxxx" });

            Assert.IsFalse(result);
        }
    }
}
