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

using System.Collections.Generic;
using System.Linq;
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

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Update_A_List_Element()
        {
            // Add well
            var well = _devKit.CreateFullWell();
            well.Uid = _devKit.Uid();
            AddWell(well);

            // Query well 
            var returnWell = GetWell(well);

            var welldatum = returnWell.WellDatum.FirstOrDefault(x => x.Uid.Equals("SL"));
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Sea Level", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.SL, welldatum.Code);

            // Update well
            var datumSl = _devKit.WellDatum("Sea Level", ElevCodeEnum.LAT, "SL");

            var update = new Well() { Uid = well.Uid, WellDatum = _devKit.List(datumSl) };
            UpdateWell(update);

            // Query updated well
            returnWell = GetWell(well);

            welldatum = returnWell.WellDatum.FirstOrDefault(x => x.Uid.Equals("SL"));
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Sea Level", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.LAT, welldatum.Code);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Update_Well_And_Ignore_Invalid_Element()
        {
            _well.Operator = "AAA Company";

            AddWell(_well);

            // Update well with invalid element
            var updateXml = string.Format(DevKit141Aspect.BasicWellXmlTemplate, _well.Uid,
                "<operator>BBB Company</operator>" + 
                "<fieldsssssss>Big Field</fieldsssssss>");

            var results = _devKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var result = GetWell(_well);
            Assert.AreEqual("BBB Company", result.Operator);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Update_Well_And_Ignore_Invalid_Attribute()
        {
            _well.Operator = "AAA Company";

            AddWell(_well);

            // Update well with invalid element
            var updateXml = string.Format(DevKit141Aspect.BasicWellXmlTemplate, _well.Uid,
                "<operator>BBB Company</operator>" + 
                "<field abc=\"abc\">Big Field</field>");

            var results = _devKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var result = GetWell(_well);
            Assert.AreEqual("BBB Company", result.Operator);
            Assert.AreEqual("Big Field", result.Field);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Update_With_Invalid_Child_Element()
        {
            _well.Operator = "AAA Company";
            AddWell(_well);

            // Update well with invalid element
            var updateXml = string.Format(DevKit141Aspect.BasicWellXmlTemplate, _well.Uid,
                "<operator><abc>BBB Company</abc></operator>");

            var results = _devKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var result = GetWell(_well);
            Assert.AreEqual(_well.Name, result.Name);
            Assert.IsNull(result.Operator);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Error_409_Missing_Required_Fields_For_Optional_Property()
        {
            // Add well
            var response = _devKit.Add<WellList, Well>(_well);

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

            var updateResponse = _devKit.Update<WellList, Well>(update);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding a recurring element for the first time on an UpdateInStore")]
        public void Wel141lDataAdapter_UpdateInStore_Add_Recurring_Element_Success()
        {
            _well.Name = _devKit.Name("WellAddRecurringOnUpdate");
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well that adds a recurring element for the first time on update
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellDatum = new List<WellDatum>
                {
                    _devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "KB"),
                    _devKit.WellDatum("Casing Flange", ElevCodeEnum.CF, "CF")
                }
            };
            var updateResponse = _devKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an nested, non-recurring, element for the first time on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Nested_Element_Success()
        {
            // Add a minimal test well and Assert its Success
            _well.Name = _devKit.Name("WellAddNestedOnUpdate");
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well that adds a nested (non-recurring) element for the first time on update
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellPublicLandSurveySystemLocation =
                    new PublicLandSurveySystem() {PrincipalMeridian = PrincipalMeridian.ChoctawMeridian, Range = 1}
            };
            var updateResponse = _devKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an element with attributes for the first time on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Element_With_Attribute_Success()
        {
            // Add a wellDatum to the test _well
            _well.Name = _devKit.Name("WellAddWithAttributesOnUpdate");
            _well.WellDatum = new List<WellDatum> {_devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "KB")};

            // Add a well with a datum that we can reference in the update
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with a new element that has attributes and Assert Success
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellheadElevation = new WellElevationCoord() { Uom = WellVerticalCoordinateUom.m, Datum = "KB" }
            };
            var updateResponse = _devKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding a nested array element, e.g. referencePoint.location with elements having uom attributes, e.g. latitude during update")]
        public void Well141DataAdapter_UpdateInStore_Add_Nested_Array_Element_With_Uom_Success()
        {
            var well = _devKit.CreateFullWell();
            var referencePoint = well.ReferencePoint;
            well.ReferencePoint = null;
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with a new element that has attributes and Assert Success
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                ReferencePoint = referencePoint
            };
            var updateResponse = _devKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an extensionNameValue field to commonData on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Extension_Name_Value_Success()
        {
            // Add a minimal test well and Assert its Success
            AddWell(_well);

            var extensionName1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");

            // Create an update well that adds a nested (non-recurring) element for the first time on update
            var updateWell = new Well()
            {
                Uid = _well.Uid,
                CommonData = new CommonData
                {
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                       extensionName1
                    }
                }
            };

            var updateResponse = _devKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var result = GetWell(_well);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            var extensionNameValues = commonData.ExtensionNameValue;
            Assert.IsNotNull(extensionNameValues);
            Assert.AreEqual(1, extensionNameValues.Count);
            var extensionName = extensionNameValues.FirstOrDefault();
            Assert.IsNotNull(extensionName);
            Assert.AreEqual(extensionName1.Uid, extensionName.Uid);
            Assert.AreEqual(extensionName1.Name.Name, extensionName.Name.Name);
            Assert.AreEqual(extensionName1.Value.Uom, extensionName.Value.Uom);
            Assert.AreEqual(extensionName1.Value.Value, extensionName.Value.Value);
        }

        private void AddWell(Well well)
        {
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Well GetWell(Well well)
        {
            var query = new Well { Uid = well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }

        private void UpdateWell(Well well)
        {
            var updateResponse = _devKit.Update<WellList, Well>(well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }
    }
}
