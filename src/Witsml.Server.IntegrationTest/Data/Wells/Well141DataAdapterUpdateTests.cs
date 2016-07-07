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
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Well141DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public class Well141DataAdapterUpdateTests
    {
        private DevKit141Aspect DevKit;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone };
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Can_Update_A_List_Element()
        {
            // Add well
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            // Query well 
            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            var returnWell = result.FirstOrDefault();

            var welldatum = returnWell.WellDatum.Where(x => x.Uid.Equals("SL")).FirstOrDefault();
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Sea Level", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.SL, welldatum.Code);

            // Update well
            var datumSL = DevKit.WellDatum("Sea Level", ElevCodeEnum.LAT, "SL");

            var updateWell = new Well() { Uid = uid, WellDatum = DevKit.List(datumSL) };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query updated well
            query = new Well { Uid = uid };
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            returnWell = result.FirstOrDefault();

            welldatum = returnWell.WellDatum.Where(x => x.Uid.Equals("SL")).FirstOrDefault();
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Sea Level", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.LAT, welldatum.Code);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Error_446_Uom_With_Null_Measure_Data()
        {
            // Add well
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + uid + "\">" + Environment.NewLine +                          
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"ft\"></wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, updateResponse.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Error_446_Uom_With_NaN_Measure_Data()
        {
            // Add well
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + uid + "\">" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"ft\">NaN</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, updateResponse.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Can_Update_Well_And_Ignore_Invalid_Element()
        {
            _well.Name = DevKit.Name("Bug-5855-UpdateInStore-Bad-Element");
            _well.Operator = "AAA Company";

            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Update well with invalid element
            var updateXml = string.Format(DevKit141Aspect.BasicWellXmlTemplate, uidWell,
                "<operator>BBB Company</operator>" + 
                "<fieldsssssss>Big Field</fieldsssssss>");

            var results = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var query = new Well { Uid = uidWell };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("BBB Company", result[0].Operator);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Can_Update_Well_And_Ignore_Invalid_Attribute()
        {
            _well.Name = DevKit.Name("Bug-5855-UpdateInStore-Bad-Attribute");
            _well.Operator = "AAA Company";

            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Update well with invalid element
            var updateXml = string.Format(DevKit141Aspect.BasicWellXmlTemplate, uidWell,
                "<operator>BBB Company</operator>" + 
                "<field abc=\"abc\">Big Field</field>");

            var results = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var query = new Well { Uid = uidWell };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("BBB Company", result[0].Operator);
            Assert.AreEqual("Big Field", result[0].Field);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Can_Update_With_Invalid_Child_Element()
        {
            _well.Name = DevKit.Name("Bug-5855-UpdateInStore-Invalid-Child-Element");
            _well.Operator = "AAA Company";

            var response = DevKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;

            // Update well with invalid element
            var updateXml = string.Format(DevKit141Aspect.BasicWellXmlTemplate, uidWell,
                "<operator><abc>BBB Company</abc></operator>");

            var results = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var query = new Well { Uid = uidWell };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(_well.Name, result[0].Name);
            Assert.IsNull(result[0].Operator);
        }
    }
}
