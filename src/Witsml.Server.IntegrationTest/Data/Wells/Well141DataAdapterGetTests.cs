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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141DataAdapterGetTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };
        }

        [TestMethod]
        public void Well141DataProvider_GetFromStore_Query_OptionsIn_requestObjectSelectionCapability()
        {
            var well = new Well();
            var result = _devKit.Query<WellList, Well>(well, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            well = result.FirstOrDefault();
            Assert.IsNotNull(well);
            Assert.AreEqual("abc", well.Uid);
            Assert.IsNotNull(well.StatusWell);
            Assert.IsTrue(well.WellLocation.Count == 1);
            Assert.AreEqual(1, well.PercentInterest.Value);
            Assert.IsNotNull(well.CommonData.DateTimeLastChange);
        }

        [TestMethod]
        public void Well141DataProvider_GetFromStore_Query_OptionsIn_PrivateGroupOnly()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            var well = new Well { Name = _devKit.Name("Well-to-add-01"), TimeZone = _devKit.TimeZone };
            var response = _devKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            CommonData commonData = new CommonData();
            commonData.PrivateGroupOnly = true;
            well = new Well { Name = _devKit.Name("Well-to-add-01"), TimeZone = _devKit.TimeZone, CommonData = commonData };
            response = _devKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var valid = !string.IsNullOrEmpty(uid);
            Assert.IsTrue(valid);

            well = new Well();
            var result = _devKit.Query<WellList, Well>(well, optionsIn: OptionsIn.ReturnElements.All + ";" + OptionsIn.RequestPrivateGroupOnly.True);
            Assert.IsNotNull(result);

            var notPrivateGroupWells = result.Where(x =>
            {
                bool isPrivate = x.CommonData.PrivateGroupOnly ?? false;
                return !isPrivate;
            });
            Assert.IsFalse(notPrivateGroupWells.Any());

            well = result.FirstOrDefault(x => uid.Equals(x.Uid));
            Assert.IsNotNull(well);
        }

        [TestMethod]
        public void Test_return_element_all()
        {
            var well = _devKit.CreateTestWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);
        }

        [TestMethod]
        public void Test_return_element_id_only()
        {
            var well = _devKit.CreateTestWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.ReturnElements.IdOnly).XMLout;
            var document = WitsmlParser.Parse(xmlOut);
            var parser = new WitsmlQueryParser(document.Root, ObjectTypes.Well, null);
            Assert.IsFalse(parser.HasElements("wellDatum"));

            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlOut);
            Assert.AreEqual(1, wellList.Well.Count);
            returnWell = wellList.Well.FirstOrDefault();

            Assert.AreEqual(well.Name, returnWell.Name);
            Assert.IsNull(returnWell.DateTimeSpud);
            Assert.IsNull(returnWell.GroundElevation);
        }

        [TestMethod]
        public void Test_return_element_id_only_with_additional_elements()
        {
            var well = _devKit.CreateTestWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid, Country = string.Empty, CommonData = new CommonData() };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.ReturnElements.IdOnly).XMLout;
            var document = WitsmlParser.Parse(xmlOut);
            var parser = new WitsmlQueryParser(document.Root, ObjectTypes.Well, null);

            Assert.IsTrue(parser.HasElements("country"));
            Assert.IsTrue(parser.HasElements("commonData"));
            Assert.IsFalse(parser.HasElements("wellDatum"));
            
            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlOut);
            Assert.AreEqual(1, wellList.Well.Count);
            returnWell = wellList.Well.FirstOrDefault();

            Assert.AreEqual(well.Name, returnWell.Name);
            Assert.AreEqual(well.Country, returnWell.Country);
            Assert.AreEqual(well.CommonData.ItemState.ToString(), returnWell.CommonData.ItemState.ToString());
            Assert.AreEqual(well.CommonData.Comments, returnWell.CommonData.Comments);
            Assert.IsNull(returnWell.DateTimeSpud);
            Assert.IsNull(returnWell.GroundElevation);
        }

        [TestMethod]
        public void Test_return_element_default()
        {
            var well = _devKit.CreateTestWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid, WellDatum = new List<WellDatum> { new WellDatum() } };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, null).XMLout;
            var document = WitsmlParser.Parse(xmlOut);
            var parser = new WitsmlQueryParser(document.Root, ObjectTypes.Well, null);
            Assert.IsFalse(parser.HasElements("name"));

            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlOut);
            Assert.AreEqual(1, wellList.Well.Count);
            returnWell = wellList.Well.FirstOrDefault();

            Assert.IsNull(returnWell.DateTimeSpud);
            Assert.IsNull(returnWell.GroundElevation);
            Assert.IsNull(returnWell.CommonData);

            foreach (var datum in well.WellDatum)
            {
                var returnDatum = returnWell.WellDatum.FirstOrDefault(d => d.Uid == datum.Uid);
                Assert.IsNotNull(returnDatum);
                Assert.AreEqual(datum.Code, returnDatum.Code);
            }
        }

        [TestMethod]
        public void Test_return_element_requested()
        {
            var well = _devKit.CreateTestWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid, CommonData = new CommonData { Comments = string.Empty } };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.ReturnElements.Requested).XMLout;
            var document = WitsmlParser.Parse(xmlOut);
            var parser = new WitsmlQueryParser(document.Root, ObjectTypes.Well, null);

            Assert.IsFalse(parser.HasElements("name"));
            Assert.IsFalse(parser.HasElements("wellDatum"));

            var wellList = EnergisticsConverter.XmlToObject<WellList>(xmlOut);
            Assert.AreEqual(1, wellList.Well.Count);
            returnWell = wellList.Well.FirstOrDefault();

            Assert.IsNull(returnWell.DateTimeSpud);
            Assert.IsNull(returnWell.GroundElevation);

            var commonData = returnWell.CommonData;

            Assert.IsNotNull(commonData);
            Assert.IsFalse(string.IsNullOrEmpty(commonData.Comments));
            Assert.IsNull(commonData.DateTimeLastChange);
        }

        [TestMethod]
        public void Query_OptionsIn_ReturnElements_IdOnly()
        {
            var wellName = _devKit.Name("Well-to-add-01");

            var well = new Well { Name = wellName, TimeZone = _devKit.TimeZone, NameLegal = "Company Legal Name", Field = "Big Field" };
            var response = _devKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var queryWell = new Well { Name = wellName };
            var result = _devKit.Get<WellList, Well>(_devKit.List(queryWell), optionsIn: OptionsIn.ReturnElements.IdOnly);
            Assert.IsNotNull(result);

            var xmlout = result.XMLout;
            var doc = new XmlDocument();
            doc.LoadXml(xmlout);
            var wells = doc.DocumentElement;

            var uidExists = false;
            foreach (XmlNode node in wells.ChildNodes)
            {
                uidExists = true;
                Assert.IsTrue(node.Attributes.Count == 1);
                Assert.IsTrue(node.HasChildNodes);
                Assert.AreEqual(1, node.ChildNodes.Count);
                Assert.AreEqual("name", node.ChildNodes[0].Name);
            }
            Assert.IsTrue(uidExists);
        }

        [TestMethod]
        public void Query_OptionsIn_ReturnElements_Requested()
        {
            var wellName = _devKit.Name("Well-to-add-01");

            var well = new Well { Name = wellName, TimeZone = _devKit.TimeZone, NameLegal = "Company Legal Name", Field = "Big Field" };
            var response = _devKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            string uid = response.SuppMsgOut;

            var queryWell = new Well { Uid = uid, Name = wellName, NameLegal = "", Field = "" };
            var result = _devKit.Get<WellList, Well>(_devKit.List(queryWell), optionsIn: OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            var xmlout = result.XMLout;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlout);
            XmlElement wells = doc.DocumentElement;

            bool uidExists = false;
            foreach (XmlNode node in wells.ChildNodes)
            {
                Assert.AreEqual(1, node.Attributes.Count);
                Assert.IsTrue(node.HasChildNodes);
                Assert.IsTrue(node.ChildNodes.Count <= 3);
                Assert.AreEqual("name", node.ChildNodes[0].Name);
                Assert.AreEqual(wellName, node.ChildNodes[0].InnerText);

                if (uid.Equals(node.Attributes[0].InnerText))
                {
                    uidExists = true;
                    Assert.AreEqual("nameLegal", node.ChildNodes[1].Name);
                    Assert.AreEqual("Company Legal Name", node.ChildNodes[1].InnerText);
                    Assert.AreEqual("field", node.ChildNodes[2].Name);
                    Assert.AreEqual("Big Field", node.ChildNodes[2].InnerText);
                }
            }

            Assert.IsTrue(uidExists);
        }

        [TestMethod]
        public void WitsmlDataProvider_GetFromStore_Get_Full_Well()
        {
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
           
            well.Uid = uid;
            well.CommonData.DateTimeCreation = returnWell.CommonData.DateTimeCreation;
            well.CommonData.DateTimeLastChange = returnWell.CommonData.DateTimeLastChange;                        
            string wellXml = EnergisticsConverter.ObjectToXml(well);
            string returnXml = EnergisticsConverter.ObjectToXml(returnWell);

            Assert.AreEqual(wellXml, returnXml);
        }

        [TestMethod]
        public void Test_Well_Selection_Uid_ReturnElement_All_dTimLicense_With_Offset()
        {
            string inputXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well>" + Environment.NewLine +
                "<name>PDS Full Test Well</name>" + Environment.NewLine +
                "<dTimLicense>2001-05-15T13:20:00-05:00</dTimLicense>" + Environment.NewLine +
                "<timeZone>-06:00</timeZone>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

            WellList wells = EnergisticsConverter.XmlToObject<WellList>(inputXml);
            var well = wells.Items[0] as Well;
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();

            well.Uid = uid;
            well.CommonData = returnWell.CommonData;
            string wellXml = EnergisticsConverter.ObjectToXml(well);
            string returnXml = EnergisticsConverter.ObjectToXml(returnWell);

            Assert.AreEqual(wellXml, returnXml);
        }

        [TestMethod]
        public void Test_Well_query_by_dTimLicense_with_custom_timestamp()
        {
            var uid = _devKit.Uid();
            var timeStr = "2001-05-15T13:20:00.0000000+00:00";

            string inputXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well uid=\"" + uid + "\">" + Environment.NewLine +
                "<name>PDS Full Test Well</name>" + Environment.NewLine +
                "<dTimLicense>" + timeStr + "</dTimLicense>" + Environment.NewLine +
                "<timeZone>-06:00</timeZone>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

            WellList wells = EnergisticsConverter.XmlToObject<WellList>(inputXml);
            var well = wells.Items[0] as Well;
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = _devKit.GetFromStore(ObjectTypes.Well, inputXml, null, null);
            var results = EnergisticsConverter.XmlToObject<WellList>(result.XMLout).Well;

            Assert.AreEqual(1, results.Count);
            var returnWell = results.FirstOrDefault();

            Assert.IsNotNull(returnWell);
            Assert.AreEqual(timeStr, returnWell.DateTimeLicense.Value.ToString());
        }

        [TestMethod]
        public void Test_Well_Selection_Uid_Caseless_Compare()
        {
            var testUid = "Test_Well_Selection_Uid_Caseless_Compare_" + _devKit.Uid();
            var query = new Well { Uid = testUid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.IdOnly);

            if (result.Count == 0)
            {
                var well = _devKit.CreateFullWell();
                well.Uid = testUid;
                var response = _devKit.Add<WellList, Well>(well);

                Assert.IsNotNull(response);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            query = new Well { Uid = testUid.ToUpper()};
            result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsTrue(result.Where(x => x.Uid == testUid).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Different_Case()
        {
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var query = new Well { Uid = "", Name = well.Name.ToLower(), NameLegal = well.NameLegal.ToUpper() };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsTrue(result.Where(x => x.Uid == uid).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Criteria_Not_Satisfied()
        {
            var dummy = "Dummy";
            var datumKB = _devKit.WellDatum(dummy);
            var query = new Well { Uid = dummy, Name = dummy, NameLegal = dummy, Country=dummy, County=dummy, WellDatum = _devKit.List(datumKB) };
            var result = _devKit.Get<WellList, Well>(_devKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result.XMLout);
            XmlElement wells = doc.DocumentElement;
            Assert.IsNotNull(wells);

            // Section 6.6.4
            Assert.AreEqual(ObjectTypes.SingleToPlural(ObjectTypes.Well), wells.Name);
            Assert.IsFalse(_devKit.HasChildNodes(wells));
        }

        [TestMethod]
        public void Test_Well_Selection_MultiQueries_Same_Object_Returned()
        {
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var datumKB = _devKit.WellDatum("Kelly Bushing");       
            var query1 = new Well { Uid = "", WellDatum = _devKit.List(datumKB) };
            var query2 = new Well { Uid = uid };
            var result = _devKit.Get<WellList, Well>(_devKit.List(query1, query2), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.XMLout);
            var resultWellList = EnergisticsConverter.XmlToObject<WellList>(result.XMLout);

            Assert.IsNotNull(resultWellList);
            var sameWellList = resultWellList.Items.Cast<Well>().Where(x => x.Uid == uid);

            // Section 6.6.4.1
            Assert.IsTrue(sameWellList.Count() > 1);
        }

        [TestMethod]
        public void Test_Well_Selection_MultiQueries_One_Query_Fails()
        {
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var datumKB = _devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB);
            var datumSL = _devKit.WellDatum(null, ElevCodeEnum.SL);

            var badWellQuery = new Well { Uid = "", WellDatum = _devKit.List(datumKB, datumSL) };
            var goodWellQuery = new Well { Uid = uid };

            var result = _devKit.Get<WellList, Well>(_devKit.List(goodWellQuery, badWellQuery), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 6.6.4 
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void Test_Well_Selection_Not_Equal_Comparison_dTimCreation()
        {
            var well_01 = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well_01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid_01 = response.SuppMsgOut;
            var now = DateTimeOffset.UtcNow;

            var well_02 = _devKit.CreateFullWell();
            response = _devKit.Add<WellList, Well>(well_02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid_02 = response.SuppMsgOut;

            var query = new Well { CommonData = new CommonData() };
            query.CommonData.DateTimeCreation = now;
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 6.6.4
            Assert.IsTrue(result.Where(x => x.Uid == uid_02).Any());
            Assert.IsFalse(result.Where(x => x.Uid == uid_01).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Not_Equal_Comparison_dTimLastChange()
        {
            var well_01 = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well_01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_01 = response.SuppMsgOut;

            var query = new Well { Uid = uid_01 };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(uid_01, result[0].Uid);

            var wellLastChangeTime = result[0].CommonData.DateTimeLastChange;

            var well_02 = _devKit.CreateFullWell();
            well_02.CommonData.DateTimeCreation = DateTimeOffset.UtcNow;
            response = _devKit.Add<WellList, Well>(well_02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_02 = response.SuppMsgOut;

            query = new Well { CommonData = new CommonData() };
            query.CommonData.DateTimeLastChange = wellLastChangeTime;
            result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            
            // Section 6.6.4
            Assert.IsTrue(result.Where(x => x.Uid == uid_02).Any());
            Assert.IsFalse(result.Where(x => x.Uid == uid_01).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Do_Not_Return_Empty_Values()
        {
            var well = _devKit.CreateTestWell();
            Assert.IsNull(well.WaterDepth);
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, result.Count);

            // Section 6.6.4.1 
            Assert.IsNull(result[0].WaterDepth);
        }

        [TestMethod]
        public void Test_Well_Selection_Recurring_Items()
        {
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var datumKB = _devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB);
            var datumSL = _devKit.WellDatum("Sea Level", ElevCodeEnum.SL);
            var query = new Well { Uid = "", WellDatum = _devKit.List(datumKB,  datumSL) };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsTrue(result.Where(x => x.Uid == uid).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Recurring_Items_Criteria_OR()
        {
            var well_01 = _devKit.CreateFullWell();
            well_01.WellDatum.RemoveAt(0);            
            var response = _devKit.Add<WellList, Well>(well_01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_01 = response.SuppMsgOut;

            var well_02 = _devKit.CreateFullWell();
            well_02.WellDatum.RemoveAt(1);
            response = _devKit.Add<WellList, Well>(well_02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_02 = response.SuppMsgOut;

            var datumKB = _devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB);
            var datumSL = _devKit.WellDatum("Sea Level", ElevCodeEnum.SL);
            var query = new Well { WellDatum = _devKit.List(datumKB, datumSL) };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.IsTrue(result.Where(x => x.Uid == uid_01).Any());
            Assert.IsTrue(result.Where(x => x.Uid == uid_02).Any());
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Supports_NaN_With_Property_And_XElement_Name_Not_Same()
        {
            // Add well
            _well.PercentInterest = new DimensionlessMeasure(99.8, DimensionlessUom.Euc);
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Query well with NaN
            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<well uid=\"" + uidWell + "\">" + Environment.NewLine +
                         "<pcInterest uom=\"Euc\">NaN</pcInterest>" + Environment.NewLine +
                    "</well>" + Environment.NewLine +
               "</wells>";

            var results = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(results.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual("Euc", wellList.Well[0].PercentInterest.Uom.ToString());
            Assert.AreEqual(99.8, wellList.Well[0].PercentInterest.Value);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Supports_NaN_On_Class_Property()
        {
            // Add well
            _well.WellDatum = new List<WellDatum>();
            var datum = _devKit.WellDatum("Kelly Bushing", code: ElevCodeEnum.KB, uid: ElevCodeEnum.KB.ToString());
            datum.Elevation = new WellElevationCoord() { Uom = WellVerticalCoordinateUom.ft, Value = 99.8 };
            _well.WellDatum.Add(datum);

            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Query well with NaN
            var queryIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<well uid=\"" + uidWell + "\">" + Environment.NewLine +
                           "<wellDatum uid=\"KB\">" + Environment.NewLine +
                           "    <name>Kelly Bushing</name>" + Environment.NewLine +
                           "    <code>KB</code>" + Environment.NewLine +
                           "    <elevation uom=\"ft\">NaN</elevation>" + Environment.NewLine +
                           "</wellDatum>" + Environment.NewLine +
                    "</well>" + Environment.NewLine +
               "</wells>";

            var results = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(results.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual("Kelly Bushing", wellList.Well[0].WellDatum[0].Name);
            Assert.AreEqual(99.8, wellList.Well[0].WellDatum[0].Elevation.Value);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Can_Get_Measure_Data_With_Uom_And_Null()
        {
            // Add well
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well> uid=\"" + uid + "\"" + Environment.NewLine +
                           "     <name>" + well.Name + "</name>" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"ft\"></wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var getResponse = _devKit.GetFromStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(getResponse);
            Assert.AreEqual((short)ErrorCodes.Success, getResponse.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(getResponse.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual(500, wellList.Well[0].WellheadElevation.Value);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Can_Get_Uom_Data_OptionsIn_Requested()
        {
            // Add well
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well> uid=\"" + uid + "\"" + Environment.NewLine +
                           "     <name>" + well.Name + "</name>" + Environment.NewLine +
                           "    <pcInterest uom=\"" + well.PercentInterest.Uom + "\">" + "</pcInterest>" + Environment.NewLine +
                           "    <wellDatum uid=\"" + well.WellDatum[0].Uid + "\">" + Environment.NewLine +
                           "      <name>" + well.WellDatum[0].Name + "</name>" + Environment.NewLine +
                           "      <code>" + well.WellDatum[0].Code + "</code>" + Environment.NewLine +
                           "      <elevation uom=\"" + well.WellDatum[0].Elevation.Uom + "\">" + "</elevation>" + Environment.NewLine +
                           "    </wellDatum>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            // Make a requested query
            var getResponse = _devKit.GetFromStore(ObjectTypes.Well, xmlIn, null, "returnElements=requested");
            Assert.IsNotNull(getResponse);
            Assert.AreEqual((short)ErrorCodes.Success, getResponse.Result);

            // Convert the XMLout to a well list.
            var wellList = EnergisticsConverter.XmlToObject<WellList>(getResponse.XMLout);

            // Test that our well was returned in the output
            Assert.AreEqual(1, wellList.Well.Count);

            // Test that the queriedWell's uom and uom values are the same as the added well after a requested query
            var queriedWell = wellList.Well[0];
            Assert.AreEqual(well.PercentInterest.Uom, queriedWell.PercentInterest.Uom);
            Assert.AreEqual(well.PercentInterest.Value, queriedWell.PercentInterest.Value);
            Assert.AreEqual(well.WellDatum[0].Elevation.Uom, queriedWell.WellDatum[0].Elevation.Uom);
            Assert.AreEqual(well.WellDatum[0].Elevation.Value, queriedWell.WellDatum[0].Elevation.Value);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Can_Get_Measure_Data_With_Uom_And_NaN()
        {
            // Add well
            var well = _devKit.CreateFullWell();
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well> uid=\"" + uid + "\"" + Environment.NewLine +
                            "     <name>" + well.Name + "</name>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"ft\">NaN</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var getResponse = _devKit.GetFromStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(getResponse);
            Assert.AreEqual((short)ErrorCodes.Success, getResponse.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(getResponse.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual(500, wellList.Well[0].WellheadElevation.Value);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Can_Get_Well_And_Ignore_Invalid_Element()
        {
            _well.Name = _devKit.Name("Bug-5855-GetFromStore-Bad-Element");
            _well.Operator = "AAA Company";

            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Query well with invalid element
            var queryIn = string.Format(DevKit141Aspect.BasicWellXmlTemplate, uidWell,
                "<operator/>" +
                "<fieldsssssss>Big Field</fieldsssssss>");

            var results = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(results.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual("AAA Company", wellList.Well[0].Operator);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Can_Get_Well_And_Ignore_Invalid_Attribute()
        {
            _well.Name = _devKit.Name("Bug-5855-GetFromStore-Bad-Attribute");
            _well.Operator = "AAA Company";
            _well.Field = "Very Big Field";

            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Query well with invalid attribute
            var queryIn = string.Format(DevKit141Aspect.BasicWellXmlTemplate, uidWell,
                "<operator/>" +
                "<field abc=\"abc\"></field>");

            var results = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(results.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual("AAA Company", wellList.Well[0].Operator);
            Assert.AreEqual("Very Big Field", wellList.Well[0].Field);
        }

        [TestMethod]
        public void Well141DataAdapter_GetFromStore_Can_Get_Well_With_Invalid_Child_Element()
        {
            _well.Name = _devKit.Name("Bug-5855-UpdateInStore-Invalid-Child-Element");
            _well.Operator = "AAA Company";

            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Query well with invalid attribute
            var queryIn = string.Format(DevKit141Aspect.BasicWellXmlTemplate, uidWell,
                "<name/>" +
                "<operator><abc>BBB Company</abc></operator>");

            var results = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, "returnElements=requested");
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            var wellList = EnergisticsConverter.XmlToObject<WellList>(results.XMLout);
            Assert.AreEqual(1, wellList.Well.Count);
            Assert.AreEqual(_well.Name, wellList.Well[0].Name);
            Assert.AreEqual("AAA Company", wellList.Well[0].Operator);
        }

        private void AssertTestWell(Well expected, Well actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Country, actual.Country);
            Assert.AreEqual(expected.DateTimeSpud.ToString(), actual.DateTimeSpud.ToString());
            Assert.AreEqual(expected.GroundElevation.Value, actual.GroundElevation.Value);
            Assert.AreEqual(expected.WellDatum.Count, actual.WellDatum.Count);

            foreach (var datum in expected.WellDatum)
            {
                var returnDatum = actual.WellDatum.FirstOrDefault(d => d.Uid == datum.Uid);
                Assert.IsNotNull(returnDatum);
                Assert.AreEqual(datum.Code, returnDatum.Code);
            }

            Assert.IsNotNull(actual.CommonData);
            Assert.IsNotNull(actual.CommonData.DateTimeLastChange);
            Assert.AreEqual(expected.CommonData.ItemState, actual.CommonData.ItemState);
            Assert.AreEqual(expected.CommonData.Comments, actual.CommonData.Comments);
        }
    }
}
