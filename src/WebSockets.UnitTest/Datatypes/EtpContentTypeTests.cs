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
        public void EtpContentType_rejects_content_type_without_version()
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
