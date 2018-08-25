//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Data
{
    [TestClass]
    public class DataObjectTemplateTests
    {
        [TestMethod]
        public void DataObjectTemplate_Create_Blank_Xml_Template_For_Well_131()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml131.WellList>();
            var xmlTyped = template.Create(typeof(Witsml131.WellList));

            Console.WriteLine(xmlGeneric);

            Assert.IsFalse(ReferenceEquals(xmlGeneric, xmlTyped));
            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }

        [TestMethod]
        public void DataObjectTemplate_Create_Blank_Xml_Template_For_Well_141()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml141.WellList>();
            var xmlTyped = template.Create(typeof(Witsml141.WellList));

            Console.WriteLine(xmlGeneric);

            Assert.IsFalse(ReferenceEquals(xmlGeneric, xmlTyped));
            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }

        [TestMethod]
        public void DataObjectTemplate_Create_Blank_Xml_Template_For_Well_200()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml200.Well>();
            var xmlTyped = template.Create(typeof(Witsml200.Well));

            Console.WriteLine(xmlGeneric);

            Assert.IsFalse(ReferenceEquals(xmlGeneric, xmlTyped));
            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }

        [TestMethod]
        public void DataObjectTemplate_Create_Blank_Xml_Template_For_Log_200()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml200.Log>();
            var xmlTyped = template.Create(typeof(Witsml200.Log));

            Console.WriteLine(xmlGeneric);

            Assert.IsFalse(ReferenceEquals(xmlGeneric, xmlTyped));
            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }

        [TestMethod]
        public void DataObjectTemplate_Create_Header_Only_Template_For_Log_131()
        {
            var template = new DataObjectTemplate();
            var document = template.Create<Witsml131.LogList>();

            template.Remove(document, "//logData");

            var xml = document.ToString();
            Console.WriteLine(xml);

            Assert.IsFalse(xml.Contains("logData"));
        }

        [TestMethod]
        public void DataObjectTemplate_Remove_And_Ignore_Elements_For_Log_141()
        {
            var template = new DataObjectTemplate(new [] { "CommonData", "CustomData" });
            var document = template.Create<Witsml141.LogList>();

            template.Remove(document, "//logCurveInfo", "//logData");

            var xml = document.ToString();
            Console.WriteLine(xml);

            Assert.IsFalse(xml.Contains("logCurveInfo"), "logCurveInfo");
            Assert.IsFalse(xml.Contains("logData"), "logData");
            //Assert.IsFalse(xml.Contains("commonData"), "commonData");
            //Assert.IsFalse(xml.Contains("customData"), "customData");
        }

        [TestMethod]
        public void DataObjectTemplate_Remove_And_Set_Element_Values_For_Log_141()
        {
            var template = new DataObjectTemplate(new[] { "CommonData", "CustomData" });
            var document = template.Create<Witsml141.LogList>();

            template
                .Remove(document, "//startDateTimeIndex", "//endDateTimeIndex", "//logCurveInfo/*", "//logCurveInfo/@*", "//logParam")
                .Set(document, "//startIndex", 0.0)
                .Set(document, "//startIndex/@uom", "m")
                .Set(document, "//endIndex", 100.5)
                .Set(document, "//endIndex/@uom", "m");

            var xml = document.ToString();
            Console.WriteLine(xml);

            Assert.IsTrue(xml.Contains("<startIndex uom=\"m\">0</startIndex>"), "startIndex");
            Assert.IsTrue(xml.Contains("<endIndex uom=\"m\">100.5</endIndex>"), "endIndex");
            Assert.IsTrue(xml.Contains("<logCurveInfo />"), "logCurveInfo");
        }
    }
}
