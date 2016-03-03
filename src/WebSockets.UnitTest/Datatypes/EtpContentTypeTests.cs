using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Energistics.Datatypes
{
    [TestClass]
    public class EtpContentTypeTests
    {
        [TestMethod]
        public void EtpContentType_can_parse_base_content_type_without_trailing_semicolon()
        {
            var actual = "application/x-witsml+xml;version=2.0";
            var contentType = new EtpContentType(actual);

            Assert.IsNotNull(contentType);
            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("witsml", contentType.Family);
            Assert.AreEqual("2.0", contentType.Version);
        }

        [TestMethod]
        public void EtpContentType_can_parse_base_content_type_with_trailing_semicolon()
        {
            var actual = "application/x-witsml+xml;version=2.0;";
            var contentType = new EtpContentType(actual);

            Assert.IsNotNull(contentType);
            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("witsml", contentType.Family);
            Assert.AreEqual("2.0", contentType.Version);
        }

        [TestMethod]
        public void EtpContentType_can_rejects_content_type_without_version()
        {
            var actual = "application/x-witsml+xml;";
            var contentType = new EtpContentType(actual);

            Assert.IsNotNull(contentType);
            Assert.IsFalse(contentType.IsValid);
        }

        [TestMethod]
        public void EtpContentType_can_parse_witsml_20_well_content_type()
        {
            var actual = "application/x-witsml+xml;version=2.0;type=well;";
            var contentType = new EtpContentType(actual);

            Assert.IsNotNull(contentType);
            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("well", contentType.ObjectType);
            Assert.AreEqual("2.0", contentType.Version);
        }

        [TestMethod]
        public void EtpContentType_can_parse_witsml_1411_well_content_type()
        {
            var actual = "application/x-witsml+xml;version=1.4.1.1;type=well;";
            var contentType = new EtpContentType(actual);

            Assert.IsNotNull(contentType);
            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("well", contentType.ObjectType);
            Assert.AreEqual("1.4.1.1", contentType.Version);
        }
    }
}
