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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml
{
    /// <summary>
    /// WitsmlParser tests.
    /// </summary>
    [TestClass]
    public class WitsmlParserTests
    {
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
