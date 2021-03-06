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

// ----------------------------------------------------------------------
// <auto-generated>
//     Changes to this file may cause incorrect behavior and will be lost
//     if the code is regenerated.
// </auto-generated>
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Tubulars
{
    [TestClass]
    public partial class Tubular131ValidatorTests : Tubular131TestBase
    {
        #region Error -401

        public static readonly string QueryInvalidPluralRoot =
            "<tubular xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\">" + Environment.NewLine +
            "  <tubular>" + Environment.NewLine +
            "    <name>Test Plural Root Element</name>" + Environment.NewLine +
            "  </tubular>" + Environment.NewLine +
            "</tubular>";

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Tubular, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.AddToStore(ObjectTypes.Tubular, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        [TestMethod]
        public void Tubular131Validator_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.DeleteFromStore(ObjectTypes.Tubular, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        #endregion Error -401

        #region Error -402

        #endregion Error -402

        #region Error -403

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Tubular, QueryMissingNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Tubular, QueryInvalidNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Tubular, QueryInvalidNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        #endregion Error -403

        #region Error -405

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_405_Tubular_Already_Exists()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular, ErrorCodes.DataObjectUidAlreadyExists);
        }

        #endregion Error -405

        #region Error -406

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_406_Tubular_Missing_Parent_Uid()
        {
            AddParents();
            Tubular.UidWellbore = null;
            DevKit.AddAndAssert(Tubular, ErrorCodes.MissingElementUidForAdd);
        }

        #endregion Error -406

        #region Error -407

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_407_Tubular_Missing_Witsml_Object_Type()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            var response = DevKit.Update<TubularList, Tubular>(Tubular, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        #endregion Error -407

        #region Error -408

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_408_Tubular_Empty_QueryIn()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, string.Empty, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        #endregion Error -408

        #region Error -409

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_409_Tubular_XmlIn_Must_Conform_To_Schema()
        {
            AddParents();

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var response = DevKit.AddToStore(ObjectTypes.Tubular, nonConformingXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_409_Tubular_XmlIn_Must_Conform_To_Schema()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, nonConformingXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_409_Tubular_QueryIn_Must_Conform_To_Schema()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var response = DevKit.GetFromStore(ObjectTypes.Tubular, nonConformingXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_DeleteFromStore_Error_409_Tubular_QueryIn_Must_Conform_To_Schema()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var response = DevKit.DeleteFromStore(ObjectTypes.Tubular, nonConformingXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }

        #endregion Error -409

        #region Error -415

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_415_Tubular_Update_Without_Specifing_UID()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            Tubular.Uid = string.Empty;
            DevKit.UpdateAndAssert<TubularList, Tubular>(Tubular, ErrorCodes.DataObjectUidMissing);
        }

        #endregion Error -415
        #region Error -420

        [TestMethod]
        public void Tubular131Validator_DeleteFromStore_Error_420_Tubular_Specifying_A_Non_Recuring_Element_That_Is_Required()
        {
            AddParents();

            DevKit.AddAndAssert(Tubular);

            var deleteXml = string.Format(BasicXMLTemplate,Tubular.UidWell, Tubular.UidWellbore,Tubular.Uid,
                "<nameWell />");
            var results = DevKit.DeleteFromStore(ObjectTypes.Tubular, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyMandatoryNodeSpecified, results.Result);
        }

        #endregion Error -420

        #region Error -426

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_426_Tubular_Compressed_XmlIn_Must_Conform_To_Schema()
        {
            AddParents();

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var optionsIn = string.Empty;
            ClientCompression.Compress(ref nonConformingXml, ref optionsIn);

            var response = DevKit.AddToStore(ObjectTypes.Tubular, nonConformingXml, null, optionsIn);
            Assert.AreEqual((short)ErrorCodes.CompressedInputNonConforming, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_426_Tubular_Compressed_XmlIn_Must_Conform_To_Schema()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var optionsIn = string.Empty;
            ClientCompression.Compress(ref nonConformingXml, ref optionsIn);

            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, nonConformingXml, null, optionsIn);
            Assert.AreEqual((short)ErrorCodes.CompressedInputNonConforming, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_426_Tubular_Compressed_QueryIn_Must_Conform_To_Schema()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<name>{Tubular.Name}</name><name>{Tubular.Name}</name>");

            var optionsIn = string.Empty;
            ClientCompression.Compress(ref nonConformingXml, ref optionsIn);

            var response = DevKit.GetFromStore(ObjectTypes.Tubular, nonConformingXml, null, optionsIn);
            Assert.AreEqual((short)ErrorCodes.CompressedInputNonConforming, response.Result);
        }

        #endregion Error -426

        #region Error -433

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_433_Tubular_Does_Not_Exist()
        {
            AddParents();
            DevKit.UpdateAndAssert<TubularList, Tubular>(Tubular, ErrorCodes.DataObjectNotExist);
        }

        #endregion Error -433
        #region Error -444

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_444_Tubular_Updating_More_Than_One_Data_Object()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            var updateXml = "<tubulars xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"><tubular uidWell=\"{0}\" uidWellbore=\"{1}\" uid=\"{2}\"></tubular><tubular uidWell=\"{0}\" uidWellbore=\"{1}\" uid=\"{2}\"></tubular></tubulars>";
            updateXml = string.Format(updateXml, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid);

            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, response.Result);
        }

        #endregion Error -444
        #region Error -468

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_468_Tubular_No_Schema_Version_Declared()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, QueryMissingVersion, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, response.Result);
        }

        #endregion Error -468

        #region Error -478

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_478_Tubular_Parent_Uid_Case_Not_Matching()
        {
            Well.Uid = Well.Uid.ToUpper();
            Wellbore.Uid = Wellbore.Uid.ToUpper();
            Wellbore.UidWell = Well.Uid.ToUpper();
            AddParents();

            Tubular.UidWell = Well.Uid.ToLower();

            DevKit.AddAndAssert(Tubular, ErrorCodes.IncorrectCaseParentUid);
        }

        #endregion Error -478

        #region Error -479

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_479_Tubular_Cannot_Decompress_XmlIn()
        {
            AddParents();

            var uncompressedXml = "abcd1234";
            var compressedXml = uncompressedXml;

            var optionsIn = string.Empty;
            ClientCompression.Compress(ref compressedXml, ref optionsIn);

            var response = DevKit.AddToStore(ObjectTypes.Tubular, uncompressedXml, null, optionsIn);
            Assert.AreEqual((short)ErrorCodes.CannotDecompressQuery, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_479_Tubular_Cannot_Decompress_XmlIn()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var uncompressedXml = "abcd1234";
            var compressedXml = uncompressedXml;

            var optionsIn = string.Empty;
            ClientCompression.Compress(ref compressedXml, ref optionsIn);

            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, uncompressedXml, null, optionsIn);
            Assert.AreEqual((short)ErrorCodes.CannotDecompressQuery, response.Result);
        }

        [TestMethod]
        public void Tubular131Validator_GetFromStore_Error_479_Tubular_Cannot_Decompress_XmlIn()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);

            var uncompressedXml = "abcd1234";
            var compressedXml = uncompressedXml;

            var optionsIn = string.Empty;
            ClientCompression.Compress(ref compressedXml, ref optionsIn);

            var response = DevKit.GetFromStore(ObjectTypes.Tubular, uncompressedXml, null, optionsIn);
            Assert.AreEqual((short)ErrorCodes.CannotDecompressQuery, response.Result);
        }

        #endregion Error -479

        #region Error -481

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_481_Tubular_Parent_Does_Not_Exist()
        {
            DevKit.AddAndAssert(Tubular, ErrorCodes.MissingParentDataObject);
        }

        #endregion Error -481

        #region Error -483

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_483_Tubular_Update_With_Non_Conforming_Template()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, QueryEmptyRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        #endregion Error -483

        #region Error -484

        [TestMethod]
        public void Tubular131Validator_UpdateInStore_Error_484_Tubular_Update_Will_Delete_Required_Element()
        {
            AddParents();
            DevKit.AddAndAssert<TubularList, Tubular>(Tubular);
            var nonConformingXml = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                $"<nameWell></nameWell>");

            var response = DevKit.UpdateInStore(ObjectTypes.Tubular, nonConformingXml, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, response.Result);
        }

        #endregion Error -484

        #region Error -486

        [TestMethod]
        public void Tubular131Validator_AddToStore_Error_486_Tubular_Data_Object_Types_Dont_Match()
        {
            AddParents();

            var xmlIn = string.Format(BasicXMLTemplate, Tubular.UidWell, Tubular.UidWellbore, Tubular.Uid,
                string.Empty);

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        #endregion Error -486
    }
}