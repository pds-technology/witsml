using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Energistics.Datatypes
{
    [TestClass]
    public class EtpUriTests
    {
        [TestMethod]
        public void EtpUri_can_detect_root_uri()
        {
            Assert.IsTrue(EtpUri.IsRoot("/"));
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_20_base_uri()
        {
            var uri = new EtpUri("eml://witsml20");

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.IsTrue(uri.IsBaseUri);
            Assert.AreEqual("2.0", uri.Version);
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_20_well_uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml20/well(" + uuid + ")");

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual(uuid, uri.ObjectId);
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual("2.0", uri.Version);
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_20_log_channel_uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml20/log(" + uuid + ")/ROPA");
            var ids = uri.GetObjectIds().FirstOrDefault();

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual("ROPA", uri.ObjectType);
            Assert.AreEqual("2.0", uri.Version);

            Assert.IsNotNull(ids);
            Assert.AreEqual("log", ids.Key);
            Assert.AreEqual(uuid, ids.Value);
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_1411_base_uri()
        {
            var uri = new EtpUri("eml://witsml1411");

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.IsTrue(uri.IsBaseUri);
            Assert.AreEqual("1.4.1.1", uri.Version);
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_1411_well_uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml1411/well(" + uuid + ")");
            var type = "application/x-witsml+xml;version=1.4.1.1;type=well;";

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual(uuid, uri.ObjectId);
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual("1.4.1.1", uri.Version);
            Assert.AreEqual(type, uri.ContentType);
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_1411_wellbore_uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml1411/well(" + Uuid() + ")/wellbore(" + uuid + ")");

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual(uuid, uri.ObjectId);
            Assert.AreEqual("wellbore", uri.ObjectType);
            Assert.AreEqual("1.4.1.1", uri.Version);
        }

        [TestMethod]
        public void EtpUri_can_parse_witsml_1411_log_uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml1411/well(" + Uuid() + ")/wellbore(" + Uuid() + ")/log(" + uuid + ")");

            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual(uuid, uri.ObjectId);
            Assert.AreEqual("log", uri.ObjectType);
            Assert.AreEqual("1.4.1.1", uri.Version);
        }

        private string Uuid()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
