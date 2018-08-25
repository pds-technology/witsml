//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public partial class Wellbore141DataAdapterDeleteTests : Wellbore141TestBase
    {
        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Delete_FullWellbore()
        {
            // Add wellbore
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            // Assert wellbore is added
             DevKit.GetAndAssert(Wellbore);

            // Delete wellbore
            var delete = new Wellbore {Uid = Wellbore.Uid, UidWell = Wellbore.UidWell};
            DevKit.DeleteAndAssert(delete);

            // Assert the wellbore has been deleted
            var results = DevKit.Query<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Delete_FullWellbore_With_Case_Insensitive_Uid()
        {
            var uid = DevKit.Uid();
            Wellbore.Uid = "wb" + uid;

            // Add wellbore
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            // Assert wellbore is added
            DevKit.GetAndAssert(Wellbore);

            // Delete wellbore
            var delete = new Wellbore {Uid = "Wb" + uid, UidWell = Wellbore.UidWell};
            DevKit.DeleteAndAssert(delete);

            // Assert the wellbore has been deleted
            var results = DevKit.Query<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            Wellbore.PurposeWellbore = WellPurpose.appraisal;
            Wellbore.DateTimeKickoff = DateTimeOffset.UtcNow;

            // Add wellbore
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            // Assert all testing elements are added
            var result =  DevKit.GetAndAssert(Wellbore);
            Assert.AreEqual(Wellbore.PurposeWellbore, result.PurposeWellbore);
            Assert.AreEqual(Wellbore.DateTimeKickoff, result.DateTimeKickoff);

            // Partial delete wellbore
            const string delete = "<purposeWellbore /><dTimKickoff />";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the wellbore elements has been deleted
            result =  DevKit.GetAndAssert(Wellbore);
            Assert.IsNull(result.PurposeWellbore);
            Assert.IsNull(result.DateTimeKickoff);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Attributes()
        {
            var md = new MeasuredDepthCoord {Uom = MeasuredDepthUom.m, Value = 1.0, Datum = "datum1"};
            Wellbore.MD = md;

            // Add wellbore
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            // Assert all testing elements are added
            var result =  DevKit.GetAndAssert(Wellbore);
            var resultMd = result.MD;
            Assert.IsNotNull(resultMd);
            Assert.IsNotNull(resultMd.Datum);

            // Partial delete wellbore
            var delete = "<md datum=\"\" />";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the attributes has been deleted
            result =  DevKit.GetAndAssert(Wellbore);
            resultMd = result.MD;
            Assert.IsNotNull(resultMd);
            Assert.IsNull(resultMd.Datum);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Elements()
        {
            var testCommonData = new CommonData
            {
                Comments = "Testing partial delete nested elements",
                ItemState = ItemState.plan
            };

            Wellbore.CommonData = testCommonData;

            // Add wellbore
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            // Assert all testing elements are added
            var result =  DevKit.GetAndAssert(Wellbore);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete wellbore
            const string delete = "<commonData><comments /><itemState /></commonData>";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the wellbore elements has been deleted
            result =  DevKit.GetAndAssert(Wellbore);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.IsNull(commonData.Comments);
            Assert.IsNull(commonData.ItemState);
        }

        [TestMethod]
        [Description("Tests the removal of the 1st extensionNameValue element and unset the description element of the 2nd extensionNameValue element in commonData")]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Elements()
        {
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var ext2 = DevKit.ExtensionNameValue("Ext-2", "2.0", "ft");
            ext2.Description = "Testing partial delete of nested recurring elements";
            var testCommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue> {ext1, ext2}
            };
            Wellbore.CommonData = testCommonData;

            // Add wellbore
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            // Assert all testing elements are added
            var result =  DevKit.GetAndAssert(Wellbore);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(2, commonData.ExtensionNameValue.Count);

            // Partial delete wellbore
            var delete = "<commonData>" + Environment.NewLine +
                    "<extensionNameValue uid=\"Ext-1\" />" + Environment.NewLine +
                    "<extensionNameValue uid=\"Ext-2\">" + Environment.NewLine +
                        "<description />" + Environment.NewLine +
                    "</extensionNameValue>" + Environment.NewLine +
                "</commonData>";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the partial delete of the recurring elements
           
            result =  DevKit.GetAndAssert(Wellbore);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            var exts = commonData.ExtensionNameValue;
            Assert.AreEqual(1, exts.Count);
            var resultExt1 = exts.FirstOrDefault(e => e.Uid == ext1.Uid);
            Assert.IsNull(resultExt1);
            var resultExt2 = exts.FirstOrDefault(e => e.Uid == ext2.Uid);
            Assert.IsNotNull(resultExt2);
            Assert.IsNull(resultExt2.Description);
        }


        [TestMethod, Description("Tests you cannot do DeleteFromStore without plural container")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var nonPluralWell = "<wellbore xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <wellbore uidWell=\"{0}\" uid=\"{0}\">" + Environment.NewLine +
                           "       <name>plural wellbore</name>" + Environment.NewLine +
                           "   </wellbore>" + Environment.NewLine +
                           "</wellbore>";

            var xmlIn = string.Format(nonPluralWell, Well.Uid, Wellbore.Uid);
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = DevKit.Delete<WellboreList, Wellbore>(Wellbore, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty queryIn")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_408_Empty_QueryIn()
        {
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, string.Empty, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with non conforming QueryIn")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_409_QueryIn_Must_Conform_To_Schema()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            Wellbore.NumGovt = "101";
            DevKit.AddAndAssert(Wellbore);

            // Delete well with invalid element
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<numGovt /><numGovt />");
            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the wellbore uid")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Delete well with invalid element
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, string.Empty, string.Empty);
            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty UID")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_416_Empty_UID()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore

            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            Wellbore.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            DevKit.AddAndAssert(Wellbore);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate,Well.Uid, Wellbore.Uid,
                "<commonData><extensionNameValue uid=\"\" /></commonData>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUidSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty uom")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            Wellbore.MD = new MeasuredDepthCoord()
            {
                Uom = MeasuredDepthUom.ft,
                Value = 1.0
            };
            DevKit.AddAndAssert(Wellbore);

            // Delete wellbore's MD
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<md uom=\"\" />");

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUomSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with the UID missing")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_418_Missing_Uid()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            Wellbore.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };
            DevKit.AddAndAssert(Wellbore);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<commonData><extensionNameValue /></commonData>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForDelete, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore non recurring element with no uid")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_419_Deleting_Empty_NonRecurring_Element_With_No_Uid()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Delete wellbore's MD
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<commonData />");

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyNonRecurringElementSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_420_Delete_Required_Element()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Delete nameWell
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<nameWell />");

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyMandatoryNodeSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore if wellbore has any child data objects")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_432Wellbore_Has_Child_Data_Objects()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Add rig
            var rig = new Rig()
            {
                UidWell = Well.Uid,
                UidWellbore = Wellbore.Uid,
                Uid = DevKit.Uid(),
                NameWell = Well.Name,
                NameWellbore = Wellbore.Name,
                Name = "Big Rig"
            };
            DevKit.AddAndAssert(rig);

            // Delete wellbore
            var delete = new Wellbore { Uid = Wellbore.Uid, UidWell = Well.Uid };

            var results = DevKit.Delete<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.NotBottomLevelDataObject, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a wellbore that does not exist")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_433Wellbore_Does_Not_Exist()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Delete wellbore
            var delete = new Wellbore { Uid = Wellbore.Uid, UidWell = Well.Uid };

            var results = DevKit.Delete<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_444_Updating_With_More_Than_One_Data_Object()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add second well
            var well2 = new Well()
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Well 02"),
                TimeZone = DevKit.TimeZone
            };

            DevKit.AddAndAssert(well2);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Add second wellbore
            var wellbore2 = new Wellbore()
            {
                UidWell = well2.Uid,
                NameWell = well2.Name,
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore 02"),
                MD = new MeasuredDepthCoord(0, MeasuredDepthUom.ft)
            };

            DevKit.AddAndAssert(wellbore2);

            var multiObjectXml = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <wellbore uidWell=\"{0}\" uid=\"{1}\">" + Environment.NewLine +
                          "{2}" +
                          "   </wellbore>" + Environment.NewLine +
                          "   <wellbore uidWell=\"{3}\" uid=\"{4}\">" + Environment.NewLine +
                          "{5}" +
                          "   </wellbore>" + Environment.NewLine +
                          "</wellbores>";

            // Delete wellbores
            var deleteXml = string.Format(
                multiObjectXml,
                Well.Uid,
                Wellbore.Uid,
                "<md uom=\"ft\">1</md>",
                well2.Uid,
                wellbore2.Uid,
                "<md uom=\"ft\">2</md>"
                );

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a non-container element with non-empty attribute value")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_1021_Delete_Simple_Content_With_Non_Empty_Attribute()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            Wellbore.MD = new MeasuredDepthCoord {Uom = MeasuredDepthUom.m, Datum = "abc", Value = 12.0};
            DevKit.AddAndAssert(Wellbore);

            // Delete nameWell
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<md uom=\"m\" datum=\"abc\" />");

            var results = DevKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.ErrorDeletingSimpleContent, results.Result);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Partial_Delete_Updates_ChangeLog()
        {
            AddParents();

            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var ext2 = DevKit.ExtensionNameValue("Ext-2", "2.0", "ft");
            var commonData = new CommonData { Comments = "comments", ExtensionNameValue = new List<ExtensionNameValue> { ext1, ext2 } };

            Wellbore.SuffixAPI = "suffixApi";
            Wellbore.MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Datum = "abc", Value = 12.0 };
            Wellbore.CommonData = commonData;

            DevKit.AddAndAssert(Wellbore);

            var delete = @" <suffixApi/>
                            <MD datum = """"/>
                            <commonData>
                                <comments/>
                                <extensionNameValue uid=""" + ext1.Uid + @"""/>
                            </commonData>";

            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = DevKit.GetAndAssert(Wellbore);
            Assert.IsNull(wellbore.SuffixAPI);
            Assert.IsNull(wellbore.MD.Datum);
            Assert.IsNull(wellbore.CommonData.Comments);
            Assert.AreEqual(1, wellbore.CommonData.ExtensionNameValue.Count);
            Assert.IsNull(wellbore.CommonData.ExtensionNameValue.Find(e => e.Uid == ext1.Uid));

            var expectedHistoryCount = 2;
            var expectedChangeType = ChangeInfoType.update;
            DevKit.AssertChangeLog(wellbore, expectedHistoryCount, expectedChangeType);
        }
    }
}
