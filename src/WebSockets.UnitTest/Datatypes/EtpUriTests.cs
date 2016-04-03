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
            var type = "application/x-witsml+xml;version=1.4.1.1;type=obj_well;";

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
