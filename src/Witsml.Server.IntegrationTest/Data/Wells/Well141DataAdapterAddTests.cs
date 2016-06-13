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

using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141DataAdapterAddTests
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

        [TestCleanup]
        public void TestCleanUp()
        {
            WitsmlSettings.TruncateXmlOutDebugSize = DevKitAspect.DefaultXmlOutDebugSize;
        }

        [TestMethod]
        public void Can_add_well_without_validation()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Add_Well_Error_401_No_Plural_Root_Element()
        {
            string xmlIn = "<well xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well>" + Environment.NewLine +
                           "   <name>Test Add Well Plural Root Element</name>" + Environment.NewLine +
                           "     <timeZone>-06:00</timeZone>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</well>";

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod]
        public void Uid_returned_add_well()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var valid = !string.IsNullOrEmpty(uid);
            Assert.IsTrue(valid);

            well = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(well);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            well = result.FirstOrDefault();
            Assert.IsNotNull(well);
            Assert.AreEqual(uid, well.Uid);
        }

        [TestMethod]
        public void Add_Well_Error_405_Uid_Existed()
        {
            var well = new Well { Name = DevKit.Name("Test Add Well - Uid Existed"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var valid = !string.IsNullOrEmpty(uid);
            Assert.IsTrue(valid);

            var existedWell = new Well { Uid = uid, Name = DevKit.Name("Test Add Well - Adding existed well"), TimeZone = DevKit.TimeZone };
            response = DevKit.Add<WellList, Well>(existedWell);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void Case_preserved_add_well()
        {
            var nameLegal = "Well Legal Name";
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone, NameLegal = nameLegal };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var valid = !string.IsNullOrEmpty(uid);
            Assert.IsTrue(valid);

            well = new Well { Uid = uid, NameLegal = string.Empty };
            var result = DevKit.Query<WellList, Well>(well);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            well = result.FirstOrDefault();
            Assert.IsNotNull(well);
            Assert.AreEqual(nameLegal, well.NameLegal);  // Section 6.1.5
        }

        [TestMethod]
        public void Test_error_code_407_missing_witsml_object_type()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-missing-witsml-type"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWMLtypeIn, response.Result);
        }


        [TestMethod]
        public void Test_error_code_408_missing_input_template()
        {
            var response = DevKit.AddToStore(ObjectTypes.Well, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Test_error_code_409_non_conforming_input_template()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-invalid-input-template") }; // <-- Missing required TimeZone
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void Test_error_code_411_optionsIn_invalid_format()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-invalid-optionsIn-format"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, optionsIn: "compressionMethod:gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ParametersNotEncodedByRules, response.Result);
        }

        [TestMethod]
        public void Test_error_code_413_unsupported_data_object()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-unsupported-error") };
            var wells = new WellList { Well = DevKit.List(well) };

            // update Version property to an unsupported data schema version
            wells.Version = "1.4.x.y";

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotSupported, response.Result);
        }

        [TestMethod]
        public void Test_error_code_440_optionsIn_keyword_not_recognized()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-invalid-optionsIn-keyword") };
            var response = DevKit.Add<WellList, Well>(well, optionsIn: "returnElements=all");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void Test_error_code_441_optionsIn_value_not_recognized()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-invalid-optionsIn-value"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, optionsIn: "compressionMethod=7zip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }


        [TestMethod]
        public void Test_error_code_442_optionsIn_keyword_not_supported()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-optionsIn-keyword-not-supported") };
            var response = DevKit.Add<WellList, Well>(well, optionsIn: "compressionMethod=gzip");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByServer, response.Result);
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void Test_error_code_443_invalid_unit_of_measure_value()
        {
            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
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
        public void Test_error_code_444_mulitple_data_objects_error()
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
        public void Test_error_code_453_missing_unit_for_measure_data()
        {
            string xmlIn = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
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
        public void Add_well_error_464_Child_Uid_Not_Unique()
        {
            var well = DevKit.CreateFullWell();
            var datumKB = DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "This is WellDatum");
            var datumSL = DevKit.WellDatum("Sea Level", ElevCodeEnum.SL, "This is WellDatum");
            well.WellDatum = new List<WellDatum>() { datumKB, datumSL };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void Test_error_code_466_non_conforming_capabilities_in()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-invalid-capabilitiesIn"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, ObjectTypes.Well, "<capClients />");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.CapabilitiesInNonConforming, response.Result);
        }

        [TestMethod]
        public void Test_error_code_468_missing_version_attribute()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-missing-version-attribute") };
            var wells = new WellList { Well = DevKit.List(well) };

            // update Version property to an unsupported data schema version
            wells.Version = null;

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, response.Result);
        }

        [TestMethod]
        public void Test_error_code_486_data_object_types_dont_match()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-data-type-not-match") };
            var wells = new WellList { Well = DevKit.List(well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Test_error_code_487_data_object_not_supported()
        {
            var entity = new Target { Name = "Entity-to-test-unsupported-error" };
            var list = new TargetList { Target = DevKit.List(entity) };

            var xmlIn = EnergisticsConverter.ObjectToXml(list);
            var response = DevKit.AddToStore("target", xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypeNotSupported, response.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_With_PrivateGroupOnly_True()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            // Add a well with PrivateGroupOnly set to false
            _well.CommonData = new CommonData() { PrivateGroupOnly = true };
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = DevKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsFalse(result.Any(x => uidWell.Equals(x.Uid)));

        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_With_PrivateGroupOnly_False()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            // Add a well with PrivateGroupOnly set to false
            _well.CommonData = new CommonData() { PrivateGroupOnly = false };
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = DevKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsTrue(result.Any(x => uidWell.Equals(x.Uid)));
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_With_Default_PrivateGroupOnly()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            // Add a well with default PrivateGroupOnly
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = DevKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsTrue(result.Any(x => uidWell.Equals(x.Uid)));
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well_And_Ignore_Invalid_Element()
        {
            var wellName = DevKit.Name("Bug-5855-AddToStore-Bad-Element");

            string xmlIn = string.Format(DevKit.BasicAddWellXml(), null, wellName, "<fieldsssssss>Big Field</fieldsssssss>");

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well_And_Ignore_Invalid_Attribute()
        {
            var wellName = DevKit.Name("Bug-5855-AddToStore-Bad-Attribute");

            string xmlIn = string.Format(DevKit.BasicAddWellXml(), null, wellName, "<field abc=\"cde\">Big Field</field>");

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = new Well { Uid = response.SuppMsgOut };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Big Field", result[0].Field);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well_With_Invalid_Child_Element()
        {
            var wellName = DevKit.Name("Bug-5855-AddToStore-Invalid-Child-Element");

            string xmlIn = string.Format(DevKit.BasicAddWellXml(), null, wellName, "<field><abc>Big Field</abc></field>");

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = new Well { Uid = response.SuppMsgOut };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(wellName, result[0].Name);
            Assert.IsNull(result[0].Field);
        }
    }
}
