using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Studio.Connections
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
