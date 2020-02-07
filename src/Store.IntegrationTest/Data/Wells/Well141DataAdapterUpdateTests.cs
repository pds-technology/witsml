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
    /// Well141DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public partial class Well141DataAdapterUpdateTests : Well141TestBase
    {
        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Update_A_List_Element()
        {
            // Add well
            var well = DevKit.GetFullWell();
            well.Uid = DevKit.Uid();
            DevKit.AddAndAssert(well);
            // Query well 
            var returnWell = DevKit.GetAndAssert(well);

            var welldatum = returnWell.WellDatum.FirstOrDefault(x => x.Uid.Equals("SL"));
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Sea Level", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.SL, welldatum.Code);

            // Update well
            var datumSl = DevKit.WellDatum("Sea Level", ElevCodeEnum.LAT, "SL");

            var update = new Well() { Uid = well.Uid, WellDatum = DevKit.List(datumSl) };
            DevKit.UpdateAndAssert(update);

            // Query updated well
            returnWell = DevKit.GetAndAssert(well);

            welldatum = returnWell.WellDatum.FirstOrDefault(x => x.Uid.Equals("SL"));
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Sea Level", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.LAT, welldatum.Code);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_UpdateWell_And_Ignore_Invalid_Element()
        {
            Well.Operator = "AAA Company";

            DevKit.AddAndAssert(Well);

            // Update well with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid,
                "<operator>BBB Company</operator>" +
                "<fieldsssssss>Big Field</fieldsssssss>");

            var results = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var result = DevKit.GetAndAssert(Well);
            Assert.AreEqual("BBB Company", result.Operator);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_UpdateWell_And_Ignore_Invalid_Attribute()
        {
            Well.Operator = "AAA Company";

            DevKit.AddAndAssert(Well);

            // Update well with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid,
                "<operator>BBB Company</operator>" +
                "<field abc=\"abc\">Big Field</field>");

            var results = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var result = DevKit.GetAndAssert(Well);
            Assert.AreEqual("BBB Company", result.Operator);
            Assert.AreEqual("Big Field", result.Field);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Update_With_Invalid_Child_Element()
        {
            Well.Operator = "AAA Company";
            DevKit.AddAndAssert(Well);

            // Update well with invalid element
            var updateXml = string.Format(BasicXMLTemplate, Well.Uid,
                "<operator><abc>BBB Company</abc></operator>");

            var results = DevKit.UpdateInStore(ObjectTypes.Well, updateXml, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, results.Result);

            // Query the updated well 
            var result = DevKit.GetAndAssert(Well);
            Assert.AreEqual(Well.Name, result.Name);
            Assert.IsNull(result.Operator);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Error_409_Missing_Required_Fields_For_Optional_Property()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(Well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;

            var wellDatum = new WellDatum
            {
                Uid = "DF",
                Code = ElevCodeEnum.DF
            };

            var update = new Well
            {
                Uid = uid,
                WellDatum = new List<WellDatum> { wellDatum }
            };

            var updateResponse = DevKit.Update<WellList, Well>(update);

            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.UpdateTemplateNonConforming, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding a recurring element for the first time on an UpdateInStore")]
        public void Wel141lDataAdapter_UpdateInStore_Add_Recurring_Element_Success()
        {
            Well.Name = DevKit.Name("WellAddRecurringOnUpdate");
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well that adds a recurring element for the first time on update
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellDatum = new List<WellDatum>
                {
                    DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "KB"),
                    DevKit.WellDatum("Casing Flange", ElevCodeEnum.CF, "CF")
                }
            };

            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an nested, non-recurring, element for the first time on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Nested_Element_Success()
        {
            // Add a minimal test well and Assert its Success
            Well.Name = DevKit.Name("WellAddNestedOnUpdate");
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well that adds a nested (non-recurring) element for the first time on update
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellPublicLandSurveySystemLocation =
                    new PublicLandSurveySystem() { PrincipalMeridian = PrincipalMeridian.ChoctawMeridian, Range = 1 }
            };

            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an element with attributes for the first time on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Element_With_Attribute_Success()
        {
            // Add a wellDatum to the test Well
            Well.Name = DevKit.Name("WellAddWithAttributesOnUpdate");
            Well.WellDatum = new List<WellDatum> { DevKit.WellDatum("Kelly Bushing", ElevCodeEnum.KB, "KB") };

            // Add a well with a datum that we can reference in the update
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with a new element that has attributes and Assert Success
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                WellheadElevation = new WellElevationCoord() { Uom = WellVerticalCoordinateUom.m, Datum = "KB", Value = 0.0 }
            };

            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding a nested array element, e.g. referencePoint.location with elements having uom attributes, e.g. latitude during update")]
        public void Well141DataAdapter_UpdateInStore_Add_Nested_Array_Element_With_Uom_Success()
        {
            var well = DevKit.GetFullWell();
            well.Uid = DevKit.Uid();
            var referencePoint = well.ReferencePoint;
            well.ReferencePoint = null;
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Create an update well with a new element that has attributes and Assert Success
            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                ReferencePoint = referencePoint
            };

            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);
        }

        [TestMethod, Description("Tests adding an extensionNameValue field to commonData on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Extension_Name_Value_Success()
        {
            // Add a minimal test well and Assert its Success
            DevKit.AddAndAssert(Well);

            var extensionName1 = DevKit.ExtensionNameValue("Ext-1", "1.0", "m");

            // Create an update well that adds a nested (non-recurring) element for the first time on update
            var updateWell = new Well()
            {
                Uid = Well.Uid,
                CommonData = new CommonData
                {
                    ExtensionNameValue = new List<ExtensionNameValue>
                    {
                       extensionName1
                    }
                }
            };

            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var result = DevKit.GetAndAssert(Well);
            var commonData = result.CommonData;
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

        [TestMethod, Description("Tests adding an extensionNameValue field to commonData on an UpdateInStore")]
        public void Well141DataAdapter_UpdateInStore_Add_Complex_Element_To_Existing_Recurring_Element()
        {
            // Add a minimal test well and Assert its Success
            Well.WellDatum = new List<WellDatum>
            {
                new WellDatum()
                {
                    Code = ElevCodeEnum.KB,
                    Uid = "KB",
                    Name = "Kelly Bushing"
                }
            };
            DevKit.AddAndAssert(Well);


            // Create an update well that will update the existing KB datum with a datumName
            var datamWithDatumName = new WellDatum()
            {
                Uid = "KB",
                DatumName = new WellKnownNameStruct()
                {
                    Code = "XX",
                    NamingSystem = "TestName",
                    Value = "Test"
                }
            };

            var updateWell = new Well()
            {
                Uid = Well.Uid,
                WellDatum = new List<WellDatum>()
                {
                    datamWithDatumName
                }
            };

            // Update well
            var updateResponse = DevKit.Update<WellList, Well>(updateWell);
            Assert.IsNotNull(updateResponse);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            // Query well to make sure datumName was added
            var result = DevKit.GetAndAssert(Well);
            var welldatum = result.WellDatum.FirstOrDefault(x => x.Uid.Equals("KB"));
            Assert.IsNotNull(welldatum);
            Assert.AreEqual("Kelly Bushing", welldatum.Name);
            Assert.AreEqual(ElevCodeEnum.KB, welldatum.Code);

            var datumName = welldatum.DatumName;
            Assert.IsNotNull(datumName);
            Assert.AreEqual("TestName", datumName.NamingSystem);
            Assert.AreEqual("XX", datumName.Code);
            Assert.AreEqual("Test", datumName.Value);
        }


        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Acquisition_Success()
        {
            // Add a valid well with three AcquisitionTimeZones
            var response = DevKit.AddValidAcquisition(Well);

            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                CommonData = new CommonData
                {
                    AcquisitionTimeZone = new List<TimestampedTimeZone>()
                    {
                        new TimestampedTimeZone() {DateTimeSpecified = true, DateTime = DateTime.UtcNow, Value = "+03:00"}
                    }
                }
            };

            // Update and Assert for success
            DevKit.UpdateAndAssert(updateWell);

            // Retrieve the updated well and check that there are four acquisitions
            var queryWell = DevKit.GetAndAssert(new Well() { Uid = response.SuppMsgOut });
            Assert.IsNotNull(queryWell.CommonData);
            Assert.IsNotNull(queryWell.CommonData.AcquisitionTimeZone);
            Assert.AreEqual(1, queryWell.CommonData.AcquisitionTimeZone.Count, "Acquisition time zone count did not match");
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Acquisition_Error_483()
        {
            // Add a valid well with three AcquisitionTimeZones
            var response = DevKit.AddValidAcquisition(Well);

            var updateWell = new Well()
            {
                Uid = response.SuppMsgOut,
                CommonData = new CommonData
                {
                    AcquisitionTimeZone = new List<TimestampedTimeZone>()
                    {
                        // Appending a subsequent TimestampedTimeZone without a DateTime specified is an error
                        new TimestampedTimeZone { DateTimeSpecified = false, Value = "+03:00" }
                    }
                }
            };

            // Update and Assert for error
            DevKit.UpdateAndAssert(updateWell, ErrorCodes.UpdateTemplateNonConforming);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Can_Update_CustomData_Elements()
        {
            DevKit.AddAndAssert<WellList, Well>(Well);

            // Update with New Data
            var doc = new XmlDocument();

            var element1 = doc.CreateElement("FirstItem", "http://www.witsml.org/schemas/1series");
            element1.InnerText = "123.45";

            var element2 = doc.CreateElement("LastItem", element1.NamespaceURI);
            element2.InnerText = "987.65";

            Well.CustomData = new CustomData
            {
                Any = DevKit.List(element1, element2)
            };

            DevKit.UpdateAndAssert<WellList, Well>(Well);

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

            // Partial Update
            well.CustomData.Any[1].InnerText = "0.0";

            var element3 = doc.CreateElement("NewItem", element1.NamespaceURI);
            element3.InnerText = "abc";
            well.CustomData.Any.Add(element3);

            DevKit.UpdateAndAssert<WellList, Well>(well);

            // Query
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            well = result.FirstOrDefault();

            Assert.IsNotNull(well?.CustomData);
            Assert.AreEqual(3, well.CustomData.Any.Count);

            Assert.AreEqual(element1.LocalName, well.CustomData.Any[0].LocalName);
            Assert.AreEqual(element1.InnerText, well.CustomData.Any[0].InnerText);

            Assert.AreEqual(element2.LocalName, well.CustomData.Any[1].LocalName);
            Assert.AreEqual("0.0", well.CustomData.Any[1].InnerText);

            Assert.AreEqual(element3.LocalName, well.CustomData.Any[2].LocalName);
            Assert.AreEqual(element3.InnerText, well.CustomData.Any[2].InnerText);
        }

        [TestMethod]
        public void Well141DataAdapter_UpdateInStore_Can_Update_Well_To_Add_Local_Crs()
        {
            AddParents();

            var wellCrs = new WellCRS
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name(),
                LocalCRS = new LocalCRS
                {
                    UsesWellAsOrigin = true,
                    XRotationCounterClockwise = false,
                    YAxisAzimuth = new YAxisAzimuth
                    {
                        Value = 10,
                        Uom = PlaneAngleUom.dega,
                        NorthDirection = AziRef.gridnorth
                    }
                }
            };

            Well.WellCRS = wellCrs.AsList();

            DevKit.AddAndAssert<WellList, Well>(Well);
            DevKit.GetAndAssert<WellList, Well>(Well);

            // Update UID and other properties to simulate adding a new WellCRS
            wellCrs.Uid = DevKit.Uid();
            wellCrs.Name = DevKit.Name();
            wellCrs.LocalCRS.YAxisAzimuth.Value = 20;

            DevKit.UpdateAndAssert<WellList, Well>(Well);
            DevKit.GetAndAssert<WellList, Well>(Well);
        }
    }
}
