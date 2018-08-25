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
using System.Xml;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    /// <summary>
    /// Well141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public partial class Well141DataAdapterDeleteTests : Well141TestBase
    {
        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Delete_FullWell()
        {
            Well = DevKit.GetFullWell();
            Well.Uid = DevKit.Uid();

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert well is added
            DevKit.GetAndAssert(Well);

            // Delete well
            var delete = new Well {Uid = Well.Uid};
            DevKit.DeleteAndAssert(delete);

            // Assert the well has been deleted
            var query = new Well { Uid = Well.Uid };

            var results = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Delete_FullWell_With_Case_Insensitive_Uid()
        {
            var uid = DevKit.Uid();
            Well = DevKit.GetFullWell();
            Well.Uid = "w" + uid;

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert well is added
            DevKit.GetAndAssert(Well);

            // Delete well
            var delete = new Well { Uid = "W" + uid };
            DevKit.DeleteAndAssert(delete);

            // Assert the well has been deleted
            var query = new Well { Uid = Well.Uid };

            var results = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            Well.Country = "USA";
            Well.DateTimeSpud = DateTimeOffset.UtcNow;

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Well);
            Assert.AreEqual(Well.Country, result.Country);
            Assert.AreEqual(Well.DateTimeSpud, result.DateTimeSpud);

            // Partial delete well
            const string delete = "<country /><dTimSpud />";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the well elements has been deleted
            result = DevKit.GetAndAssert(Well);
            Assert.IsNull(result.Country);
            Assert.IsNull(result.DateTimeSpud);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Attributes()
        {
            var datumKb = DevKit.WellDatum("KB", ElevCodeEnum.KB, "KB");
            datumKb.DatumName = new WellKnownNameStruct {Code = "5106", NamingSystem = "EPSG", Value = "KB"};

            Well.WellDatum = new List<WellDatum> {datumKb};

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Well);
            var data = result.WellDatum;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Count);

            // Partial delete well
            var delete = "<wellDatum uid=\"KB\">" + Environment.NewLine +
                    "<datumName code=\"\" />" + Environment.NewLine +
                "</wellDatum>";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the attributes has been deleted
            result = DevKit.GetAndAssert(Well);
            data = result.WellDatum;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Count);
            var datum = data.FirstOrDefault();
            Assert.IsNotNull(datum);
            Assert.IsNotNull(datum.DatumName);
            Assert.IsNull(datum.DatumName.Code);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Elements()
        {
            var testCommonData = new CommonData
            {
                Comments = "Testing partial delete nested elements",
                ItemState = ItemState.plan
            };

            Well.CommonData = testCommonData;

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Well);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete well
            const string delete = "<commonData><comments /><itemState /></commonData>";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the well elements has been deleted
            result = DevKit.GetAndAssert(Well);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.IsNull(commonData.Comments);
            Assert.IsNull(commonData.ItemState);
        }

        [TestMethod]
        [Description("Tests the removal of the 1st wellDatum element and unset the code element of the 2nd wellDatum element")]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Elements()
        {
            var datumKb = DevKit.WellDatum("KB", ElevCodeEnum.KB, "KB");
            var datumSl = DevKit.WellDatum("SL", ElevCodeEnum.SL, "SL");
            Well.WellDatum = new List<WellDatum> {datumKb, datumSl};

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Well);
            var data = result.WellDatum;
            Assert.AreEqual(2, data.Count);
            var datum1 = data.FirstOrDefault(d => d.Uid == datumKb.Uid);
            Assert.IsNotNull(datum1);
            var datum2 = data.FirstOrDefault(d => d.Uid == datumSl.Uid);
            Assert.IsNotNull(datum2);

            // Partial delete well
            var delete = "<wellDatum uid=\"KB\" />" + Environment.NewLine +
                "<wellDatum uid=\"SL\">" + Environment.NewLine +
                    "<code />" + Environment.NewLine +
                "</wellDatum>";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the partial delete of the recurring elements
            result = DevKit.GetAndAssert(Well);
            data = result.WellDatum;
            Assert.AreEqual(1, data.Count);
            datum1 = data.FirstOrDefault(d => d.Uid == datumKb.Uid);
            Assert.IsNull(datum1);
            datum2 = data.FirstOrDefault(d => d.Uid == datumSl.Uid);
            Assert.IsNotNull(datum2);
            Assert.IsNull(datum2.Code);
        }

        [TestMethod]
        [Description("Tests the removal of the 1st wellDatum element and unset the code element of the 2nd wellDatum element")]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Nested_Recurring_Elements()
        {
            var datumKb = DevKit.WellDatum("KB", ElevCodeEnum.KB, "KB");
            var datumSl = DevKit.WellDatum("SL", ElevCodeEnum.SL, "SL");

            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var ext2 = DevKit.ExtensionNameValue("Ext-2", "2.0", "ft");
            var ext3 = DevKit.ExtensionNameValue("Ext-3", "3.0", "s");
            ext3.Description = "Testing partial delete of nested recurring elements";

            datumKb.ExtensionNameValue = new List<ExtensionNameValue> {ext1};
            datumSl.ExtensionNameValue = new List<ExtensionNameValue> {ext2, ext3};
            Well.WellDatum = new List<WellDatum> { datumKb, datumSl };

            // Add well
            DevKit.AddAndAssert(Well);

            // Assert all testing elements are added
            var result = DevKit.GetAndAssert(Well);
            var data = result.WellDatum;
            Assert.AreEqual(2, data.Count);
            var datum1 = data.FirstOrDefault(d => d.Uid == datumKb.Uid);
            Assert.IsNotNull(datum1);
            var extDatum1 = datum1.ExtensionNameValue;
            Assert.IsNotNull(extDatum1);
            Assert.AreEqual(1, extDatum1.Count);
            var datum2 = data.FirstOrDefault(d => d.Uid == datumSl.Uid);
            Assert.IsNotNull(datum2);
            var extDatum2 = datum2.ExtensionNameValue;
            Assert.IsNotNull(extDatum2);
            Assert.AreEqual(2, extDatum2.Count);

            // Partial delete well
            var delete = "<wellDatum uid=\"KB\" />" + Environment.NewLine +
                "<wellDatum uid=\"SL\">" + Environment.NewLine +
                    "<code />" + Environment.NewLine +
                    "<extensionNameValue uid=\"Ext-2\" />" + Environment.NewLine +
                    "<extensionNameValue uid=\"Ext-3\">" + Environment.NewLine + 
                        "<description />" + Environment.NewLine +
                    "</extensionNameValue>" + Environment.NewLine +
                "</wellDatum>";
            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert wellDatum
            result = DevKit.GetAndAssert(Well);
            data = result.WellDatum;
            Assert.AreEqual(1, data.Count);
            datum1 = data.FirstOrDefault(d => d.Uid == datumKb.Uid);
            Assert.IsNull(datum1);
            datum2 = data.FirstOrDefault(d => d.Uid == datumSl.Uid);
            Assert.IsNotNull(datum2);
            Assert.IsNull(datum2.Code);

            // Assert extensionNameValues
            extDatum2 = datum2.ExtensionNameValue;
            Assert.IsNotNull(extDatum2);
            Assert.AreEqual(1, extDatum2.Count);
            var resultExt2 = extDatum2.FirstOrDefault(e => e.Uid == ext2.Uid);
            Assert.IsNull(resultExt2);
            var resultExt3 = extDatum2.FirstOrDefault(e => e.Uid == ext3.Uid);
            Assert.IsNotNull(resultExt3);
            Assert.IsNull(resultExt3.Description);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without plural container")]
        public void Well141DataAdapter_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var nonPluralWell = "<well xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                           "   <well uid=\"{0}\">" + Environment.NewLine +
                           "       <name>No Plural well</name>" + Environment.NewLine +
                           "   </well>" + Environment.NewLine +
                           "</well>";

            var xmlIn = string.Format(nonPluralWell, Well.Uid);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Well141DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = DevKit.Delete<WellList, Well>(Well, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing queryIn")]
        public void Well141DataAdapter_DeleteFromStore_Error_408_Delete_Without_QueryIn()
        {
            var results = DevKit.DeleteFromStore(ObjectTypes.Well, string.Empty, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with QueryIn that doesn't conform to delete schema")]
        public void Well141DataAdapter_DeleteFromStore_Error_409_XmlIn_Doesnt_Conform_To_Delete_Schema()
        {
            // Add well
            Well.Field = "Field1";
            DevKit.AddAndAssert(Well);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                $"<field /><field />");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the well uid")]
        public void Well141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Delete well with invalid element
            var deleteXml = string.Format(BasicXMLTemplate, string.Empty, string.Empty);
            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with missing uid attribute.")]
        public void Well141DataAdapter_DeleteFromStore_Error_416_Empty_UID_Attribute()
        {
            // Add well
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            Well.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            DevKit.AddAndAssert(Well);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                "<commonData><extensionNameValue uid=\"\" /></commonData>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUidSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with a missing uom")]
        public void Well141DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            // Add well
            Well.PercentInterest = new DimensionlessMeasure(0, DimensionlessUom.Item);
            DevKit.AddAndAssert(Well);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                "<pcInterest uom=\"\" />");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUomSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without all required fields on an optional element")]
        public void Well141DataAdapter_DeleteFromStore_Error_418_Missing_Uid()
        {
            // Add well
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            Well.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            DevKit.AddAndAssert(Well);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                "<commonData><extensionNameValue /></commonData>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForDelete, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore specify an empty non-recurring container-element with no unique identifier in the schema")]
        public void Well141DataAdapter_DeleteFromStore_Error_419_Specifying_A_Non_Recuring_Container_Without_UID()
        {
            // Add a minimal test well and Assert its Success
            DevKit.AddAndAssert(Well);

            // Add wellbore
            var wellbore = new Wellbore()
            {
                UidWell = Well.Uid,
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore 01"),
                NameWell = Well.Name
            };
            DevKit.AddAndAssert(wellbore);

            // Add rig
            var rig = new Rig()
            {
                UidWell = Well.Uid,
                UidWellbore = wellbore.Uid,
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Rig 01"),
                NameWellbore = wellbore.Name,
                NameWell = Well.Name
            };
            DevKit.AddAndAssert(rig);
            var wellDatum = new WellDatum()
            {
                Uid = "RIG",
                Name = "Rig",
                Rig = new RefWellWellboreRig()
                {
                    RigReference = new RefNameString()
                    {
                        UidRef = rig.Uid,
                        Value = rig.Name
                    }
                }
            };

            var returnWell = DevKit.GetAndAssert(Well);
            returnWell.WellDatum = new List<WellDatum>()
            {
                wellDatum
            };
            DevKit.UpdateAndAssert(returnWell);
            
            // Delete 
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                $"<wellDatum uid=\"{wellDatum.Uid}\"><rig /></wellDatum>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyNonRecurringElementSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore specify an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Well141DataAdapter_DeleteFromStore_Error_420_Specifying_A_Non_Recuring_Element_That_Is_Required()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Delete sub-node (element) "timeZone" which is required.
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid, "<timeZone />" );

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyMandatoryNodeSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore specify an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Well141DataAdapter_DeleteFromStore_Error_420_Specifying_A_Non_Recuring_Attribute_That_Is_Required()
        {
            // Add a minimal test well and Assert its Success
            var wellDatum = new WellDatum()
            {
                Code = ElevCodeEnum.KB,
                Uid = "KB",
                Name = "Kelly Bushing",
                DatumName = new WellKnownNameStruct()
                {
                    Code = "XX",
                    NamingSystem = "TestName",
                    Value = "Test"
                }
            };

            Well.WellDatum = new List<WellDatum>
            {
              wellDatum 
            };

            DevKit.AddAndAssert(Well);

            // Delete 
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                $"<wellDatum uid=\"{wellDatum.Uid}\"><datumName namingSystem=\"\" /></wellDatum>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyMandatoryNodeSpecified, results.Result);

        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore if it results in a mandatory node being deleted")]
        public void Well141DataAdapter_DeleteFromStore_Error_421_Deleting_A_Mandatory_Node()
        {
            // Add well
            var location = new Location
            {
                Uid = "Loc-1",
                WellCRS = new RefNameString
                {
                    UidRef = "localWell1",
                    Value = "LocalWellCRS"
                }
            };

            var referencePoint = new ReferencePoint
            {
                Uid = "localWell1",
                Name = "Well01",
                Location = new List<Location>()
                {
                    location
                }
            };

            Well.ReferencePoint = new List<ReferencePoint>()
            {
                referencePoint
            };

            DevKit.AddAndAssert(Well);

            // Delete well
            var deleteXml = string.Format(BasicXMLTemplate, Well.Uid,
                $"<referencePoint uid=\"{referencePoint.Uid}\"><location uid=\"{location.Uid}\" /></referencePoint>");

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MustRetainOneRecurringNode, results.Result);
        }

        [TestMethod, Description("Tests trying to delete a well that has children")]
        public void Well141DataAdapter_DeleteFromStore_Error_432_Deleting_AWell_Has_A_Child()
        {
            // Add well
            DevKit.AddAndAssert(Well);

            // Add wellbore
            var wellbore = new Wellbore()
            {
                UidWell = Well.Uid,
                NameWell = Well.Name,
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore 01")
            };

            DevKit.AddAndAssert(wellbore);

            var delete = new Well()
            {
                Uid = Well.Uid
            };
            // Delete well
            var deleteResponse = DevKit.Delete<WellList, Well>(delete);
            Assert.IsNotNull(deleteResponse);
            Assert.AreEqual((short)ErrorCodes.NotBottomLevelDataObject, deleteResponse.Result);
        }

        [TestMethod, Description("Tests trying to delete a well that doesn't exist")]
        public void Well141DataAdapter_DeleteFromStore_Error_433_Deleting_AWell_That_Doesnt_Exist()
        {
            // Delete well
            var deleteResponse = DevKit.Delete<WellList, Well>(Well);
            Assert.IsNotNull(deleteResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, deleteResponse.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Well141DataAdapter_DeleteFromStore_Error_444_Deleting_With_More_Than_One_Data_Object()
        {
            // Add well 
            Well.Field = "Field1";
            DevKit.AddAndAssert(Well);

            // Add second well
            var well2 = new Well()
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Well 02"),
                TimeZone = DevKit.TimeZone,
                Field = "Field2"
            };
            DevKit.AddAndAssert(well2);

            var multiObjectXml = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
                          "   <well uid=\"{0}\">" + Environment.NewLine +
                          "{1}" +
                          "   </well>" + Environment.NewLine +
                          "   <well uid=\"{2}\">" + Environment.NewLine +
                          "{3}" +
                          "   </well>" + Environment.NewLine +
                          "</wells>";

            // Delete Multiple Well Objects
            var deleteXml = string.Format(
                multiObjectXml,
                Well.Uid,
                "<field />",
                well2.Uid,
                "<field />"
                );

            var results = DevKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Acquisition_Success()
        {
            // Add well with three acquisitions
            var response = DevKit.AddValidAcquisition(Well);

            var deleteWellAcqusition = new Well()
            {
                Uid = response.SuppMsgOut,
                CommonData = new CommonData
                {
                    AcquisitionTimeZone = new List<TimestampedTimeZone>()
                    {
                        new TimestampedTimeZone() {DateTimeSpecified = false},
                    }
                }
            };

            // Delete well acqusitions and Assert success
            DevKit.DeleteAndAssert(deleteWellAcqusition, partialDelete: true);

            var queryWell = DevKit.GetAndAssert(new Well() { Uid = response.SuppMsgOut });
            Assert.IsNotNull(queryWell.CommonData);
            Assert.IsNotNull(queryWell.CommonData.AcquisitionTimeZone);
            Assert.AreEqual(0, queryWell.CommonData.AcquisitionTimeZone.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Remove_CustomData_Elements()
        {
            var doc = new XmlDocument();

            var element1 = doc.CreateElement("FirstItem", "http://www.witsml.org/schemas/1series");
            element1.InnerText = "123.45";

            var element2 = doc.CreateElement("LastItem", element1.NamespaceURI);
            element2.InnerText = "987.65";

            Well.CustomData = new CustomData
            {
                Any = DevKit.List(element1, element2)
            };

            DevKit.AddAndAssert<WellList, Well>(Well);

            // Query
            var query = new Well { Uid = Well.Uid };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            var well = result.FirstOrDefault();

            Assert.IsNotNull(well?.CustomData);
            Assert.AreEqual(2, well.CustomData.Any.Count);

            Assert.AreEqual(element1.LocalName, well.CustomData.Any[0].LocalName);
            Assert.AreEqual(element1.InnerText, well.CustomData.Any[0].InnerText);

            Assert.AreEqual(element2.LocalName, well.CustomData.Any[1].LocalName);
            Assert.AreEqual(element2.InnerText, well.CustomData.Any[1].InnerText);

            // Partial delete
            element2.InnerText = string.Empty;

            well = new Well
            {
                Uid = Well.Uid,
                CustomData = new CustomData
                {
                    Any = DevKit.List(element2)
                }
            };

            DevKit.DeleteAndAssert<WellList, Well>(well, partialDelete: true);

            // Query
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            well = result.FirstOrDefault();

            Assert.IsNotNull(well?.CustomData);
            Assert.AreEqual(1, well.CustomData.Any.Count);

            Assert.AreEqual(element1.LocalName, well.CustomData.Any[0].LocalName);
            Assert.AreEqual(element1.InnerText, well.CustomData.Any[0].InnerText);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Partial_Delete_Updates_ChangeLog()
        {
            var wellDatum = DevKit.WellDatum("CV", ElevCodeEnum.CV, "CV");
            var ext1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var ext2 = DevKit.ExtensionNameValue("Ext-2", "2.0", "ft");
            wellDatum.ExtensionNameValue = new List<ExtensionNameValue> { ext1, ext2 };

            Well.WellDatum = new List<WellDatum> { wellDatum };
            Well.Country = "Country";

            DevKit.AddAndAssert<WellList, Well>(Well);

            var delete = @" <country/>
                            <wellDatum uid=""" + wellDatum.Uid + @""">
                                <code/>
                                <extensionNameValue uid=""" + ext1.Uid + @"""/>
                            </wellDatum>";

            var queryIn = string.Format(BasicXMLTemplate, Well.Uid, delete);
            var response = DevKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var well = DevKit.GetAndAssert(Well);
            Assert.IsNull(well.Country);
            Assert.IsNull(well.WellDatum.First().Code);
            Assert.AreEqual(1, well.WellDatum.First().ExtensionNameValue.Count);
            Assert.IsNull(well.WellDatum.Find(e => e.Uid == ext1.Uid));

            var expectedHistoryCount = 2;
            var expectedChangeType = ChangeInfoType.update;
            DevKit.AssertChangeLog(well, expectedHistoryCount, expectedChangeType);
        }
    }
}
