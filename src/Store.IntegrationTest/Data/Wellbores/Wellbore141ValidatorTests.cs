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
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// AddWellboreValidator test class
    /// </summary>
    public partial class Wellbore141ValidatorTests
    {

        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_406_Missing_Parent_Uid()
        {
            Wellbore.UidWell = null;
            var response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForAdd, response.Result);         
        }

        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_478_Parent_Uid_Case_Not_Matching()
        {
            var uid = "arent-well-478" + DevKit.Uid();
            Well.Uid = "P" + uid;
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            Wellbore.UidWell = Well.Uid;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = DevKit.Name("Wellbore-to-add-02"), NameWell = Well.Name, UidWell = "p" + uid };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.IncorrectCaseParentUid, response.Result);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> to an non-existing well.
        /// </summary>
        [TestMethod]
        public void Wellbore141Validator_AddToStore_Error_481_Missing_Parent_Object()
        {
            var response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.MissingParentDataObject, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_All_Error_407_Missing_Witsml_Object_Type()
        {
            QueryEmptyList[0].Name = DevKit.Name("Wellbore-to-add-missing-witsml-type");
            var response = DevKit.Get<WellboreList, Wellbore>(QueryEmptyList, string.Empty, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_without_ReturnElements_Error_407_Missing_Witsml_Object_Type()
        {
            var wellbore = new Wellbore { Name = "Wellbore-to-query-missing-witsml-type" };
            var response = DevKit.Get<WellboreList, Wellbore>(DevKit.List(wellbore), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, null, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_Non_ConformingQueryEmptyList_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, QueryEmptyRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Non_ConformingQueryEmptyList_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, QueryEmptyRoot, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_UpdateInStore_Error_483_None_Non_ConformingQueryEmptyList_Template()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.Wellbore, QueryEmptyRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(QueryEmptyList, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_DataOnly_Not_Growing_Object()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(QueryEmptyList, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(QueryEmptyList, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var response = DevKit.Get<WellboreList, Wellbore>(QueryEmptyList, ObjectTypes.Wellbore, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_MinimumQueryEmptyList_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, QueryEmptyRoot, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_MinimumQueryEmptyList_Template_MultiChild()
        {
            var badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore/>" + Environment.NewLine +
                              "   <wellbore/>" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_MinimumQueryEmptyList_Template_Has_Attribute()
        {
            var badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <wellbore uid=\"Test Wellbores\" />" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_MinimumQueryEmptyList_Template_NonEmptyChild()
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
            var response = DevKit.Get<WellboreList, Wellbore>(QueryEmptyList, ObjectTypes.Wellbore, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);
            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }

        [TestMethod]
        public void Wellbore141Validator_GetFromStore_Error_409_Bad_Child_Element()
        {
            var badQuery = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well />" + Environment.NewLine +
                              "</wellbores>";

            var response = DevKit.GetFromStore(ObjectTypes.Wellbore, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }
    }
}
