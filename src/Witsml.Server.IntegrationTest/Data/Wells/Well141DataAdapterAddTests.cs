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

using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Wells
{
    [TestClass]
    public class Well141DataAdapterAddTests
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
        public void TestCleanUp()
        {
            WitsmlSettings.TruncateXmlOutDebugSize = DevKitAspect.DefaultXmlOutDebugSize;
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well()
        {
            AddTestWell(_well);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Uid_Returned()
        {
            var response = AddTestWell(_well);

            var uid = response.SuppMsgOut;
            Assert.AreEqual(_well.Uid, uid);

            var query = new Well { Uid = uid };
            var result = _devKit.Query<WellList, Well>(query);
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
            _well.NameLegal = nameLegal;
            AddTestWell(_well);

            var query = new Well { Uid = _well.Uid, NameLegal = string.Empty };
            var result = _devKit.Query<WellList, Well>(query);
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
            _well.CommonData = new CommonData() { PrivateGroupOnly = true };
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = _devKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
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
            _well.CommonData = new CommonData() { PrivateGroupOnly = false };
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = _devKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
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
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWell = response.SuppMsgOut;
            Assert.IsFalse(string.IsNullOrEmpty(uidWell));

            // Query all wells with default OptionsIn
            var query = new Well();
            var result = _devKit.Query<WellList, Well>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.IsNotNull(result);

            Assert.IsFalse(result.Any(x => x.CommonData?.PrivateGroupOnly ?? false));
            Assert.IsTrue(result.Any(x => uidWell.Equals(x.Uid)));
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well_And_Ignore_Invalid_Element()
        {
            var wellName = _devKit.Name("Bug-5855-AddToStore-Bad-Element");

            string xmlIn = string.Format(DevKit141Aspect.BasicAddWellXmlTemplate, null, wellName, "<fieldsssssss>Big Field</fieldsssssss>");

            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well_And_Ignore_Invalid_Attribute()
        {
            var wellName = _devKit.Name("Bug-5855-AddToStore-Bad-Attribute");

            string xmlIn = string.Format(DevKit141Aspect.BasicAddWellXmlTemplate, null, wellName, "<field abc=\"cde\">Big Field</field>");

            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = new Well { Uid = response.SuppMsgOut };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Big Field", result[0].Field);
        }

        [TestMethod]
        public void Well141DataAdapter_AddToStore_Can_Add_Well_With_Invalid_Child_Element()
        {
            var wellName = _devKit.Name("Bug-5855-AddToStore-Invalid-Child-Element");

            string xmlIn = string.Format(DevKit141Aspect.BasicAddWellXmlTemplate, null, wellName, "<field><abc>Big Field</abc></field>");

            var response = _devKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = new Well { Uid = response.SuppMsgOut };
            var result = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(wellName, result[0].Name);
            Assert.IsNull(result[0].Field);
        }

        private WMLS_AddToStoreResponse AddTestWell(Well well)
        {
            var response = _devKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            return response;
        }
    }
}
