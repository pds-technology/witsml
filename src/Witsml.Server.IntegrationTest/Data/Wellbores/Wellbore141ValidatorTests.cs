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
        private DevKit141Aspect _devKit;
        private List<Wellbore> _query;
        private Well _well;
        private Wellbore _wellbore;

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
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _query = _devKit.List(new Wellbore());

            _well = new Well
            {
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Well 01"),
                TimeZone = _devKit.TimeZone
            };

            _wellbore = new Wellbore()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _devKit = null;
        }       

        /// <summary>
        /// Test adding an existing <see cref="Wellbore"/> 
        /// </summary>
        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_405_Data_Object_Uid_Duplicate()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidAlreadyExists, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_406_Missing_Parent_Uid()
        {
            _wellbore.UidWell = null;
            var response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForAdd, response.Result);         
        }

        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_478_Parent_Uid_Case_Not_Matching()
        {
            var uid = "arent-well-478" + _devKit.Uid();
            _well.Uid = "P" + uid;
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellbore.UidWell = _well.Uid;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = _devKit.Name("Wellbore-to-add-02"), NameWell = _well.Name, UidWell = "p" + uid };
            response = _devKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_481_Missing_Parent_Object()
        {
            var response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Wellbore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            string queryIn = "<wellbores version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <wellbore/>" + Environment.NewLine +
                            "</wellbores>";

            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, queryIn, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Wellbore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Wellbore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, _badQueryNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_All_Error_407_Missing_Witsml_Object_Type()
        {
            _query[0].Name = _devKit.Name("Wellbore-to-add-missing-witsml-type");
            var response = _devKit.Get<WellboreList, Wellbore>(_query, string.Empty, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_without_ReturnElements_Error_407_Missing_Witsml_Object_Type()
        {
            var wellbore = new Wellbore { Name = "Wellbore-to-query-missing-witsml-type" };
            var response = _devKit.Get<WellboreList, Wellbore>(_devKit.List(wellbore), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, null, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_Non_Conforming_Query_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Non_Conforming_Query_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_UpdateInStore_Error_483_None_Non_Conforming_Query_Template()
        {
            var response = _devKit.UpdateInStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, null);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var response = _devKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_DataOnly_Not_Growing_Object()
        {
            var response = _devKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var response = _devKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var response = _devKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template()
        {
            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, _badQueryEmptyWellboreList, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_MultiChild()
        {
            var badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore/>" + Environment.NewLine +
                              "   <wellbore/>" + Environment.NewLine +
                              "</wellbores>";

            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_Has_Attribute()
        {
            var badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore uid=\"Test Wellbores\" />" + Environment.NewLine +
                              "</wellbores>";

            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
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

            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_Not_ChangeLog()
        {
            var response = _devKit.Get<WellboreList, Wellbore>(_query, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_Bad_Child_Element()
        {
            var badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well />" + Environment.NewLine +
                              "</wellbores>";

            var response = _devKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }
    }
}
