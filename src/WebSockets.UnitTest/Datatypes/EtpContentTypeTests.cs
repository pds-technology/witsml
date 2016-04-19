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

using NUnit.Framework;

namespace Energistics.Datatypes
{
    [TestFixture]
    public class EtpContentTypeTests
    {
        [Test]
        public void EtpContentType_Can_Parse_Base_Content_Type_Without_Trailing_Semicolon()
        {
            var expected = "application/x-witsml+xml;version=2.0";
            var contentType = new EtpContentType(expected);

            Assert.IsTrue(contentType.IsValid);
            Assert.IsTrue(contentType.IsBaseType);
            Assert.AreEqual("witsml", contentType.Family);
            Assert.AreEqual("2.0", contentType.Version);
        }

        [Test]
        public void EtpContentType_Can_Parse_Base_Content_Type_With_Trailing_Semicolon()
        {
            var expected = "application/x-witsml+xml;version=2.0;";
            var contentType = new EtpContentType(expected);

            Assert.IsTrue(contentType.IsValid);
            Assert.IsTrue(contentType.IsBaseType);
            Assert.AreEqual("witsml", contentType.Family);
            Assert.AreEqual("2.0", contentType.Version);
        }

        [Test]
        public void EtpContentType_Rejects_Content_Type_Without_Version()
        {
            var expected = "application/x-witsml+xml;";
            var contentType = new EtpContentType(expected);

            Assert.IsFalse(contentType.IsValid);
        }

        [Test]
        public void EtpContentType_For_Can_Create_1411_Well_Content_Type()
        {
            var expected = "application/x-witsml+xml;version=1.4.1.1";
            var contentType = new EtpContentType(expected).For("well");

            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("well", contentType.ObjectType);
            Assert.AreEqual("1.4.1.1", contentType.Version);
            Assert.AreEqual(expected + ";type=obj_well;", (string)contentType);
        }

        [Test]
        public void EtpContentType_Can_Parse_Witsml_20_Well_Content_Type()
        {
            var expected = "application/x-witsml+xml;version=2.0;type=well;";
            var contentType = new EtpContentType(expected);

            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("well", contentType.ObjectType);
            Assert.AreEqual("2.0", contentType.Version);
        }

        [Test]
        public void EtpContentType_Can_Parse_Witsml_1411_Well_Content_Type()
        {
            var expected = "application/x-witsml+xml;version=1.4.1.1;type=well;";
            var contentType = new EtpContentType(expected);

            Assert.IsTrue(contentType.IsValid);
            Assert.AreEqual("well", contentType.ObjectType);
            Assert.AreEqual("1.4.1.1", contentType.Version);
        }
    }
}
