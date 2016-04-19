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

using System;
using System.Linq;
using NUnit.Framework;

namespace Energistics.Datatypes
{
    [TestFixture]
    public class EtpUriTests
    {
        [Test]
        public void EtpUri_IsRoot_Can_Detect_Root_Uri()
        {
            Assert.IsTrue(EtpUri.IsRoot("/"));
        }

        [Test]
        public void EtpUri_Can_Detect_Invalid_Uri()
        {
            var expected = "eml://witsml/well";
            var uri = new EtpUri(expected);

            Assert.IsFalse(uri.IsValid);
            Assert.IsNull(uri.Version);
            Assert.AreEqual(uri, uri.Parent);
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_20_Base_Uri()
        {
            var uri = new EtpUri("eml://witsml20");

            Assert.IsTrue(uri.IsValid);
            Assert.IsTrue(uri.IsBaseUri);
            Assert.AreEqual(uri, uri.Parent);
            Assert.AreEqual("2.0", uri.Version);
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_20_Well_Uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml20/well(" + uuid + ")");
            var clone = new EtpUri(uri);

            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual(uuid, uri.ObjectId);
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual("2.0", uri.Version);

            // Assert Equals and GetHashCode
            Assert.IsTrue(uri.Equals(clone));
            Assert.IsTrue(uri.Equals((object)clone));
            Assert.IsFalse(uri.Equals((string)clone));
            Assert.AreEqual(uri.GetHashCode(), clone.GetHashCode());
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_20_Log_Channel_Uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml20/log(" + uuid + ")/channel(ROPA)");
            var ids = uri.GetObjectIds().FirstOrDefault();

            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual("channel", uri.ObjectType);
            Assert.AreEqual("ROPA", uri.ObjectId);
            Assert.AreEqual("2.0", uri.Version);

            Assert.IsNotNull(ids);
            Assert.AreEqual("log", ids.Key);
            Assert.AreEqual(uuid, ids.Value);
        }

        [Test]
        public void EtpUri_IsRelatedTo_Can_Detect_Different_Families()
        {
            var uriResqml = new EtpUri("eml://resqml20");
            var uriWitsml = new EtpUri("eml://witsml20");

            Assert.IsTrue(uriResqml.IsValid);
            Assert.IsTrue(uriWitsml.IsValid);
            Assert.IsFalse(uriResqml.IsRelatedTo(uriWitsml));
        }

        [Test]
        public void EtpUri_IsRelatedTo_Can_Detect_Different_Versions()
        {
            var uri14 = new EtpUri("eml://witsml14/well");
            var uri20 = new EtpUri("eml://witsml20/well");

            Assert.IsTrue(uri14.IsValid);
            Assert.IsTrue(uri20.IsValid);
            Assert.IsFalse(uri14.IsRelatedTo(uri20));
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_1411_Base_Uri()
        {
            var uri = new EtpUri("eml://witsml1411");

            Assert.IsTrue(uri.IsValid);
            Assert.IsTrue(uri.IsBaseUri);
            Assert.AreEqual("1.4.1.1", uri.Version);
        }

        [Test]
        public void EtpUri_Append_Can_Append_Object_Type_To_Base_Uri()
        {
            var uri14 = new EtpUri("eml://witsml1411");
            var uriWell = uri14.Append("well");

            Assert.IsTrue(uriWell.IsValid);
            Assert.IsFalse(uriWell.IsBaseUri);
            Assert.AreEqual("1.4.1.1", uriWell.Version);
            Assert.AreEqual("well", uriWell.ObjectType);

            Assert.AreEqual(uri14, uriWell.Parent);
            Assert.IsTrue(uri14.IsRelatedTo(uriWell));
        }

        [Test]
        public void EtpUri_Append_Can_Append_Object_Type_And_Id_To_Base_Uri()
        {
            var uri = new EtpUri("eml://witsml1411").Append("well", "w-01");

            Assert.IsTrue(uri.IsValid);
            Assert.IsFalse(uri.IsBaseUri);
            Assert.AreEqual("1.4.1.1", uri.Version);
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual("w-01", uri.ObjectId);
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_1411_Well_Uri()
        {
            var uuid = Uuid();
            var expected = "eml://witsml1411/well(" + uuid + ")";
            var type = "application/x-witsml+xml;version=1.4.1.1;type=obj_well;";
            var uri = new EtpUri(expected);

            Assert.IsTrue(uri.IsValid);
            Assert.AreEqual(uuid, uri.ObjectId);
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual("1.4.1.1", uri.Version);
            Assert.AreEqual(expected, uri.ToString());
            Assert.AreEqual(type, uri.ContentType.ToString());
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_1411_Wellbore_Uri()
        {
            var uuid = Uuid();
            var uriWell = new EtpUri("eml://witsml1411/well(" + Uuid() + ")");
            var uriWellbore = uriWell.Append("wellbore", uuid);

            Assert.IsNotNull(uriWellbore);
            Assert.IsTrue(uriWellbore.IsValid);
            Assert.AreEqual(uuid, uriWellbore.ObjectId);
            Assert.AreEqual("wellbore", uriWellbore.ObjectType);
            Assert.AreEqual("1.4.1.1", uriWellbore.Version);

            Assert.IsTrue(uriWellbore.IsRelatedTo(uriWell));
            Assert.AreEqual(uriWell, uriWellbore.Parent);
        }

        [Test]
        public void EtpUri_Can_Parse_Witsml_1411_Log_Uri()
        {
            var uuid = Uuid();
            var uri = new EtpUri("eml://witsml1411/well(" + Uuid() + ")/wellbore(" + Uuid() + ")/log(" + uuid + ")");

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
