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
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using Shouldly;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// WitsmlParser tests.
    /// </summary>
    [TestClass]
    public class WitsmlParserTests
    {
        private static readonly string _wellXml = @"
                                <wells xmlns=""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">
                                <well>
                                    <name>Test Full Well</name>
                                    <pcInterest uom=""%"">45</pcInterest>
                                </well>
                                </wells>";

        [TestMethod]
        public void WitsmlParser_Parse_Xml_Returns_XDocument()
        {
            var document = WitsmlParser.Parse(" " + _wellXml + "  ");

            Assert.IsNotNull(document);
            Assert.IsNotNull(document.Root);
        }

        [TestMethod]
        public void WitsmlParser_Parse_Invalid_Xml_Returns_409_InputTemplateNonConforming()
        {
            Should.Throw<WitsmlException>(() =>
            {
                WitsmlParser.Parse(string.Empty);
            }).ErrorCode.ShouldBe(ErrorCodes.InputTemplateNonConforming);

            Should.Throw<WitsmlException>(() =>
            {
                WitsmlParser.Parse(null);
            }).ErrorCode.ShouldBe(ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void WitsmlParser_Parse_Xml_Using_Standard_DevKit_Returns_object()
        {
            var xdoc = WitsmlParser.Parse(_wellXml);
            Assert.IsNotNull(xdoc.Root);

            var result = WitsmlParser.Parse<WellList>(xdoc.Root, false);
            Assert.AreEqual(1, result.Well.Count);
            Assert.AreEqual("Test Full Well", result.Well[0].Name);

            //add element with NAN
            var newElement = new XElement(xdoc.Root.GetDefaultNamespace() +  "groundElevation") {Value = "NaN"};
            newElement.Add(new XAttribute("uom", "ft"));
            xdoc.Root.Elements().FirstOrDefault()?.Add(newElement);
            
            var resultRemoveNan = WitsmlParser.Parse<WellList>(xdoc.Root);
            Assert.AreEqual(1, resultRemoveNan.Well.Count);
            Assert.IsNull(resultRemoveNan.Well[0].GroundElevation);
        }

        [TestMethod]
        public void WitsmlParser_Parse_Invalid_Xml_Using_Standard_DevKit_Throws_WitsmlException()
        {
            var xdoc = WitsmlParser.Parse(_wellXml);
            Assert.IsNotNull(xdoc.Root);

            Should.Throw<WitsmlException>(() =>
            {
                xdoc.Root?.Elements().FirstOrDefault()?.Elements().Where(e => e.Name.LocalName == "pcInterest").Attributes().Remove();
                WitsmlParser.Parse<WellList>(xdoc.Root);
            });
        }

        [TestMethod]
        public void WitsmlParser_Parse_Invalid_Xml_Using_Standard_DevKit_Throws_409_InputTemplateNonConforming()
        {
            var xdoc = WitsmlParser.Parse(_wellXml);
            Assert.IsNotNull(xdoc.Root);

            Should.Throw<WitsmlException>(() =>
            {
                WitsmlParser.Parse<Log>(xdoc.Root);
            }).ErrorCode.ShouldBe(ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void WitsmlParser_Parse_Xml_With_Type_Returns_object()
        {
            var xdoc = WitsmlParser.Parse(_wellXml);
            Assert.IsNotNull(xdoc.Root);

            var result = WitsmlParser.Parse(typeof(WellList), xdoc.Root, false) as WellList;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Well.Count);
            Assert.AreEqual("Test Full Well", result.Well[0].Name);
        }

        [TestMethod]
        public void WitsmlParser_Parse_Invalid_Xml_With_Type_Throws_409_InputTemplateNonConforming()
        {
            var xdoc = WitsmlParser.Parse(_wellXml);
            Assert.IsNotNull(xdoc.Root);

            Should.Throw<WitsmlException>(() =>
            {
                WitsmlParser.Parse(typeof(Log), xdoc.Root, false);
            }).ErrorCode.ShouldBe(ErrorCodes.InputTemplateNonConforming);

            Should.Throw<WitsmlException>(() =>
            {
                WitsmlParser.Parse(typeof(WellList), null, false);
            }).ErrorCode.ShouldBe(ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void WitsmlParser_ToXml_Returns_Empty_String_For_Null_Object()
        {
            Assert.AreEqual(string.Empty, WitsmlParser.ToXml(null));
        }

        [TestMethod]
        public void WitsmlParser_ToXml_Returns_Xml_From_Object()
        {
            var inWellObject = WitsmlParser.Parse<WellList>(WitsmlParser.Parse(_wellXml).Root);
            var result = WitsmlParser.ToXml(inWellObject);
            var outWellObject = WitsmlParser.Parse<WellList>(WitsmlParser.Parse(result).Root);

            Assert.AreEqual(inWellObject.Well.Count, outWellObject.Well.Count);
            Assert.AreEqual(inWellObject.Well[0].Name, outWellObject.Well[0].Name);
            Assert.AreEqual(inWellObject.Well[0].PercentInterest.Uom, outWellObject.Well[0].PercentInterest.Uom);
            Assert.AreEqual(inWellObject.Well[0].PercentInterest.Value, outWellObject.Well[0].PercentInterest.Value);
        }

        [TestMethod]
        public void WitsmlParser_RemoveEmptyElements_Removes_Empty_Elements_From_Elements()
        {
            var xdoc = WitsmlParser.Parse(_wellXml);
            Assert.IsNotNull(xdoc.Root);

            var elemPurposeWell = new XElement(xdoc.Root.GetDefaultNamespace() + "purposeWell");
            var elemStatusWell = new XElement(xdoc.Root.GetDefaultNamespace() + "statusWell", "plugged and abandoned");
            var elemGroundElevation = new XElement(xdoc.Root.GetDefaultNamespace() + "groundElevation");
            var nil = XmlUtil.Xsi.GetName("nil");
            elemGroundElevation.Add(new XAttribute(nil, true));

            xdoc.Root.Elements().FirstOrDefault()?.Add(elemPurposeWell);
            xdoc.Root.Elements().FirstOrDefault()?.Add(elemStatusWell);
            xdoc.Root.Elements().FirstOrDefault()?.Add(elemGroundElevation);

            WitsmlParser.RemoveEmptyElements(xdoc.Root);
            var welllist = EnergisticsConverter.XmlToObject<WellList>(xdoc.Root.ToString());

            Assert.IsNotNull(welllist.Well[0].StatusWell);
            Assert.IsNull(welllist.Well[0].PurposeWell);
            Assert.IsNull(welllist.Well[0].GroundElevation);
        }

        [TestMethod]
        public void WitsmlParser_RemoveNaNElements_Removes_NaN_Elements()
        {
            string wellXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
           "<well>" + Environment.NewLine +
           "<name>Test Full Well</name>" + Environment.NewLine +
           "<pcInterest uom=\"%\">NaN</pcInterest>" + Environment.NewLine +
           "</well>" + Environment.NewLine +
           "</wells>";

            var document = WitsmlParser.Parse(wellXml);
            var result = WitsmlParser.RemoveNaNElements<WellList>(document.Root);
            var welllist = EnergisticsConverter.XmlToObject<WellList>(result);

            Assert.IsNull(welllist.Well[0].PercentInterest);
        }

        [TestMethod]
        public void WitsmlParser_RemoveNaNElements_Removes_Nested_NaN_Elements()
        {
            string wellXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
           "<well>" + Environment.NewLine +
           "<name>Test Full Well</name>" + Environment.NewLine +
            "<wellDatum uid=\"KB\">" + Environment.NewLine +
           "    <name>Kelly Bushing</name>" + Environment.NewLine +
           "    <code>KB</code>" + Environment.NewLine +
           "    <elevation uom=\"ft\" datum=\"SL\">NaN</elevation>" + Environment.NewLine +
           "</wellDatum>" + Environment.NewLine +
           "</well>" + Environment.NewLine +
           "</wells>";

            var document = WitsmlParser.Parse(wellXml);
            var result = WitsmlParser.RemoveNaNElements<WellList>(document.Root);
            var welllist = EnergisticsConverter.XmlToObject<WellList>(result);

            Assert.IsNull(welllist.Well[0].WellDatum[0].Elevation);
        }
    }
}
