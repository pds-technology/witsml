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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// AddWellboreValidator test class
    /// </summary>
    [TestClass]
    public class Wellbore141ValidatorTests
    {
        private DevKit141Aspect DevKit;
        private List<Wellbore> _query;

        private static readonly string _badQueryEmptyWellboreList =
            "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
            "</wellbores>";

        private static readonly string _badQueryNamespace = 
            "<wellbores xmlns=\"www.witsml.org/schemas/131\" version = \"1.4.1.1\" >" + Environment.NewLine +
            "</wellbores>";

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _query = DevKit.List(new Wellbore());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DevKit = null;
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully
        /// </summary>
        [TestMethod]
        public void Validate_wellbore()
        {
            var wellName = DevKit.Name("Well-to-add-01");
            var well = new Well { Name = wellName, TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = wellName,
                Name = DevKit.Name("Wellbore 01-01")
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);   
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully with dTimKickoff specified
        /// </summary>
        [TestMethod]
        public void Validate_wellbore_with_dTimKickoff()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = well.Name,
                Name = DevKit.Name("Wellbore 01-01"),
                DateTimeKickoff = DateTimeOffset.Now
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        /// <summary>
        /// Test adding an existing <see cref="Wellbore"/> 
        /// </summary>
        [TestMethod]
        public void Test_error_code_405_data_object_uid_duplicate()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = DevKit.Name("Wellbore-to-add-01"), NameWell = well.Name, UidWell = response.SuppMsgOut, Uid = DevKit.Uid() };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void Test_error_code_406_missing_parent_uid()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = DevKit.Name("Wellbore-to-add-01"), NameWell = well.Name };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentUid, response.Result);         
        }

        [TestMethod]
        public void Test_error_code_478_parent_uid_case_not_matching()
        {
            var uid = "arent-well-01-for-error-code-478" + DevKit.Uid();
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone, Uid = "P" + uid};
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = DevKit.Name("Wellbore-to-add-01"), NameWell = well.Name, UidWell = well.Uid };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            wellbore = new Wellbore { Name = DevKit.Name("Wellbore-to-add-02"), NameWell = well.Name, UidWell = "p" + uid };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Test_error_code_481_missing_parent_object()
        {
            var well = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var wellbore = new Wellbore { Name = DevKit.Name("Wellbore-to-add-01"), NameWell = well.Name, UidWell = DevKit.Uid() };
            var response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Test_can_add_wellbore_with_same_uid_under_different_well()
        {
            var well1 = new Well { Name = DevKit.Name("Well-to-add-01"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well1);

            var wellbore1 = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = well1.Name,
                Name = DevKit.Name("Wellbore 01-01")
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore1);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var well2 = new Well { Name = DevKit.Name("Well-to-add-02"), TimeZone = DevKit.TimeZone };
            response = DevKit.Add<WellList, Well>(well2);

            var wellbore2 = new Wellbore()
            {
                Uid = uid,
                UidWell = response.SuppMsgOut,
                NameWell = well2.Name,
                Name = DevKit.Name("Wellbore 02-01")
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore2);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Wellbore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            string queryIn = "<wellbores version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <wellbore/>" + Environment.NewLine +
                            "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, queryIn, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Wellbore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Wellbore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_All_Error_407_Missing_Witsml_Object_Type()
        {
            _query[0].Name = DevKit.Name("Wellbore-to-add-missing-witsml-type");
            var response = DevKit.Get<WellboreList, Wellbore>(_query, string.Empty, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.MissingWMLtypeIn, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_without_ReturnElements_Error_407_Missing_Witsml_Object_Type()
        {
            var wellbore = new Wellbore { Name = "Wellbore-to-query-missing-witsml-type" };
            var response = DevKit.Get<WellboreList, Wellbore>(DevKit.List(wellbore), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWMLtypeIn, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, null, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_Non_Conforming_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Non_Conforming_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_DataOnly_Not_Growing_Object()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_MultiChild()
        {
            string badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore/>" + Environment.NewLine +
                              "   <wellbore/>" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_Has_Attribute()
        {
            string badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore uid=\"Test Wellbores\" />" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_BadChild()
        {
            string badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <log/>" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_NonEmptyChild()
        {
            string badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore>" + Environment.NewLine +
                              "       <name>Test Wellbores</name>" + Environment.NewLine +
                              "   </wellbore>" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_Not_ChangeLog()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }
    }
}
