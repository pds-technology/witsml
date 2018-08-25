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
using System.Collections.Generic;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Xml;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Wells
{
    [TestClass]
    public partial class Well141DataAdapterAddTests : Well141TestBase
    {
        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_AddWell()
        {
            DevKit.AddAndAssert(Well);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Uid_Returned()
        {
            var response = DevKit.AddAndAssert(Well);

            var uid = response.SuppMsgOut;
            Assert.AreEqual(Well.Uid, uid);

            var query = new Well { Uid = uid };
            var result = DevKit.Query<WellList, Well>(query);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var well = result.FirstOrDefault();
            Assert.IsNotNull(well);
            Assert.AreEqual(uid, well.Uid);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Case_Preserved()
        {
            var nameLegal = "Well Legal Name";
            Well.NameLegal = nameLegal;
            DevKit.AddAndAssert(Well);

            var query = new Well { Uid = Well.Uid, NameLegal = string.Empty };
            var result = DevKit.Query<WellList, Well>(query);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var well = result.FirstOrDefault();
            Assert.IsNotNull(well);
            Assert.AreEqual(nameLegal, well.NameLegal);  // Section 6.1.5
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_With_PrivateGroupOnly_True()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            // Add a well with PrivateGroupOnly set to false
            Well.CommonData = new CommonData() { PrivateGroupOnly = true };
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = DevKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsFalse(result.Any(x => uidWell.Equals(x.Uid)));

        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_With_PrivateGroupOnly_False()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            // Add a well with PrivateGroupOnly set to false
            Well.CommonData = new CommonData() { PrivateGroupOnly = false };
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = DevKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsTrue(result.Any(x => uidWell.Equals(x.Uid)));
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_With_Default_PrivateGroupOnly()
        {
            // Prevent large debug log output
            WitsmlSettings.TruncateXmlOutDebugSize = 100;

            // Add a well with default PrivateGroupOnly
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = DevKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsTrue(result.Any(x => uidWell.Equals(x.Uid)));
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_AddWell_And_Ignore_Invalid_Element()
        {
            var wellName = DevKit.Name("Bug-5855-AddToStore-Bad-Element");

            string xmlIn = string.Format(DevKit141Aspect.BasicAddWellXmlTemplate, null, wellName, "<fieldsssssss>Big Field</fieldsssssss>");

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_AddWell_And_Ignore_Invalid_Attribute()
        {
            var wellName = DevKit.Name("Bug-5855-AddToStore-Bad-Attribute");

            string xmlIn = string.Format(DevKit141Aspect.BasicAddWellXmlTemplate, null, wellName, "<field abc=\"cde\">Big Field</field>");

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = new Well { Uid = response.SuppMsgOut };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Big Field", result[0].Field);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_AddWell_With_Invalid_Child_Element()
        {
            var wellName = DevKit.Name("Bug-5855-AddToStore-Invalid-Child-Element");

            string xmlIn = string.Format(DevKit141Aspect.BasicAddWellXmlTemplate, null, wellName, "<field><abc>Big Field</abc></field>");

            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = new Well { Uid = response.SuppMsgOut };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(wellName, result[0].Name);
            Assert.IsNull(result[0].Field);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Acquisition_Success()
        {
            DevKit.AddValidAcquisition(Well);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Acquisition_Error_409()
        {
            Well.CommonData = new CommonData
            {
                AcquisitionTimeZone = new List<TimestampedTimeZone>()
                {
                    new TimestampedTimeZone() {DateTimeSpecified = false, Value = "+01:00"},
                    new TimestampedTimeZone() {DateTimeSpecified = true, DateTime = DateTime.UtcNow, Value = "+02:00"},
                    new TimestampedTimeZone() {DateTimeSpecified = false, Value = "+03:00"} // This is not allowed
                }
            };

            DevKit.AddAndAssert(Well, ErrorCodes.InputTemplateNonConforming);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Saves_And_Retrieves_CustomData_Elements()
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
        }

        [TestMethod]
        public void WitsmlValidator_AddToStore_Can_Add_Well_With_Default_TimeZone_From_WitsmlSettings()
        {
            WitsmlSettings.DefaultTimeZone = DevKit.TimeZone;
            Well.TimeZone = null;

            DevKit.AddAndAssert<WellList, Well>(Well);
        }
    }
}
