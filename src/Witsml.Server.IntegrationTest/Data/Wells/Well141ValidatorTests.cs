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
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141ValidatorTests
    {
        private DevKit141Aspect _devKit;
        private string _badQueryNamespace;
        private string _badQueryNoWell;
        private string _queryEmptyWell;
        private List<Well> _queryEmptyWellList;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _badQueryNamespace = "<wells xmlns=\"www.witsml.org/schemas/131\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";
            _badQueryNoWell = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";
            _queryEmptyWell = "<wells  xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";
            _queryEmptyWellList = _devKit.List(new Well());

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well141Validator"), TimeZone = _devKit.TimeZone };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _devKit = null;
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_401_No_Plural_Root_Element()
        {
            var xmlIn = "<well xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well>" + Environment.NewLine +
                           "   <name>Test Add Well Plural Root Element</name>" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</well>";

            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod]
        public void Well141Validator_AddToStore_Error_405_Uid_Exist()
        {
            var response = AddWell(_well);

            var uid = response.SuppMsgOut;
            Assert.AreEqual(_well.Uid, uid);

            response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = _devKit.Add<WellList, Well>(_well, string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }


        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_408_Missing_Input_Template()
        {
            var response = _devKit.AddToStore(ObjectTypes.Well, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_409_Non_Conforming_Input_Template()
        {
            _well.TimeZone = null; // <-- Missing required TimeZone
            var response = _devKit.Add<WellList, Well>(_well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_411_OptionsIn_Invalid_Format()
        {
            var response = _devKit.Add<WellList, Well>(_well, optionsIn: "compressionMethod:gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ParametersNotEncodedByRules, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_413_Unsupported_Data_Object()
        {
            // Use an unsupported data schema version
            var wells = new WellList
            {
                Well = _devKit.List(_well),
                Version = "1.4.x.y"
            };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotSupported, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_440_OptionsIn_Keyword_Not_Recognized()
        {
            var response = _devKit.Add<WellList, Well>(_well, optionsIn: "returnElements=all");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_441_optionsIn_value_not_recognized()
        {
            var response = _devKit.Add<WellList, Well>(_well, optionsIn: "compressionMethod=7zip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }


        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_442_OptionsIn_Keyword_Not_Supported()
        {
            var response = _devKit.Add<WellList, Well>(_well, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        public void DataObjectValidator_AddToStore_Error_443_Invalid_Unit_Of_Measure_Value()
        {
            var xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well>" + Environment.NewLine +
                           "     <name>Well-to-add-missing-unit</name>" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"abc123\">1000</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_444_Mulitple_Data_Objects_Error()
        {
            var well1 = new Well { Name = _devKit.Name("Well-to-01"), TimeZone = _devKit.TimeZone, Uid = _devKit.Uid() };
            var well2 = new Well { Name = _devKit.Name("Well-to-02"), TimeZone = _devKit.TimeZone, Uid = _devKit.Uid() };
            var wells = new WellList { Well = _devKit.List(well1, well2) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, response.Result);
        }

        [TestMethod]
        public void DataObjectValidator_AddToStore_Error_453_Missing_Unit_For_Measure_Data()
        {
            var xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well>" + Environment.NewLine +
                           "     <name>Well-to-add-missing-unit</name>" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation>1000</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void DataObjectValidator_AddToStore_Error_464_Child_Uid_Not_Unique()
        {
            var well = _devKit.CreateFullWell();
            var datumKb = _devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "This is WellDatum");
            var datumSl = _devKit.WellDatum("Sea Level", ElevCodeEnum.SL, "This is WellDatum");
            well.WellDatum = new List<WellDatum>() { datumKb, datumSl };
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_466_Non_Conforming_Capabilities_In()
        {
            var response = _devKit.Add<WellList, Well>(_well, ObjectTypes.Well, "<capClients />");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.CapabilitiesInNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_468_Missing_Version_Attribute()
        {
            // Use an unsupported data schema version
            var wells = new WellList
            {
                Well = _devKit.List(_well),
                Version = null
            };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            var wells = new WellList { Well = _devKit.List(_well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = _devKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_487_Data_Object_Not_Supported()
        {
            var entity = new Target { Name = "Entity-to-test-unsupported-error" };
            var list = new TargetList { Target = _devKit.List(entity) };

            var xmlIn = EnergisticsConverter.ObjectToXml(list);
            var response = _devKit.AddToStore("target", xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypeNotSupported, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_438_Recurring_Elements_Inconsistent_Selection()
        {
            var crs1 = _devKit.WellCRS("geog1", null);
            var crs2 = _devKit.WellCRS(null, "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = _devKit.List(crs1, crs2) };
            var result = _devKit.Get<WellList, Well>(_devKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_439_Recurring_Elements_Empty_Value()
        {          
            var crs1 = _devKit.WellCRS("geog1", string.Empty);
            var crs2 = _devKit.WellCRS("proj1", "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = _devKit.List(crs1, crs2) };
            var result = _devKit.Get<WellList, Well>(_devKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_440_Option_Keyword_Not_Supported()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, _queryEmptyWell, null, "optionNotExists=BadValue");
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_441_Invalid_Keyword_Value()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, _queryEmptyWell, null, "returnElements=BadValue");
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var response = _devKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var response = _devKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_Not_ChangeLog()
        {
            var response = _devKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var response = _devKit.Get<WellList, Well>(_queryEmptyWellList, ObjectTypes.Well, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, _badQueryNoWell, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_MultiChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well/>" + Environment.NewLine +
                              "   <well/>" + Environment.NewLine +
                              "</wells>";

            var response = _devKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_Has_Attribute()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well uid=\"Test Wells\" />" + Environment.NewLine +
                              "</wells>";

            var response = _devKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_409_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_BadChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <log/>" + Environment.NewLine +
                              "</wells>";

            var response = _devKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
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

            var response = _devKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            string queryIn = "<wells version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";

            var response = _devKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_Bad_Namespace()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_None_Bad_Namespace()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Minimum_Query_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, _badQueryNoWell, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_483_Bad_Query_No_Well()
        {
            var response = _devKit.UpdateInStore(ObjectTypes.Well, _badQueryNoWell, null, optionsIn: null);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var well = new Well { Name = "Well-to-query-missing-witsml-type", TimeZone = _devKit.TimeZone };
            var response = _devKit.Get<WellList, Well>(_devKit.List(well), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Well, null, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_415_Uid_Missing()
        {
            AddWell(_well);

            // Update Well has no Uid
            var updateWell = new Well() { Country = "test" };
            var updateResponse = _devKit.Update<WellList, Well>(updateWell);

            // Assert that uid is missing
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_433_DataObject_Does_Not_Exist()
        {
            AddWell(_well);

            // Update Well has modified uid that does not exist
            var updateWell = new Well() { Country = "test", Uid = _well.Uid + "x"};
            var updateResponse = _devKit.Update<WellList, Well>(updateWell);

            // Assert that the update well does not exist
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_448_Missing_Element_Uid()
        {
            // Add a well to the store
            AddWell(_well, "WellTest448");

            // Add a reference point without a uid
            _well.ReferencePoint = new List<ReferencePoint> {new ReferencePoint() {Name = "rpName"} };
            _well.ReferencePoint[0].Location = new List<Location> {new Location()};

            // Update and Assert MissingElementUid
            var updateResponse = _devKit.Update<WellList, Well>(_well, ObjectTypes.Well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForUpdate, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_484_Missing_Required_Data()
        {
            // Add a well to the store
            AddWell(_well, "WellTest484");

            // Clear the well name (required) and update
            _well.Name = string.Empty;

            // Update and Assert MissingRequiredData
            var updateResponse = _devKit.Update<WellList, Well>(_well, ObjectTypes.Well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_444_Input_Template_Multiple_DataObjects()
        {
            // Add a well to the store
            AddWell(_well, "WellTest444");

            var wells = new WellList { Well = _devKit.List(_well, _well) };
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var updateResponse = _devKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            // Assert that we have multiple wells
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_445_Empty_New_Elements_Or_Attributes()
        {
            // Add a well to the store
            AddWell(_well, "WellTest445");

            _well.ReferencePoint = new List<ReferencePoint> { new ReferencePoint() { Uid = "Test empty reference point" } };

            // Update and Assert that there are empt elements
            var updateResponse = _devKit.Update<WellList, Well>(_well, ObjectTypes.Well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        [TestMethod]
        public void DataObjectValidator_UpdateInStore_464_Child_Uid_Not_Unique()
        {
            // Add a well to the store and Assert Success
            AddWell(_well, "WellTest464");

            // Create a well with two WellDatum with the same uid and update
            var datumKb = _devKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "This is WellDatum");
            var datumSl = _devKit.WellDatum("Sea Level", ElevCodeEnum.SL, "This is WellDatum");
            _well.WellDatum = new List<WellDatum>() { datumKb, datumSl };
            var updateResponse = _devKit.Update<WellList, Well>(_well);

            // Assert that non-unique uids were found
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_468_Missing_Version_Attribute()
        {
            // Add a well and Assert Success
            AddWell(_well, "Well-to-add-missing-version-attribute");

            var wells = new WellList
            {
                Well = _devKit.List(_well),
                Version = null
            };
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);

            // Update and Assert that the version was missing for update.
            var updateResponse = _devKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, updateResponse.Result);
        }

        [TestMethod]
        public void DataObjectValidator_UpdateInStore_Error_443_Invalid_Uom()
        {
            ValidateUpdateUom("WellTest443", "abc123", ErrorCodes.InvalidUnitOfMeasure);
        }

        [TestMethod]
        public void DataObjectValidator_UpdateInStore_Error_453_Missing_Uom_For_MeasureData()
        {
            ValidateUpdateUom("WellTest453", string.Empty, ErrorCodes.MissingUnitForMeasureData);
        }

        private void ValidateUpdateUom(string wellName, string uom, ErrorCodes expectedUpdateResult)
        {
            // Add well and get its uid
            _well.Name = _devKit.Name(wellName);
            AddWell(_well);

            // Create an update well with an invalid wellheadElevation
            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + _well.Uid + "\">" + Environment.NewLine +
                           "     <wellheadElevation uom=\"" + uom + "\">1000</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = _devKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)expectedUpdateResult, updateResponse.Result);
        }

        private WMLS_AddToStoreResponse AddWell(Well well, string wellName = null)
        {
            well.Name = wellName ?? well.Name;
            var response = _devKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            return response;
        }
    }
}
