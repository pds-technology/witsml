//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.WITSMLstudio.Store.Data
{
    [TestClass]
    public class WitsmlQueryTemplateTests
    {
        [TestMethod]
        public void WitsmlQueryTemplate_Can_Create_A_Full_131_Well_Template()
        {
            var template = new WitsmlQueryTemplate<Witsml131.Well>();
            var xml = template.AsXml<Witsml131.WellList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWell>abandoned</statusWell>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_Can_Create_A_Full_131_Wellbore_Template()
        {
            var template = new WitsmlQueryTemplate<Witsml131.Wellbore>();
            var xml = template.AsXml<Witsml131.WellboreList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWellbore>abandoned</statusWellbore>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_Can_Create_A_Full_131_Log_Template()
        {
            var template = new WitsmlQueryTemplate<Witsml131.Log>();
            var xml = template.AsXml<Witsml131.LogList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<direction>decreasing</direction>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_Can_Create_A_Full_141_Well_Template()
        {
            var template = new WitsmlQueryTemplate<Witsml141.Well>();
            var xml = template.AsXml<Witsml141.WellList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWell>abandoned</statusWell>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_Can_Create_A_Full_141_Wellbore_Template()
        {
            var template = new WitsmlQueryTemplate<Witsml141.Wellbore>();
            var xml = template.AsXml<Witsml141.WellboreList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<statusWellbore>abandoned</statusWellbore>"));
        }

        [TestMethod]
        public void WitsmlQueryTemplate_Can_Create_A_Full_141_Log_Template()
        {
            var template = new WitsmlQueryTemplate<Witsml141.Log>();
            var xml = template.AsXml<Witsml141.LogList>();

            Console.WriteLine(xml);
            Assert.IsNotNull(xml);

            Assert.IsTrue(xml.Contains("<direction>decreasing</direction>"));
        }
    }
}
