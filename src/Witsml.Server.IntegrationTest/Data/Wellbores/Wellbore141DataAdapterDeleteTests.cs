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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public class Wellbore141DataAdapterDeleteTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well
            {
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Well 01"),
                TimeZone = _devKit.TimeZone
            };

            _wellbore = new Wellbore
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

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Delete_Full_Wellbore()
        {
            // Add wellbore
            AddWellbore(_well, _wellbore);

            // Assert wellbore is added
             _devKit.GetSingleWellboreAndAssert(_wellbore);

            // Delete wellbore
            var delete = new Wellbore {Uid = _wellbore.Uid, UidWell = _wellbore.UidWell};
            _devKit.DeleteAndAssert(delete);

            // Assert the wellbore has been deleted
            var results = _devKit.Query<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Delete_Full_Wellbore_With_Case_Insensitive_Uid()
        {
            var uid = _devKit.Uid();
            _wellbore.Uid = "wb" + uid;

            // Add wellbore
            AddWellbore(_well, _wellbore);

            // Assert wellbore is added
             _devKit.GetSingleWellboreAndAssert(_wellbore);

            // Delete wellbore
            var delete = new Wellbore {Uid = "Wb" + uid, UidWell = _wellbore.UidWell};
            _devKit.DeleteAndAssert(delete);

            // Assert the wellbore has been deleted
            var results = _devKit.Query<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            _wellbore.PurposeWellbore = WellPurpose.appraisal;
            _wellbore.DateTimeKickoff = DateTimeOffset.UtcNow;

            // Add wellbore
            AddWellbore(_well, _wellbore);

            // Assert all testing elements are added
            var result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
            Assert.AreEqual(_wellbore.PurposeWellbore, result.PurposeWellbore);
            Assert.AreEqual(_wellbore.DateTimeKickoff, result.DateTimeKickoff);

            // Partial delete wellbore
            const string delete = "<purposeWellbore /><dTimKickoff />";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _wellbore.UidWell, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the wellbore elements has been deleted
            result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
            Assert.IsNull(result.PurposeWellbore);
            Assert.IsNull(result.DateTimeKickoff);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Attributes()
        {
            var md = new MeasuredDepthCoord {Uom = MeasuredDepthUom.m, Value = 1.0, Datum = "datum1"};
            _wellbore.MD = md;

            // Add wellbore
            AddWellbore(_well, _wellbore);

            // Assert all testing elements are added
            var result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
            var resultMd = result.MD;
            Assert.IsNotNull(resultMd);
            Assert.IsNotNull(resultMd.Datum);

            // Partial delete wellbore
            var delete = "<md datum=\"\" />";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _wellbore.UidWell, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the attributes has been deleted
            result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
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

            _wellbore.CommonData = testCommonData;

            // Add wellbore
            AddWellbore(_well, _wellbore);

            // Assert all testing elements are added
            var result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete wellbore
            const string delete = "<commonData><comments /><itemState /></commonData>";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _wellbore.UidWell, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the wellbore elements has been deleted
            result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.IsNull(commonData.Comments);
            Assert.IsNull(commonData.ItemState);
        }

        [TestMethod]
        [Description("Tests the removal of the 1st extensionNameValue element and unset the description element of the 2nd extensionNameValue element in commonData")]
        public void Wellbore141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Elements()
        {
            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var ext2 = _devKit.ExtensionNameValue("Ext-2", "2.0", "ft");
            ext2.Description = "Testing partial delete of nested recurring elements";
            var testCommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue> {ext1, ext2}
            };
            _wellbore.CommonData = testCommonData;

            // Add wellbore
            AddWellbore(_well, _wellbore);

            // Assert all testing elements are added
            var result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
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
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _wellbore.UidWell, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Wellbore, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the partial delete of the recurring elements
           
            result =  _devKit.GetSingleWellboreAndAssert(_wellbore);
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

            var xmlIn = string.Format(nonPluralWell, _well.Uid, _wellbore.Uid);
            var response = _devKit.DeleteFromStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = _devKit.Delete<WellboreList, Wellbore>(_wellbore, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with empty queryIn")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_408_Empty_QueryIn()
        {
            var response = _devKit.DeleteFromStore(ObjectTypes.Wellbore, string.Empty, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with non conforming QueryIn")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_409_QueryIn_Must_Conform_To_Schema()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            _wellbore.NumGovt = "101";
            _devKit.AddAndAssert(_wellbore);

            // Delete well with invalid element
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, string.Empty, _well.Uid,
                "<numGovt /><numGovt />");
            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the wellbore uid")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            _devKit.AddAndAssert(_wellbore);

            // Delete well with invalid element
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, string.Empty, _well.Uid, string.Empty);
            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty UID")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_416_Empty_UID()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore

            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            _wellbore.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            _devKit.AddAndAssert(_wellbore);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _well.Uid,
                "<commonData><extensionNameValue uid=\"\" /></commonData>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUidSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty uom")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            _wellbore.MD = new MeasuredDepthCoord()
            {
                Uom = MeasuredDepthUom.ft,
                Value = 1.0
            };
            _devKit.AddAndAssert(_wellbore);

            // Delete wellbore's MD
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _well.Uid,
                "<md uom=\"\" />");

            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUomSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with the UID missing")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_418_Missing_Uid()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            _wellbore.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };
            _devKit.AddAndAssert(_wellbore);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _well.Uid,
                "<commonData><extensionNameValue /></commonData>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForDelete, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore non recurring element with no uid")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_419_Deleting_Empty_NonRecurring_Element_With_No_Uid()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            _devKit.AddAndAssert(_wellbore);

            // Delete wellbore's MD
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _well.Uid,
                "<commonData />");

            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyNonRecurringElementSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_420_Delete_Required_Element()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            _devKit.AddAndAssert(_wellbore);

            // Delete nameWell
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellboreXmlTemplate, _wellbore.Uid, _well.Uid,
                "<nameWell />");

            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyMandatoryNodeSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore if wellbore has any child data objects")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_432_Wellbore_Has_Child_Data_Objects()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            _devKit.AddAndAssert(_wellbore);

            // Add rig
            var rig = new Rig()
            {
                UidWell = _well.Uid,
                UidWellbore = _wellbore.Uid,
                Uid = _devKit.Uid(),
                NameWell = _well.Name,
                NameWellbore = _wellbore.Name,
                Name = "Big Rig"
            };
            _devKit.AddAndAssert(rig);

            // Delete wellbore
            var delete = new Wellbore { Uid = _wellbore.Uid, UidWell = _well.Uid };

            var results = _devKit.Delete<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.NotBottomLevelDataObject, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore on a wellbore that does not exist")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_433_Wellbore_Does_Not_Exist()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Delete wellbore
            var delete = new Wellbore { Uid = _wellbore.Uid, UidWell = _well.Uid };

            var results = _devKit.Delete<WellboreList, Wellbore>(delete, ObjectTypes.Wellbore);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Wellbore141DataAdapter_DeleteFromStore_Error_444_Updating_With_More_Than_One_Data_Object()
        {
            // Add well 
            _devKit.AddAndAssert(_well);

            // Add second well
            var well2 = new Well()
            {
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Well 02"),
                TimeZone = _devKit.TimeZone
            };

            _devKit.AddAndAssert(well2);

            // Add wellbore
            _devKit.AddAndAssert(_wellbore);

            // Add second wellbore
            var wellbore2 = new Wellbore()
            {
                UidWell = well2.Uid,
                NameWell = well2.Name,
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Wellbore 02"),
                MD = new MeasuredDepthCoord(0, MeasuredDepthUom.ft)
            };

            _devKit.AddAndAssert(wellbore2);

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
                _well.Uid,
                _wellbore.Uid,
                "<md uom=\"ft\">1</md>",
                well2.Uid,
                wellbore2.Uid,
                "<md uom=\"ft\">2</md>"
                );

            var results = _devKit.DeleteFromStore(ObjectTypes.Wellbore, deleteXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }


        #region Helper Methods

        private void AddWellbore(Well well, Wellbore wellbore)
        {
            _devKit.AddAndAssert(well);
            _devKit.AddAndAssert(wellbore);
        }
      
        #endregion
    }
}
