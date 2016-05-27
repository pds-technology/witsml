//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well200DataAdapterAddTests
    {
        private DevKit200Aspect DevKit;
        private IDatabaseProvider Provider;
        private IWitsmlDataAdapter<Well> WellAdapter;

        private Well Well1;
        private Well Well2;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect(TestContext);
            Provider = DevKit.Container.Resolve<IDatabaseProvider>();

            WellAdapter = new Well200DataAdapter(Provider);

            Well1 = new Well()
            {
                Citation = DevKit.Citation("Well 01"),
                GeographicLocationWGS84 = DevKit.Location(),
                TimeZone = DevKit.TimeZone,
                Uuid = DevKit.Uid(),
            };

            Well2 = new Well()
            {
                Citation = DevKit.Citation("Well 02"),
                GeographicLocationWGS84 = DevKit.Location(),
                TimeZone = DevKit.TimeZone,
            };
        }

        [TestMethod]
        public void Well_can_be_serialized_to_xml()
        {
            var xml = EnergisticsConverter.ObjectToXml(Well1);
            Console.WriteLine(xml);
            Assert.IsNotNull(xml);
        }

        [TestMethod]
        public void Well_can_be_parsed_and_validated()
        {
            const string xml = @"<?xml version=""1.0""?>
                <Well xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" uuid=""81f60891-b69c-4515-870f-0d1954dcac11"" xmlns=""http://www.energistics.org/energyml/data/witsmlv2"">
                    <Citation xmlns=""http://www.energistics.org/energyml/data/commonv2"">
                        <Title>Well 01 - 160316-000508-840</Title>
                        <Originator>DevKit200Aspect</Originator>
                        <Creation>2016-03-16T05:05:08.8416296Z</Creation>
                        <Format>PDS.Witsml.Server.IntegrationTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null</Format>
                    </Citation>
                    <PcInterest uom=""%"">100</PcInterest>
                    <TimeZone>-06:00</TimeZone>
                    <GeographicLocationWGS84>
                        <Latitude>28.5597</Latitude>
                        <Longitude>-90.6671</Longitude>
                        <Crs xmlns:q1=""http://www.energistics.org/energyml/data/commonv2"" xsi:type=""q1:GeodeticEpsgCrs"">
                            <q1:EpsgCode>26914</q1:EpsgCode>
                        </Crs>
                    </GeographicLocationWGS84>
                </Well>";

            var context = new RequestContext(Functions.AddToStore, ObjectTypes.Well, xml, null, null);
            var parser = new WitsmlQueryParser(context);

            //WellAdapter.Validate(parser);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellWithUuid()
        {
            WellAdapter.Add(DevKit.Parser(Well1), Well1);

            var well1 = WellAdapter.Get(Well1.GetUri());

            Assert.AreEqual(Well1.Citation.Title, well1.Citation.Title);
        }

        [TestMethod]
        public void CanAddAndGetSingleWellWithoutUuid()
        {
            WellAdapter.Add(DevKit.Parser(Well2), Well2);

            var well2 = Provider.GetDatabase().GetCollection<Well>(ObjectNames.Well200).AsQueryable()
                .First(x => x.Citation.Title == Well2.Citation.Title);

            Assert.AreEqual(Well2.Citation.Title, well2.Citation.Title);
        }
    }
}
