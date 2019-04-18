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
using System.Collections.Generic;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    public partial class Well141ValidatorTests
    {

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = DevKit.Add<WellList, Well>(Well, string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }


        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_408_Missing_Input_Template()
        {
            var response = DevKit.AddToStore(ObjectTypes.Well, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_409_Non_Conforming_Input_Template()
        {
            WitsmlSettings.DefaultTimeZone = null;

            Well.TimeZone = null; // <-- Missing required TimeZone
            var response = DevKit.Add<WellList, Well>(Well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_411_OptionsIn_Invalid_Format()
        {
            var response = DevKit.Add<WellList, Well>(Well, optionsIn: "compressionMethod:gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ParametersNotEncodedByRules, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_413_Unsupported_Data_Object()
        {
            // Use an unsupported data schema version
            var wells = new WellList
            {
                Well = DevKit.List(Well),
                Version = "1.4.x.y"
            };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotSupported, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_440_OptionsIn_Keyword_Not_Recognized()
        {
            var setting = WitsmlSettings.ThrowForUnsupportedOptionsIn;
            WitsmlSettings.ThrowForUnsupportedOptionsIn = true;

            var response = DevKit.Add<WellList, Well>(Well, optionsIn: "returnElements=all");
            WitsmlSettings.ThrowForUnsupportedOptionsIn = setting;

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_441_optionsIn_value_not_recognized()
        {
            var response = DevKit.Add<WellList, Well>(Well, optionsIn: "compressionMethod=7zip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }


        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_442_OptionsIn_Keyword_Not_Supported()
        {
            WitsmlSettings.IsRequestCompressionEnabled = false;

            var response = DevKit.Add<WellList, Well>(Well, optionsIn: "compressionMethod=gzip");

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

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_444_Mulitple_Data_Objects_Error()
        {
            var well1 = new Well { Name = DevKit.Name("Well-to-01"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            var well2 = new Well { Name = DevKit.Name("Well-to-02"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            var wells = new WellList { Well = DevKit.List(well1, well2) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

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

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod]
        public void DataObjectValidator_AddToStore_Error_464_Child_Uid_Not_Unique()
        {
            var well = DevKit.GetFullWell();
            well.Uid = DevKit.Uid();
            var datumKb = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "This is WellDatum");
            var datumSl = DevKit.WellDatum("Sea Level", ElevCodeEnum.SL, "This is WellDatum");
            well.WellDatum = new List<WellDatum>() { datumKb, datumSl };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_466_Non_Conforming_Capabilities_In()
        {
            var response = DevKit.Add<WellList, Well>(Well, ObjectTypes.Well, "<capClients />");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.CapabilitiesInNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_468_Missing_Version_Attribute()
        {
            // Use an unsupported data schema version
            var wells = new WellList
            {
                Well = DevKit.List(Well),
                Version = null
            };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Error_486_Data_Object_Types_Dont_Match()
        {
            var wells = new WellList { Well = DevKit.List(Well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        // TODO: Find a 141 object that is not supported by store
        //[TestMethod]
        //public void WitsmlValidator_AddToStore_Error_487_Data_Object_Not_Supported()
        //{
        //    var entity = new Target { Name = "Entity-to-test-unsupported-error" };
        //    var list = new TargetList { Target = DevKit.List(entity) };

        //    var xmlIn = EnergisticsConverter.ObjectToXml(list);
        //    var response = DevKit.AddToStore("target", xmlIn, null, null);

        //    Assert.IsNotNull(response);
        //    Assert.AreEqual((short)ErrorCodes.DataObjectTypeNotSupported, response.Result);
        //}

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_438_Recurring_Elements_Inconsistent_Selection()
        {
            var crs1 = DevKit.WellCRS("geog1", null);
            var crs2 = DevKit.WellCRS(null, "ED50 / UTM Zone 31N");
            var query = new Well { Uid = "", WellCRS = DevKit.List(crs1, crs2) };
            var result = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 4.1.5
            Assert.AreEqual((short)ErrorCodes.RecurringItemsInconsistentSelection, result.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_439_Recurring_Elements_Empty_Value()
        {
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
            var setting = WitsmlSettings.ThrowForUnsupportedOptionsIn;
            WitsmlSettings.ThrowForUnsupportedOptionsIn = true;

            var response = DevKit.GetFromStore(ObjectTypes.Well, QueryEmptyObject, null, "optionNotExists=BadValue");
            WitsmlSettings.ThrowForUnsupportedOptionsIn = setting;

            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_441_Invalid_Keyword_Value()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, QueryEmptyObject, null, "returnElements=BadValue");
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var response = DevKit.Get<WellList, Well>(QueryEmptyList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var response = DevKit.Get<WellList, Well>(QueryEmptyList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_Not_ChangeLog()
        {
            var response = DevKit.Get<WellList, Well>(QueryEmptyList, ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var response = DevKit.Get<WellList, Well>(QueryEmptyList, ObjectTypes.Well, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, QueryEmptyRoot, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
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
        public void WitsmlValidator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Minimum_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, QueryEmptyRoot, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_483_Bad_Query_NoWell()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.Well, QueryEmptyRoot, null, optionsIn: null);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var well = new Well { Name = "Well-to-query-missing-witsml-type", TimeZone = DevKit.TimeZone };
            var response = DevKit.Get<WellList, Well>(DevKit.List(well), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, null, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_415_Uid_Missing()
        {
            AddWell(Well);

            // Update Well has no Uid
            var updateWell = new Well() { Country = "test" };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);

            // Assert that uid is missing
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_433_DataObject_Does_Not_Exist()
        {
            AddWell(Well);

            // Update Well has modified uid that does not exist
            var updateWell = new Well() { Country = "test", Uid = Well.Uid + "x" };
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);

            // Assert that the update well does not exist
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_448_Missing_Element_Uid()
        {
            // Add a well to the store
            AddWell(Well, "WellTest448");

            // Add a reference point without a uid
            Well.ReferencePoint = new List<ReferencePoint> { new ReferencePoint() { Name = "rpName" } };
            Well.ReferencePoint[0].Location = new List<Location> { new Location() };

            // Update and Assert MissingElementUid
            var updateResponse = DevKit.Update<WellList, Well>(Well, ObjectTypes.Well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForUpdate, updateResponse.Result);
        }

        [TestMethod]
        public void Well141Validator_UpdateInStore_Error_484_Missing_Required_Data()
        {
            // Add a well to the store
            AddWell(Well, "WellTest484");

            // Clear the well name (required) and update
            Well.Name = string.Empty;

            // Update and Assert MissingRequiredData
            var updateResponse = DevKit.Update<WellList, Well>(Well, ObjectTypes.Well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_444_Input_Template_Multiple_DataObjects()
        {
            // Add a well to the store
            AddWell(Well, "WellTest444");

            var wells = new WellList { Well = DevKit.List(Well, Well) };
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            // Assert that we have multiple wells
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_445_Empty_New_Elements_Or_Attributes()
        {
            // Add a well to the store
            AddWell(Well, "WellTest445");

            Well.ReferencePoint = new List<ReferencePoint> { new ReferencePoint() { Uid = "Test empty reference point" } };

            // Update and Assert that there are empt elements
            var updateResponse = DevKit.Update<WellList, Well>(Well, ObjectTypes.Well);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        [TestMethod]
        public void DataObjectValidator_UpdateInStore_464_Child_Uid_Not_Unique()
        {
            // Add a well to the store and Assert Success
            AddWell(Well, "WellTest464");

            // Create a well with two WellDatum with the same uid and update
            var datumKb = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "This is WellDatum");
            var datumSl = DevKit.WellDatum("Sea Level", ElevCodeEnum.SL, "This is WellDatum");
            Well.WellDatum = new List<WellDatum>() { datumKb, datumSl };
            var updateResponse = DevKit.Update<WellList, Well>(Well);

            // Assert that non-unique uids were found
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_468_Missing_Version_Attribute()
        {
            // Add a well and Assert Success
            AddWell(Well, "Well-to-add-missing-version-attribute");

            var wells = new WellList
            {
                Well = DevKit.List(Well),
                Version = null
            };
            var xmlIn = EnergisticsConverter.ObjectToXml(wells);

            // Update and Assert that the version was missing for update.
            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);
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

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_445_Empty_New_Element()
        {
            AddWell(Well, "Well1Test445");

            var update = new Well
            {
                Uid = Well.Uid,
                WellPublicLandSurveySystemLocation = new PublicLandSurveySystem
                {
                    QuarterTownship = string.Empty,
                    Township = 1,
                }
            };

            var response = DevKit.Update<WellList, Well>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_445_Empty_New_Attribute()
        {
            AddWell(Well, "Well2Test445");

            var update = new Well
            {
                Uid = Well.Uid,
                WellheadElevation = new WellElevationCoord
                {
                    Uom = WellVerticalCoordinateUom.m,
                    Value = 1,
                    Datum = string.Empty
                }
            };

            var response = DevKit.Update<WellList, Well>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, response.Result);
        }

        [TestMethod, Description("When adding a new element has nested uom and value, uom should not be specified if there is no value")]
        public void WitsmlValidator_UpdateInStore_Error_446_Uom_Exist_Without_Value_Nested_Element()
        {
            AddWell(Well, "Well1Test446");

            var updateXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well uid=\"" + Well.Uid + "\">" + Environment.NewLine +
                "<wellheadElevation uom=\"m\" datum=\"KB\"></wellheadElevation>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

            var response = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, response.Result);
        }

        [TestMethod, Description("When adding a new recurring element has nested uom and value, uom should not be specified if there is no value")]
        public void WitsmlValidator_UpdateInStore_Error_446_Uom_Exist_Without_Value_Array_Element()
        {
            AddWell(Well, "Well2Test446");

            var updateXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well uid=\"" + Well.Uid + "\">" + Environment.NewLine +
                    "<wellDatum uid=\"KB\" >" + Environment.NewLine +
                        "<name>Kelly Bushing</name>" + Environment.NewLine +
                        "<code>KB</code>" + Environment.NewLine +
                        "<elevation uom=\"ft\"/>" + Environment.NewLine +
                    "</wellDatum>" + Environment.NewLine +
                    "<wellDatum uid=\"DF\" >" + Environment.NewLine +
                        "<name>Derrick Floor</name>" + Environment.NewLine +
                        "<code>DF</code>" + Environment.NewLine +
                    "</wellDatum>" + Environment.NewLine +
                "</well>" + Environment.NewLine +
                "</wells>";

            var response = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, response.Result);
        }

        [TestMethod, Description("When adding a new recurring element has nested recurring element with uom and value, uom should not be specified if there is no value")]
        public void WitsmlValidator_UpdateInStore_Error_446_Uom_Exist_Without_Value_Nested_Array_Element()
        {
            Well = DevKit.GetFullWell();
            Well.Uid = DevKit.Uid();
            Well.ReferencePoint = null;
            AddWell(Well, "Well3Test446");

            var updateXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                "<well uid=\"" + Well.Uid + "\">" + Environment.NewLine +
                    "<referencePoint uid=\"abc\" >" + Environment.NewLine +
                    "<name>abc</name>" + Environment.NewLine +
                    "<type>abc</type>" + Environment.NewLine +
                    "<elevation uom=\"m\" datum=\"KB\">1</elevation>" + Environment.NewLine +
                    "<measuredDepth uom=\"m\" datum=\"KB\">1</measuredDepth>" + Environment.NewLine +
                    "<location uid=\"abc\">" + Environment.NewLine +
                        "<wellCRS uidRef=\"epsg\">abc</wellCRS>" + Environment.NewLine +
                        "<latitude uom=\"rad\">1</latitude>" + Environment.NewLine +
                        "<longitude uom=\"rad\" />" + Environment.NewLine +
                        "<original>false</original>" + Environment.NewLine +
                        "<description>abc</description>" + Environment.NewLine +
                    "</location>" + Environment.NewLine +
                    "<description>abc</description>" + Environment.NewLine +
                    "</referencePoint>" + Environment.NewLine +
                    "</well>" + Environment.NewLine +
                "</wells>";

            var response = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_446_Uom_With_Null_Measure_Data()
        {
            // Add well
            Well = DevKit.GetFullWell();
            Well.Uid = DevKit.Uid();
            AddWell(Well, "Well4Test446");

            var xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + Well.Uid + "\">" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"ft\"></wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, updateResponse.Result);
        }

        [TestMethod]
        public void WitsmlValidator_UpdateInStore_Error_446_Uom_With_NaN_Measure_Data()
        {
            // Add well
            Well = DevKit.GetFullWell();
            Well.Uid = DevKit.Uid();
            AddWell(Well, "Well5Test446");

            var xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + Well.Uid + "\">" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "     <wellheadElevation uom=\"ft\">NaN</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, updateResponse.Result);
        }

        private void ValidateUpdateUom(string wellName, string uom, ErrorCodes expectedUpdateResult)
        {
            // Add well and get its uid
            Well.Name = DevKit.Name(wellName);
            AddWell(Well);

            // Create an update well with an invalid wellheadElevation
            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"" + Well.Uid + "\">" + Environment.NewLine +
                           "     <wellheadElevation uom=\"" + uom + "\">1000</wellheadElevation>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</wells>";

            var updateResponse = DevKit.UpdateInStore(ObjectTypes.Well, xmlIn, null, null);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)expectedUpdateResult, updateResponse.Result);
        }

        private WMLS_AddToStoreResponse AddWell(Well well, string wellName = null)
        {
            well.Name = wellName ?? well.Name;
            var response = DevKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            return response;
        }
    }
}
