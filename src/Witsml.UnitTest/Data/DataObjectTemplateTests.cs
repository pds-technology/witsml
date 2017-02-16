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

using System;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Data
{
    [TestClass]
    public class DataObjectTemplateTests
    {
        [TestMethod]
        public void DataObjectTemplate_Create_Creates_Blank_Xml_Template_For_Well_131()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml131.WellList>();
            var xmlTyped = template.Create(typeof(Witsml131.WellList));

            Console.WriteLine(xmlGeneric);

            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }

        [TestMethod]
        public void DataObjectTemplate_Create_Creates_Blank_Xml_Template_For_Well_141()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml141.WellList>();
            var xmlTyped = template.Create(typeof(Witsml141.WellList));

            Console.WriteLine(xmlGeneric);

            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }

        [TestMethod]
        public void DataObjectTemplate_Create_Creates_Blank_Xml_Template_For_Well_200()
        {
            var template = new DataObjectTemplate();
            var xmlGeneric = template.Create<Witsml200.Well>();
            var xmlTyped = template.Create(typeof(Witsml200.Well));

            Console.WriteLine(xmlGeneric);

            Assert.AreEqual(xmlGeneric.ToString(), xmlTyped.ToString());
        }
    }
}
