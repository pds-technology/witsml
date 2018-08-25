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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public partial class Wellbore141DataAdapterUpdateTests : Wellbore141TestBase
    {
        [TestMethod, Description("Tests updating an existing element with UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_Update_A_List_Element()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Query wellbore
            var returnWellbore = DevKit.GetAndAssert(Wellbore);

            var mdElement = returnWellbore.MD;
            Assert.IsNotNull(mdElement);
            Assert.AreEqual(MeasuredDepthUom.ft, mdElement.Uom);
            Assert.AreEqual(0, mdElement.Value);

            // Update wellbore
            var update = new Wellbore {
                UidWell = Well.Uid,
                Uid = Wellbore.Uid,
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.ft, Value = 1 }
                };

            DevKit.UpdateAndAssert(update);

            // Query updated wellbore
            returnWellbore = DevKit.GetAndAssert(Wellbore);

            mdElement = returnWellbore.MD;
            Assert.IsNotNull(mdElement);
            Assert.AreEqual(MeasuredDepthUom.ft, mdElement.Uom);
            Assert.AreEqual(1, mdElement.Value);
        }

        [TestMethod, Description("Tests updating an element with an invalid element with UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_UpdateWell_And_Ignore_Invalid_Element()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            Wellbore.SuffixAPI = "0";

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<suffixAPI>1</suffixAPI>" +
                "<numGovtxxxxxxxx>101</numGovtxxxxxxxx>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated wellbore 
            var returnWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.AreEqual("1", returnWellbore.SuffixAPI);
        }

        [TestMethod, Description("Tests updating an existing element with another element that has an invalid attribute on UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_UpdateWell_And_Ignore_Invalid_Attribute()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            Wellbore.SuffixAPI = "0";

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<suffixAPI>1</suffixAPI>" +
                "<numGovt abc=\"abc\">101</numGovt>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated wellbore 
            var returnWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.AreEqual("1", returnWellbore.SuffixAPI);
            Assert.AreEqual("101", returnWellbore.NumGovt);
        }

        [TestMethod, Description("Tests that invalid child elements are not added during UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_Update_With_Invalid_Child_Element()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            Wellbore.SuffixAPI = "0";

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<suffixAPI><abc>1</abc></suffixAPI>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated wellbore 
            var returnWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.AreEqual(Wellbore.Name, returnWellbore.Name);
            Assert.IsNull(returnWellbore.SuffixAPI);
        }

        [TestMethod, Description("Tests adding a recurring element for the first time on an UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_Add_Recurring_Element_Success()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            var extensionName1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var extensionName2 = DevKit.ExtensionNameValue("Ext-2", "2.0", "m");
            
            // Create an update well that adds a recurring element for the first time on update
            var update = new Wellbore()
            {
                UidWell = Well.Uid,
                Uid = Wellbore.Uid,
                CommonData = new CommonData
                {
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                       extensionName1, extensionName2
                    }
                }
            };

            var results = DevKit.Update<WellboreList, Wellbore>(update);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);
        }

        [TestMethod, Description("Tests adding an element with attributes for the first time on an UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_Add_Element_With_Attribute_Success()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore
            var update = new Wellbore
            {
                UidWell = Well.Uid,
                Uid = Wellbore.Uid,
                Tvd = new WellVerticalDepthCoord { Uom = WellVerticalCoordinateUom.ft, Value = 1 }
            };

            var updateResponse = DevKit.Update<WellboreList, Wellbore>(update);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an extensionNameValue field to commonData on an UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_Add_Extension_Name_Value_Success()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            var extensionName1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");

            // Create an update well that adds a recurring element for the first time on update
            var update = new Wellbore()
            {
                UidWell = Well.Uid,
                Uid = Wellbore.Uid,
                CommonData = new CommonData
                {
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                       extensionName1
                    }
                }
            };

            var results = DevKit.Update<WellboreList, Wellbore>(update);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Get updated wellbore
            var returnedWellbore = DevKit.GetAndAssert(Wellbore);
            var commonData = returnedWellbore.CommonData;
            Assert.IsNotNull(commonData);
            var extensionNameValues = commonData.ExtensionNameValue;
            Assert.IsNotNull(extensionNameValues);
            Assert.AreEqual(1, extensionNameValues.Count);
            var extensionName = extensionNameValues.FirstOrDefault();
            Assert.IsNotNull(extensionName);
            Assert.AreEqual(extensionName1.Uid, extensionName.Uid);
            Assert.AreEqual(extensionName1.Name.Name, extensionName.Name.Name);
            Assert.AreEqual(extensionName1.Value.Uom, extensionName.Value.Uom);
            Assert.AreEqual(extensionName1.Value.Value, extensionName.Value.Value);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore without plural container")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_401_No_Plural_Root_Element()
        {
            var nonPluralWellbore = "<wellbore xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <wellbore uidWell=\"{0}\" uid=\"{1}\">" + Environment.NewLine +
                           "       <nameWell>No Plural Well</nameWell>" + Environment.NewLine +
                           "       <name>No Plural Wellbore</name>" + Environment.NewLine +
                           "   </wellbore>" + Environment.NewLine +
                           "</wellbore>";

            var xmlIn = string.Format(nonPluralWellbore, Well.Uid, Wellbore.Uid);
            var response = DevKit.UpdateInStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore while missing the object type")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = DevKit.Update<WellboreList, Wellbore>(Wellbore, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore without all required fields on an optional element")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_409_Missing_Required_Fields_For_Optional_Property()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<commonData><extensionNameValue uid=\"Ext - 1\"><name>Ext-1</name><dataType>double</dataType></extensionNameValue></commonData>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, results.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore without specifying the wellbore uid")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_415_Update_Without_Specifing_UID()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, string.Empty, Well.Uid, string.Empty);
            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, results.Result);
        }

        [TestMethod, Description("Tests trying to update a wellbore that doesn't exist")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_433_Updating_AnWellbore_That_Doesnt_Exist()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Update wellbore
            var updateResponse = DevKit.Update<WellboreList, Wellbore>(Wellbore);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, updateResponse.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore with an invalid UOM")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_443_Updating_An_Element_With_Invalid_UOM()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Query wellbore
            var returnWellbore = DevKit.GetAndAssert(Wellbore);

            var mdElement = returnWellbore.MD;
            Assert.IsNotNull(mdElement);
            Assert.AreEqual(MeasuredDepthUom.ft, mdElement.Uom);
            Assert.AreEqual(0, mdElement.Value);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Wellbore.Uid, Well.Uid,
                "<md uom=\"xxx\">1</md>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, results.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore more than one object")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_444_Updating_With_More_Than_One_Data_Object()
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

            var multiObjectXml =  "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <wellbore uidWell=\"{0}\" uid=\"{1}\">" + Environment.NewLine +
                          "{2}" +
                          "   </wellbore>" + Environment.NewLine +
                          "   <wellbore uidWell=\"{3}\" uid=\"{4}\">" + Environment.NewLine +
                          "{5}" +
                          "   </wellbore>" + Environment.NewLine +
                          "</wellbores>";

            // Update wellbore
            var updateXml = string.Format(
                multiObjectXml, 
                Well.Uid, 
                Wellbore.Uid,
                "<md uom=\"ft\">1</md>",
                well2.Uid,
                wellbore2.Uid,
                "<md uom=\"ft\">2</md>"
                );

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore a empty complex element")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_445_Updating_With_An_Empty_Element()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<commonData><extensionNameValue uid=\"test of empty element\"></extensionNameValue></commonData>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, results.Result);
        }


        [TestMethod, Description("Tests you cannot do UpdateInStore on an element with an attirubte wihtout specifying the value")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_446_Updating_An_Element_Without_A_Value()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<tvd uom=\"ft\"></tvd>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, results.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore on an element missing its attribute")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_448_Updating_An_Element_Missing_An_Attribute()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                          "<commonData><extensionNameValue><name>Ext-1</name><value uom=\"m\">1.0</value><dataType>double</dataType></extensionNameValue></commonData>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForUpdate, results.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore an element with a value with a blank attribute")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_453_Updating_An_Element_Without_A_UOM()
        {
            // Add well 
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            // Update wellbore with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid, Wellbore.Uid,
                "<tvd uom=\"\">1</tvd>");

            var results = DevKit.UpdateInStore(ObjectTypes.Wellbore, updateXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, results.Result);
        }

        [TestMethod, Description("Tests that you cannot add an element with duplicate UIDs in UpdateInStore")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_464_Updating_An_Element_With_Same_UID_As_Existing_Element()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            DevKit.AddAndAssert(Wellbore);

            var extensionName1 = DevKit.ExtensionNameValue("SameUID", "1.0", "m");
            var extensionName2 = DevKit.ExtensionNameValue("SameUID", "2.0", "m");

            // Create an update well that adds a recurring element for the first time on update
            var update = new Wellbore()
            {
                UidWell = Well.Uid,
                Uid = Wellbore.Uid,
                CommonData = new CommonData
                {
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                       extensionName1, extensionName2
                    }
                }
            };

            var results = DevKit.Update<WellboreList, Wellbore>(update);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.ChildUidNotUnique, results.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore without schema version")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_468_No_Schema_Version_Declared()
        {
            // Add Well
            DevKit.AddAndAssert(Well);

            // Add Wellbore
            DevKit.AddAndAssert(Wellbore);

            // Try to update without declaring schema version
            var missingVersionXml = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                           "   <wellbore uidWell=\"{0}\" uid=\"{1}\">" + Environment.NewLine +
                           "       <nameWell>No Plural Well</nameWell>" + Environment.NewLine +
                           "       <name>No Plural Wellbore</name>" + Environment.NewLine +
                           "   </wellbore>" + Environment.NewLine +
                           "</wellbores>";

            var xmlIn = string.Format(missingVersionXml, Well.Uid, Wellbore.Uid);
            var response = DevKit.UpdateInStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, response.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore without a proper XML template")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_483_Update_With_Non_Conforming_Template()
        {
            // Add Well
            DevKit.AddAndAssert(Well);

            // Add Wellbore
            DevKit.AddAndAssert(Wellbore);

            // Try to update without declaring schema version
            var nonConformingTempalteXml = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine + "</wellbores>";

            var xmlIn = string.Format(nonConformingTempalteXml, Well.Uid, Wellbore.Uid);
            var response = DevKit.UpdateInStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, response.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore if it results in removing the value of a required element")]
        public void Wellbore141DataAdapter_UpdateInStore_Error_484_Update_Will_Delete_Required_Element()
        {
            // Add Well
            DevKit.AddAndAssert(Well);

            // Add Wellbore
            DevKit.AddAndAssert(Wellbore);

            // Try to update without declaring schema version
            var nullingNameXml = "<wellbores xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <wellbore uidWell=\"{0}\" uid=\"{1}\">" + Environment.NewLine +
                           "       <name></name>" + Environment.NewLine +
                           "   </wellbore>" + Environment.NewLine +
                           "</wellbores>";

            var xmlIn = string.Format(nullingNameXml, Well.Uid, Wellbore.Uid);
            var response = DevKit.UpdateInStore(ObjectTypes.Wellbore, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, response.Result);
        }
    }
}
