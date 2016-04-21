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
        private DevKit141Aspect DevKit;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestMethod]
        public void Test_return_element_all()
        {
            var well = DevKit.CreateTestWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);
        }

        [TestMethod]
        public void Test_return_element_id_only()
        {
            var well = DevKit.CreateTestWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.ReturnElements.IdOnly).XMLout;
            var context = new RequestContext(Functions.GetFromStore, ObjectTypes.Well, xmlOut, null, null);
            var parser = new WitsmlQueryParser(context);
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
            var well = DevKit.CreateTestWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid, Country = string.Empty, CommonData = new CommonData() };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.ReturnElements.IdOnly).XMLout;
            var context = new RequestContext(Functions.GetFromStore, ObjectTypes.Well, xmlOut, null, null);
            var parser = new WitsmlQueryParser(context);

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
            var well = DevKit.CreateTestWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid, WellDatum = new List<WellDatum> { new WellDatum() } };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, null).XMLout;
            var context = new RequestContext(Functions.GetFromStore, ObjectTypes.Well, xmlOut, null, null);
            var parser = new WitsmlQueryParser(context);
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
            var well = DevKit.CreateTestWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
            AssertTestWell(well, returnWell);

            query = new Well { Uid = uid, CommonData = new CommonData { Comments = string.Empty } };
            var queryIn = EnergisticsConverter.ObjectToXml(new WellList { Well = new List<Well> { query } });
            var xmlOut = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.ReturnElements.Requested).XMLout;
            var context = new RequestContext(Functions.GetFromStore, ObjectTypes.Well, xmlOut, null, null);
            var parser = new WitsmlQueryParser(context);

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
        public void Test_Well_Selection_Uid_ReturnElement_All()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();
           
            well.Uid = uid;
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
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

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
            var uid = DevKit.Uid();
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
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = DevKit.GetFromStore(ObjectTypes.Well, inputXml, null, null);
            var results = EnergisticsConverter.XmlToObject<WellList>(result.XMLout).Well;

            Assert.AreEqual(1, results.Count);
            var returnWell = results.FirstOrDefault();

            Assert.IsNotNull(returnWell);
            Assert.AreEqual(timeStr, returnWell.DateTimeLicense.Value.ToString());
        }

        [TestMethod]
        public void Test_Well_Selection_Uid_Caseless_Compare()
        {
            var testUid = "Test_Well_Selection_Uid_Caseless_Compare_" + DevKit.Uid();
            var query = new Well { Uid = testUid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.IdOnly);

            if (result.Count == 0)
            {
                var well = DevKit.CreateFullWell();
                well.Uid = testUid;
                var response = DevKit.Add<WellList, Well>(well);

                Assert.IsNotNull(response);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            query = new Well { Uid = testUid.ToUpper()};
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsTrue(result.Where(x => x.Uid == testUid).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Different_Case()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var query = new Well { Uid = "", Name = well.Name.ToLower(), NameLegal = well.NameLegal.ToUpper() };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsTrue(result.Where(x => x.Uid == uid).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Criteria_Not_Satisfied()
        {
            var dummy = "Dummy";
            var datumKB = DevKit.WellDatum(dummy);
            var query = new Well { Uid = dummy, Name = dummy, NameLegal = dummy, Country=dummy, County=dummy, WellDatum = DevKit.List(datumKB) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result.XMLout);
            XmlElement wells = doc.DocumentElement;
            Assert.IsNotNull(wells);

            // Section 6.6.4
            Assert.AreEqual(ObjectTypes.SingleToPlural(ObjectTypes.Well), wells.Name);
            Assert.IsFalse(DevKit.HasChildNodes(wells));
        }

        [TestMethod]
        public void Test_Well_Selection_MultiQueries_Same_Object_Returned()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var datumKB = DevKit.WellDatum("Kelly Bushing");       
            var query1 = new Well { Uid = "", WellDatum = DevKit.List(datumKB) };
            var query2 = new Well { Uid = uid };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query1, query2), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

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
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var datumKB = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB);
            var datumSL = DevKit.WellDatum(null, ElevCodeEnum.SL);

            var badWellQuery = new Well { Uid = "", WellDatum = DevKit.List(datumKB, datumSL) };
            var goodWellQuery = new Well { Uid = uid };

            var result = DevKit.Get<WellList, Well>(DevKit.List(goodWellQuery, badWellQuery), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 6.6.4 
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void Test_Well_Selection_Not_Equal_Comparison_dTimCreation()
        {
            var well_01 = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well_01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid_01 = response.SuppMsgOut;
            var now = DateTimeOffset.UtcNow;

            var well_02 = DevKit.CreateFullWell();
            response = DevKit.Add<WellList, Well>(well_02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid_02 = response.SuppMsgOut;

            var query = new Well { CommonData = new CommonData() };
            query.CommonData.DateTimeCreation = now;
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 6.6.4
            Assert.IsTrue(result.Where(x => x.Uid == uid_02).Any());
            Assert.IsFalse(result.Where(x => x.Uid == uid_01).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Not_Equal_Comparison_dTimLastChange()
        {
            var well_01 = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well_01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_01 = response.SuppMsgOut;

            var query = new Well { Uid = uid_01 };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(uid_01, result[0].Uid);

            var wellLastChangeTime = result[0].CommonData.DateTimeLastChange;

            var well_02 = DevKit.CreateFullWell();
            well_02.CommonData.DateTimeCreation = DateTimeOffset.UtcNow;
            response = DevKit.Add<WellList, Well>(well_02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_02 = response.SuppMsgOut;

            query = new Well { CommonData = new CommonData() };
            query.CommonData.DateTimeLastChange = wellLastChangeTime;
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            
            // Section 6.6.4
            Assert.IsTrue(result.Where(x => x.Uid == uid_02).Any());
            Assert.IsFalse(result.Where(x => x.Uid == uid_01).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Do_Not_Return_Empty_Values()
        {
            var well = DevKit.CreateTestWell();
            Assert.IsNull(well.WaterDepth);
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, result.Count);

            // Section 6.6.4.1 
            Assert.IsNull(result[0].WaterDepth);
        }

        [TestMethod]
        public void Test_Well_Selection_Recurring_Items()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var datumKB = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB);
            var datumSL = DevKit.WellDatum("Sea Level", ElevCodeEnum.SL);
            var query = new Well { Uid = "", WellDatum = DevKit.List(datumKB,  datumSL) };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.IsTrue(result.Where(x => x.Uid == uid).Any());
        }

        [TestMethod]
        public void Test_Well_Selection_Recurring_Items_Criteria_OR()
        {
            var well_01 = DevKit.CreateFullWell();
            well_01.WellDatum.RemoveAt(0);            
            var response = DevKit.Add<WellList, Well>(well_01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_01 = response.SuppMsgOut;

            var well_02 = DevKit.CreateFullWell();
            well_02.WellDatum.RemoveAt(1);
            response = DevKit.Add<WellList, Well>(well_02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid_02 = response.SuppMsgOut;

            var datumKB = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB);
            var datumSL = DevKit.WellDatum("Sea Level", ElevCodeEnum.SL);
            var query = new Well { WellDatum = DevKit.List(datumKB, datumSL) };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.IsTrue(result.Where(x => x.Uid == uid_01).Any());
            Assert.IsTrue(result.Where(x => x.Uid == uid_02).Any());
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
