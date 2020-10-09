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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Query
{
    [TestClass]
    public class QueryTemplatesTests
    {
        private const string Xml141Namespace = "{http://www.witsml.org/schemas/1series}";
        private const string Xml131Namespace = "{http://www.witsml.org/schemas/131}";

        [TestMethod]
        public void QueryTemplatesTests_Create_Header_Only_Template_For_Log_131()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.Log, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version131.Value, OptionsIn.ReturnElements.HeaderOnly);

            var xml = template.ToString();
            Console.WriteLine(xml);

            Assert.IsFalse(xml.Contains("logData"));
        }

        [TestMethod]
        public void QueryTemplatesTests_Create_Header_Only_Template_For_Log_141()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.Log, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version141.Value, OptionsIn.ReturnElements.HeaderOnly);

            var xml = template.ToString();
            Console.WriteLine(xml);

            Assert.IsFalse(xml.Contains("logData"));
        }

        [TestMethod]
        public void QueryTemplatesTests_Create_Trajectory_Has_Uom_141()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.Trajectory, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version141.Value, OptionsIn.ReturnElements.All);
            var node = template.Descendants(Xml141Namespace + "location").FirstOrDefault();

            var elementList = new List<string>()
            {
                "latitude",
                "longitude",
                "easting",
                "northing",
                "southing",
                "projectedX",
                "projectedY",
                "localX",
                "localY"
            };

            AssertElementHasAttribute(node, Xml141Namespace, elementList);
        }

        [TestMethod]
        public void QueryTemplatesTests_Create_Trajectory_Has_Uom_131()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.Trajectory, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version131.Value, OptionsIn.ReturnElements.All);
            var node = template.Descendants(Xml131Namespace + "location").FirstOrDefault();

            var elementList = new List<string>()
            {
                "latitude",
                "longitude",
                "easting",
                "northing",
                "southing",
                "projectedX",
                "projectedY",
                "localX",
                "localY"
            };

            AssertElementHasAttribute(node, Xml131Namespace, elementList);
        }

        [TestMethod]
        public void QueryTemplatesTests_Create_CementJob_Has_Uom_141()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.CementJob, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version131.Value, OptionsIn.ReturnElements.All);
            var node = template.Descendants(Xml131Namespace + "cementAdditive").FirstOrDefault();

            var elementList = new List<string>()
            {
                "concentration",
                "wtSack",
                "volSack"
            };

            AssertElementHasAttribute(node, Xml131Namespace, elementList);
        }

        [TestMethod]
        public void QueryTemplatesTests_Create_CementJob_Has_Uom_131()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.CementJob, ObjectFamilies.Witsml, OptionsIn.DataVersion.Version131.Value, OptionsIn.ReturnElements.All);
            var node = template.Descendants(Xml131Namespace + "cementAdditive").FirstOrDefault();

            var elementList = new List<string>()
            {
                "concentration",
                "wtSack",
                "volSack"
            };

            AssertElementHasAttribute(node, Xml131Namespace, elementList);
        }

        private static void AssertElementHasAttribute(XContainer node, string ns, IEnumerable<string> elements)
        {
            Assert.IsNotNull(node);

            elements.ForEach(e =>
                Assert.IsNotNull(node.Element(ns + e)?.Attribute("uom"),
                    $"Element {e} does not have UOM attribute")
                );
        }
    }
}
