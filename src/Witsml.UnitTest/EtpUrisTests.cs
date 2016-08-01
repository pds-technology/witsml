//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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

using Energistics.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml
{
    /// <summary>
    /// EtpUris tests.
    /// </summary>
    [TestClass]
    public class EtpUrisTests
    {
        private DataGenerator _data;

        [TestInitialize]
        public void TestSetUp()
        {
            _data = new DataGenerator();
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Well_141_Uri()
        {
            var well = new Witsml141.Well { Uid = _data.Uid() };
            var uri = well.GetUri();

            Assert.IsTrue($"eml://witsml14/well({ well.Uid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual(well.Uid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Well_200_Uri()
        {
            var well = new Witsml200.Well { Uuid = _data.Uid() };
            var uri = well.GetUri();

            Assert.IsTrue($"eml://witsml20/Well({ well.Uuid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual(well.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Wellbore_141_Uri()
        {
            var wellbore = new Witsml141.Wellbore { Uid = _data.Uid(), UidWell = _data.Uid() };
            var uri = wellbore.GetUri();

            Assert.IsTrue($"eml://witsml14/well({ wellbore.UidWell })/wellbore({ wellbore.Uid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("wellbore", uri.ObjectType);
            Assert.AreEqual(wellbore.Uid, uri.ObjectId);
            Assert.AreEqual(uri, ((IDataObject)wellbore).GetUri());
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Wellbore_200_Uri()
        {
            var wellbore = new Witsml200.Wellbore { Uuid = _data.Uid() };
            var uri = wellbore.GetUri();

            Assert.IsTrue($"eml://witsml20/Wellbore({ wellbore.Uuid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("wellbore", uri.ObjectType);
            Assert.AreEqual(wellbore.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_141_Uri()
        {
            var log = new Witsml141.Log { Uid = _data.Uid(), UidWell = _data.Uid(), UidWellbore = _data.Uid() };
            var uri = log.GetUri();

            Assert.IsTrue($"eml://witsml14/well({ log.UidWell })/wellbore({ log.UidWellbore })/log({ log.Uid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("log", uri.ObjectType);
            Assert.AreEqual(log.Uid, uri.ObjectId);
            Assert.AreEqual(uri, ((IDataObject)log).GetUri());
            Assert.AreEqual(uri, ((IWellObject)log).GetUri());
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_200_Uri()
        {
            var log = new Witsml200.Log { Uuid = _data.Uid() };
            var uri = log.GetUri();

            Assert.IsTrue($"eml://witsml20/Log({ log.Uuid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("log", uri.ObjectType);
            Assert.AreEqual(log.Uuid, uri.ObjectId);
        }
    }
}
