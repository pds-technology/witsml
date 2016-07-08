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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141ValidatorTests
    {
        private DevKit141Aspect DevKit;
        private string _badQueryNamespace;
        private string _badQueryNoWell;
        private string _queryEmptyWell;
        private List<Well> _queryEmptyWellList;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _badQueryNamespace = "<wells xmlns=\"www.witsml.org/schemas/131\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";
            _badQueryNoWell = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";
            _queryEmptyWell = "<wells  xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";
            _queryEmptyWellList = DevKit.List(new Well());

            _well = new Well { Name = DevKit.Name("Well141Validator"), TimeZone = DevKit.TimeZone };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DevKit = null;
        }

        [TestMethod]
        public void Test_error_code_438_recurring_elements_inconsistent_selection()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var crs1 = DevKit.WellCRS("geog1", null);
            var crs2 = DevKit.WellCRS(null, "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void Test_error_code_439_recurring_elements_empty_value()
        {
            var well = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var crs1 = DevKit.WellCRS("geog1", string.Empty);
            var crs2 = DevKit.WellCRS("proj1", "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_440_Option_Keyword_Not_Supported()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, _queryEmptyWell, null, "optionNotExists=BadValue");
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_441_Invalid_Keyword_Value()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, _queryEmptyWell, null, "returnElements=BadValue");
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var response = DevKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var response = DevKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_Not_ChangeLog()
        {
            var response = DevKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var response = DevKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_RequestObjectSelectionCapability_True_Minimum_Query_Template()
        {
            var response = DevKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, _badQueryNoWell, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_MultiChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well/>" + Environment.NewLine +
                              "   <well/>" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_Has_Attribute()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well uid=\"Test Wells\" />" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_409_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_BadChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <log/>" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_NonEmptyChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well>" + Environment.NewLine +
                              "       <name>Test Wells</name>" + Environment.NewLine +
                              "   </well>" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            string queryIn = "<wells version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_RequestObjectSelectionCapability_None_Minimum_Query_Template()
        {
            var response = DevKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Minimum_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, _badQueryNoWell, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_483_Bad_Query_No_Well()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.Well, _badQueryNoWell, null, optionsIn: null);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var well = new Well { Name = "Well-to-query-missing-witsml-type", TimeZone = DevKit.TimeZone };
            var response = DevKit.Get<WellList, Well>(DevKit.List(well), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWMLtypeIn, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, null, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_415_Uid_Missing()
        {
            var response = AddTestWell(_well);

            // Update Well has no Uid
            var updateWell = new Well() { Country = "test" };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);

            // Assert that uid is missing
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_433_DataObject_Does_Not_Exist()
        {
            var response = AddTestWell(_well);

            // Update Well has modified uid that does not exist
            var updateWell = new Well() { Country = "test", Uid = response.SuppMsgOut + "x"};
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);

            // Assert that the update well does not exist
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_448_Missing_Element_Uid()
        {
            // Add a well to the store
            var response = AddTestWell(_well, "WellTest448");
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add a reference point without a uid
            _well.Uid = response.SuppMsgOut;
            _well.ReferencePoint = new List<ReferencePoint> {new ReferencePoint() {Name = "rpName"} };
            _well.ReferencePoint[0].Location = new List<Location> {new Location()};

            // Update and Assert MissingElementUid
            var updateResponse = DevKit.Update<WellList, Well>(_well, ObjectTypes.Well, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingElementUid, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_484_Missing_Required_Data()
        {
            // Add a well to the store
            var response = AddTestWell(_well, "WellTest484");
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Clear the well name (required) and update
            _well.Uid = response.SuppMsgOut;
            _well.Name = string.Empty;


            // Update and Assert MissingRequiredData
            var updateResponse = DevKit.Update<WellList, Well>(_well, ObjectTypes.Well, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        private WMLS_AddToStoreResponse AddTestWell(Well well, string wellName = null)
        {
            well.Name = wellName ?? well.Name;
            var response = DevKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            return response;
        }
    }
}
