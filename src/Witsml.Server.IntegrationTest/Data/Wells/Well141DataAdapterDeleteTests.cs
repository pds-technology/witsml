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

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Well141DataAdapter Delete tests.
    /// </summary>
    [TestClass]
    public class Well141DataAdapterDeleteTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _devKit = null;
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Delete_Full_Well()
        {
            _well = _devKit.CreateFullWell();
            _well.Uid = _devKit.Uid();

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert well is added
            _devKit.GetSingleWellAndAssert(_well);

            // Delete well
            var delete = new Well {Uid = _well.Uid};
            _devKit.DeleteAndAssert(delete);

            // Assert the well has been deleted
            var query = new Well { Uid = _well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Delete_Full_Well_With_Case_Insensitive_Uid()
        {
            var uid = _devKit.Uid();
            _well = _devKit.CreateFullWell();
            _well.Uid = "w" + uid;

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert well is added
            _devKit.GetSingleWellAndAssert(_well);

            // Delete well
            var delete = new Well { Uid = "W" + uid };
            _devKit.DeleteAndAssert(delete);

            // Assert the well has been deleted
            var query = new Well { Uid = _well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Elements()
        {
            _well.Country = "USA";
            _well.DateTimeSpud = DateTimeOffset.UtcNow;

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert all testing elements are added
            var result = _devKit.GetSingleWellAndAssert(_well);
            Assert.AreEqual(_well.Country, result.Country);
            Assert.AreEqual(_well.DateTimeSpud, result.DateTimeSpud);

            // Partial delete well
            const string delete = "<country /><dTimSpud />";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the well elements has been deleted
            result = _devKit.GetSingleWellAndAssert(_well);
            Assert.IsNull(result.Country);
            Assert.IsNull(result.DateTimeSpud);
        }

        [TestMethod]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Attributes()
        {
            var datumKb = _devKit.WellDatum("KB", ElevCodeEnum.KB, "KB");
            datumKb.DatumName = new WellKnownNameStruct {Code = "5106", NamingSystem = "EPSG", Value = "KB"};

            _well.WellDatum = new List<WellDatum> {datumKb};

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert all testing elements are added
            var result = _devKit.GetSingleWellAndAssert(_well);
            var data = result.WellDatum;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Count);

            // Partial delete well
            var delete = "<wellDatum uid=\"KB\">" + Environment.NewLine +
                    "<datumName code=\"\" />" + Environment.NewLine +
                "</wellDatum>";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the attributes has been deleted
            result = _devKit.GetSingleWellAndAssert(_well);
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

            _well.CommonData = testCommonData;

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert all testing elements are added
            var result = _devKit.GetSingleWellAndAssert(_well);
            var commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.AreEqual(testCommonData.Comments, commonData.Comments);
            Assert.AreEqual(testCommonData.ItemState, commonData.ItemState);

            // Partial delete well
            const string delete = "<commonData><comments /><itemState /></commonData>";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the well elements has been deleted
            result = _devKit.GetSingleWellAndAssert(_well);
            commonData = result.CommonData;
            Assert.IsNotNull(commonData);
            Assert.IsNull(commonData.Comments);
            Assert.IsNull(commonData.ItemState);
        }

        [TestMethod]
        [Description("Tests the removal of the 1st wellDatum element and unset the code element of the 2nd wellDatum element")]
        public void Well141DataAdapter_DeleteFromStore_Can_Partial_Delete_Recurring_Elements()
        {
            var datumKb = _devKit.WellDatum("KB", ElevCodeEnum.KB, "KB");
            var datumSl = _devKit.WellDatum("SL", ElevCodeEnum.SL, "SL");
            _well.WellDatum = new List<WellDatum> {datumKb, datumSl};

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert all testing elements are added
            var result = _devKit.GetSingleWellAndAssert(_well);
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
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the partial delete of the recurring elements
            result = _devKit.GetSingleWellAndAssert(_well);
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
            var datumKb = _devKit.WellDatum("KB", ElevCodeEnum.KB, "KB");
            var datumSl = _devKit.WellDatum("SL", ElevCodeEnum.SL, "SL");

            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            var ext2 = _devKit.ExtensionNameValue("Ext-2", "2.0", "ft");
            var ext3 = _devKit.ExtensionNameValue("Ext-3", "3.0", "s");
            ext3.Description = "Testing partial delete of nested recurring elements";

            datumKb.ExtensionNameValue = new List<ExtensionNameValue> {ext1};
            datumSl.ExtensionNameValue = new List<ExtensionNameValue> {ext2, ext3};
            _well.WellDatum = new List<WellDatum> { datumKb, datumSl };

            // Add well
            _devKit.AddAndAssert(_well);

            // Assert all testing elements are added
            var result = _devKit.GetSingleWellAndAssert(_well);
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
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert wellDatum
            result = _devKit.GetSingleWellAndAssert(_well);
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

            var xmlIn = string.Format(nonPluralWell, _well.Uid);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing the object type")]
        public void Well141DataAdapter_DeleteFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var response = _devKit.Delete<WellList, Well>(_well, string.Empty);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWmlTypeIn, response.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore while missing queryIn")]
        public void Well141DataAdapter_DeleteFromStore_Error_408_Delete_Without_QueryIn()
        {
            var results = _devKit.DeleteFromStore(ObjectTypes.Well, string.Empty, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with QueryIn that doesn't conform to delete schema")]
        public void Well141DataAdapter_DeleteFromStore_Error_409_XmlIn_Doesnt_Conform_To_Delete_Schema()
        {
            // Add well
            _well.Field = "Field1";
            _devKit.AddAndAssert(_well);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                $"<field /><field />");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the well uid")]
        public void Well141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Delete well with invalid element
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, string.Empty, string.Empty);
            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);
            Assert.AreEqual((short)ErrorCodes.DataObjectUidMissing, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without all required fields on an optional element")]
        public void Well141DataAdapter_DeleteFromStore_Error_416_Missing_Attribute()
        {
            // Add well
            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            _well.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            _devKit.AddAndAssert(_well);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                "<commonData><extensionNameValue /></commonData>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            // Note - This is currently throwing a -418 instead of 416.  
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForDelete, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with a missing uom")]
        public void Well141DataAdapter_DeleteFromStore_Error_417_Deleting_With_Empty_UOM_Attribute()
        {
            // Add well
            _well.PercentInterest = new DimensionlessMeasure(0, DimensionlessUom.Item);
            _devKit.AddAndAssert(_well);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                "<pcInterest uom=\"\" />");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyUomSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without all required fields on an optional element")]
        public void Well141DataAdapter_DeleteFromStore_Error_418_Missing_Uid_Value()
        {
            // Add well
            var ext1 = _devKit.ExtensionNameValue("Ext-1", "1.0", "m");
            _well.CommonData = new CommonData
            {
                ExtensionNameValue = new List<ExtensionNameValue>
                {
                    ext1
                }
            };

            _devKit.AddAndAssert(_well);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                "<commonData><extensionNameValue uid=\"\" /></commonData>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MissingElementUidForDelete, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore specify an empty non-recurring container-element with no unique identifier in the schema")]
        public void Well141DataAdapter_DeleteFromStore_Error_419_Specifying_A_Non_Recuring_Container_Without_UID()
        {
            // Add a minimal test well and Assert its Success
            _devKit.AddAndAssert(_well);

            // Add wellbore
            var wellbore = new Wellbore()
            {
                UidWell = _well.Uid,
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Wellbore 01"),
                NameWell = _well.Name
            };
            _devKit.AddAndAssert(wellbore);

            // Add rig
            var rig = new Rig()
            {
                UidWell = _well.Uid,
                UidWellbore = wellbore.Uid,
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Rig 01"),
                NameWellbore = wellbore.Name,
                NameWell = _well.Name
            };
            _devKit.AddAndAssert(rig);
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
            var returnWell = _devKit.GetSingleWellAndAssert(_well);
            returnWell.WellDatum = new List<WellDatum>()
            {
                wellDatum
            };

            // Delete 
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                $"<wellDatum uid=\"{wellDatum.Uid}\"><rig /></wellDatum>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.EmptyNonRecurringElementSpecified, results.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore specify an empty node for a non-recurring element or attribute that is mandatory in the write schema.")]
        public void Well141DataAdapter_DeleteFromStore_Error_420_Specifying_A_Non_Recuring_Element_That_Is_Required()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Delete sub-node (element) "timeZone" which is required.
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, "<timeZone />" );

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

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

            _well.WellDatum = new List<WellDatum>
            {
              wellDatum 
            };

            _devKit.AddAndAssert(_well);

            // Delete 
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                $"<wellDatum uid=\"{wellDatum.Uid}\"><datumName namingSystem=\"\" /></wellDatum>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

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

            _well.ReferencePoint = new List<ReferencePoint>();
            _well.ReferencePoint.Add(referencePoint);
            _devKit.AddAndAssert(_well);

            // Delete well
            var deleteXml = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid,
                $"<referencePoint uid=\"{referencePoint.Uid}\"><location uid=\"{location.Uid}\" /></referencePoint>");

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);

            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.MustRetainOneRecurringNode, results.Result);
        }

        [TestMethod, Description("Tests trying to delete a well that has children")]
        public void Well141DataAdapter_DeleteFromStore_Error_432_Deleting_A_Well_Has_A_Child()
        {
            // Add well
            _devKit.AddAndAssert(_well);

            // Add wellbore
            var wellbore = new Wellbore()
            {
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Wellbore 01")
            };

            _devKit.AddAndAssert(wellbore);

            // Delete well
            var deleteResponse = _devKit.Delete<WellList, Well>(_well);
            Assert.IsNotNull(deleteResponse);
            Assert.AreEqual((short)ErrorCodes.NotBottomLevelDataObject, deleteResponse.Result);
        }

        [TestMethod, Description("Tests trying to delete a well that doesn't exist")]
        public void Well141DataAdapter_DeleteFromStore_Error_433_Deleting_A_Well_That_Doesnt_Exist()
        {
            // Delete well
            var deleteResponse = _devKit.Delete<WellList, Well>(_well);
            Assert.IsNotNull(deleteResponse);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotExist, deleteResponse.Result);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore more than one object")]
        public void Well141DataAdapter_DeleteFromStore_Error_444_Deleting_With_More_Than_One_Data_Object()
        {
            // Add well 
            _well.Field = "Field1";
            _devKit.AddAndAssert(_well);

            // Add second well
            var well2 = new Well()
            {
                Uid = _devKit.Uid(),
                Name = _devKit.Name("Well 02"),
                TimeZone = _devKit.TimeZone,
                Field = "Field2"
            };
            _devKit.AddAndAssert(well2);

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
                _well.Uid,
                "<field />",
                well2.Uid,
                "<field />"
                );

            var results = _devKit.DeleteFromStore(ObjectTypes.Well, deleteXml, null, null);
            Assert.IsNotNull(results);
            Assert.AreEqual((short)ErrorCodes.InputTemplateMultipleDataObjects, results.Result);
        }
    }
}
