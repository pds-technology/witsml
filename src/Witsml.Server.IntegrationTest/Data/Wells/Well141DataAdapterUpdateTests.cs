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
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
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

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_443_Invalid_Uom()
        {
            ValidateUpdateUom("WellTest443", "abc123", ErrorCodes.InvalidUnitOfMeasure);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_453_Missing_Uom_For_MeasureData()
        {
            ValidateUpdateUom("WellTest453", string.Empty, ErrorCodes.MissingUnitForMeasureData);
        }

        private WMLS_UpdateInStoreResponse ValidateUpdateUom(string wellName, string uom, ErrorCodes expectedUpdateResult)
        {
            // Add well and get its uid
            _well.Name = DevKit.Name(wellName);
            var response = DevKit.Add<WellList, Well>(_well);
            var uid = response.SuppMsgOut;

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with an invalid wellheadElevation
            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + uid + "\">" + Environment.NewLine +
                           "     <wellheadElevation uom=\"" + uom + "\">1000</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)expectedUpdateResult, updateResponse.Result);

            return updateResponse;
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_444_Input_Template_Multiple_DataObjects()
        {
            // Add a well to the store
            var response = AddTestWell(_well, "WellTest444");
            var uid = response.SuppMsgOut;

            Assert.AreEqual((short) ErrorCodes.Success, response.Result);

            // Create a well list with two valid wells and update
            _well.Uid = uid;
            var wells = new WellList { Well = DevKit.List(_well, _well) };
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            // Assert that we have multiple wells
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, updateResponse.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_445_Input_Template_Multiple_DataObjects()
        {
            // Add a well to the store
            var response = AddTestWell(_well, "WellTest445");
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add empty element
            _well.Uid = response.SuppMsgOut;
            _well.ReferencePoint = new List<ReferencePoint> {new ReferencePoint() {Uid = "Test empty reference point"}};

            // Update and Assert that there are empt elements
            var updateResponse = DevKit.Update<WellList, Well>(_well, ObjectTypes.Well, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_464_Child_Uid_Not_Unique()
        {
            // Add a well to the store and Assert Success
            var response = AddTestWell(_well, "WellTest464");
            var uid = response.SuppMsgOut;
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create a well with two WellDatum with the same uid and update
            var datumKb = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "This is WellDatum");
            var datumSl = DevKit.WellDatum("Sea Level", ElevCodeEnum.SL, "This is WellDatum");
            _well.Uid = uid;
            _well.WellDatum = new List<WellDatum>() { datumKb, datumSl };
            var updateResponse = DevKit.Update<WellList, Well>(_well);

            // Assert that non-unique uids were found
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void Test_error_code_468_missing_version_attribute()
        {
            // Add a well and Assert Success
            var response = AddTestWell(_well, "Well-to-add-missing-version-attribute");
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // update Version property to an unsupported data schema version
            _well.Uid = response.SuppMsgOut;
            var wells = new WellList
            {
                Well = DevKit.List(_well),
                Version = null
            };
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);

            // Update and Assert that the version was missing for update.
            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, updateResponse.Result);
        }

        private WMLS_AddToStoreResponse AddTestWell(Well well, string wellName)
        {
            _well.Name = DevKit.Name(wellName);
            var response = DevKit.Add<WellList, Well>(well);
            return response;
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Error_409_Missing_Required_Fields_For_Optional_Property()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(_well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var wellDatum = new WellDatum
            {
                Uid = "DF",
                Code = ElevCodeEnum.DF
            };

            var update = new Well
            {
                Uid = uid,
                WellDatum = new List<WellDatum> {wellDatum}
            };

            var updateResponse = DevKit.Update<WellList, Well>(update);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding a recurring element for the first time on an UpdateInStore")]
        public void WellDataAdapter_UpdateInStore_Add_Recurring_Element_Success()
        {
            _well.Name = DevKit.Name("WellAddRecurringOnUpdate");
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well that adds a recurring element for the first time on update
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellDatum = new List<WellDatum>
                {
                    DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "KB"),
                    DevKit.WellDatum("Casing Flange", ElevCodeEnum.CF, "CF")
                }
            };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an nested, non-recurring, element for the first time on an UpdateInStore")]
        public void WellDataAdapter_UpdateInStore_Add_Nested_Element_Success()
        {
            // Add a minimal test well and Assert its Success
            _well.Name = DevKit.Name("WellAddNestedOnUpdate");
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well that adds a nested (non-recurring) element for the first time on update
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellPublicLandSurveySystemLocation =
                    new PublicLandSurveySystem() {PrincipalMeridian = PrincipalMeridian.ChoctawMeridian, Range = 1}
            };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an element with attributes for the first time on an UpdateInStore")]
        public void WellDataAdapter_UpdateInStore_Add_Element_With_Attribute_Success()
        {
            // Add a wellDatum to the test _well
            _well.Name = DevKit.Name("WellAddWithAttributesOnUpdate");
            _well.WellDatum = new List<WellDatum> {DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "KB")};

            // Add a well with a datum that we can reference in the update
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with a new element that has attributes and Assert Success
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellheadElevation = new WellElevationCoord() { Uom = WellVerticalCoordinateUom.m, Datum = "KB" }
            };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding a nested array element, e.g. referencePoint.location with elements having uom attributes, e.g. latitude during update")]
        public void WellDataAdapter_UpdateInStore_Add_Nested_Array_Element_With_Uom_Success()
        {
            var well = DevKit.CreateFullWell();
            var referencePoint = well.ReferencePoint;
            well.ReferencePoint = null;
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with a new element that has attributes and Assert Success
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                ReferencePoint = referencePoint
            };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }
    }
}
