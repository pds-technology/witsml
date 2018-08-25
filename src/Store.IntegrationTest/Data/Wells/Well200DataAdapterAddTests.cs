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
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    [TestClass]
    public partial class Well200DataAdapterAddTests : Well200TestBase
    {
        [TestMethod]
        public void Well200DataAdapter_Well_Can_Be_Serialized_To_Xml()
        {
            var xml = EnergisticsConverter.ObjectToXml(Well);
            Console.WriteLine(xml);
            Assert.IsNotNull(xml);
        }

        [TestMethod]
        public void Well200DataAdapter_Well_Can_Be_Parsed_And_Validated()
        {
            var wellTitle = "Well 01 - 160316-000508-840";
            string xml = @"<?xml version=""1.0""?>
                <Well xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" uuid=""81f60891-b69c-4515-870f-0d1954dcac11"" xmlns=""http://www.energistics.org/energyml/data/witsmlv2"">
                    <Citation xmlns=""http://www.energistics.org/energyml/data/commonv2"">
                        <Title>" + wellTitle + @"</Title>
                        <Originator>DevKit200Aspect</Originator>
                        <Creation>2016-03-16T05:05:08.8416296Z</Creation>
                        <Format>PDS.WITSMLstudio.Store.IntegrationTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</Format>
                    </Citation>
                    <PcInterest uom=""%"">100</PcInterest>
                    <TimeZone>-06:00</TimeZone>
                    <GeographicLocationWGS84>
                        <Latitude uom=""dega"">28.5597</Latitude>
                        <Longitude uom=""dega"">-90.6671</Longitude>
                        <Crs xmlns:q1=""http://www.energistics.org/energyml/data/commonv2"" xsi:type=""q1:GeodeticEpsgCrs"">
                            <q1:EpsgCode>26914</q1:EpsgCode>
                        </Crs>
                    </GeographicLocationWGS84>
                </Well>";

            var document = WitsmlParser.Parse(xml);
            var well = WitsmlParser.Parse<Well>(document.Root);
            Assert.IsNotNull(well);
            Assert.AreEqual(wellTitle, well.Citation.Title);
        }

        [TestMethod]
        public void Well200DataAdapter_Can_Add_And_Get_Well()
        {
            AddParents();
            DevKit.AddAndAssert(Well);

            var addedWellbore = DevKit.GetAndAssert(Well);
            Assert.AreEqual(Well.Citation.Title, addedWellbore.Citation.Title);
        }
    }
}
