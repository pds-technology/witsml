using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Energistics.Protocol.ChannelStreaming
{
    [TestClass]
    public class ChannelStreamingProtocolTests : IntegrationTestBase
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
        public async Task EtpClient_RequestSession_Connects_To_Simple_Producer()
        {
            // Register protocol handlers
            _client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
            var handler = _client.Handler<IChannelStreamingConsumer>();

            // Register event handlers
            var onChannelMetadata = HandleAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleAsync<ChannelData>(x => handler.OnChannelData += x);

            // Wait for Open connection
            await _client.OpenAsync();

            // Wait for ChannelMetadata message from Simple Producer
            await onChannelMetadata.WaitAsync();
        }
    }
}
