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
        public async Task EtpClient_can_open_connection()
        {
            var task = new Task<bool>(() => true);

            _client.SocketOpened += (s, e) =>
            {
                task.Start();
            };

            _client.Open();

            var result = await task.WaitAsync();

            Assert.IsTrue(result, "EtpClient connection not opened");
        }

        [Test]
        [Description("EtpClient can send RequestSession and receive OpenSession with a valid Session ID")]
        public async Task EtpClient_can_request_session()
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
        [Description("EtpClient can send an invalid message and receive ProtocolException with the correct error code")]
        public async Task EtpClient_can_send_invalid_message_and_receive_protocol_exception()
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
