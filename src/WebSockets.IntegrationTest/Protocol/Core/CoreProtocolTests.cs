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

using System.Net;
using System.Threading.Tasks;
using Energistics.Common;
using Energistics.Datatypes;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Energistics.Protocol.Core
{
    [TestFixture]
    public class CoreProtocolTests : IntegrationTestBase
    {
        private EtpClient _client;

        [SetUp]
        public void TestSetUp()
        {
            _client = CreateClient();
        }

        [TearDown]
        public void TestTearDown()
        {
            _client.Dispose();
        }

        [Test]
        [Description("EtpClient connects to web socket server")]
        public async Task EtpClient_Open_Connects_To_WebSocket_Server()
        {
            var task = new Task<bool>(() => _client.IsOpen);

            _client.SocketOpened += (s, e) =>
            {
                task.Start();
            };

            _client.Open();

            var result = await task.WaitAsync();

            Assert.IsTrue(result, "EtpClient connection not opened");
        }

        [Test]
        [Description("EtpClient sends RequestSession and receives OpenSession with a valid Session ID")]
        public async Task EtpClient_RequestSession_Receive_OpenSession_After_Requesting_No_Protocols()
        {
            var onOpenSession = HandleAsync<OpenSession>(x => _client.Handler<ICoreClient>().OnOpenSession += x);

            var opened = await _client.OpenAsync();
            Assert.IsTrue(opened, "EtpClient connection not opened");

            var args = await onOpenSession.WaitAsync();

            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message);
            Assert.IsNotNull(args.Message.SessionId);
        }

        [Test]
        [Description("EtpClient authenticates using JWT retrieved from supported token provider")]
        public async Task EtpClient_OpenSession_Can_Authenticate_Using_Json_Web_Token()
        {
            var headers = EtpBase.Authorization(Username, Password);
            string token;

            using (var client = new WebClient())
            {
                foreach (var header in headers)
                    client.Headers[header.Key] = header.Value;

                var response = await client.UploadStringTaskAsync(AuthTokenUrl, "grant_type=password");
                var json = JObject.Parse(response);

                token = json["access_token"].Value<string>();
            }

            _client.Dispose();
            _client = new EtpClient(ServerUrl, _client.ApplicationName, _client.ApplicationVersion, EtpBase.Authorization(token));

            var onOpenSession = HandleAsync<OpenSession>(x => _client.Handler<ICoreClient>().OnOpenSession += x);

            var opened = await _client.OpenAsync();
            Assert.IsTrue(opened, "EtpClient connection not opened");

            var args = await onOpenSession.WaitAsync();

            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message);
            Assert.IsNotNull(args.Message.SessionId);
        }

        [Test]
        [Description("EtpClient sends an invalid message and receives ProtocolException with the correct error code")]
        public async Task EtpClient_SendMessage_Receive_Protocol_Exception_After_Sending_Invalid_Message()
        {
            var onProtocolException = HandleAsync<ProtocolException>(x => _client.Handler<ICoreClient>().OnProtocolException += x);

            var opened = await _client.OpenAsync();
            Assert.IsTrue(opened, "EtpClient connection not opened");

            _client.SendMessage(
                new MessageHeader() { Protocol = (int)Protocols.Core, MessageType = -999, MessageId = -999 },
                new Acknowledge());

            var args = await onProtocolException.WaitAsync();

            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message);
            Assert.AreEqual((int)ErrorCodes.EINVALID_MESSAGETYPE, args.Message.ErrorCode);
        }
    }
}
