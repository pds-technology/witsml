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
            GetWell(_well);

            // Delete well
            var delete = new Well {Uid = _well.Uid};
            DeleteWell(delete);

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
            GetWell(_well);

            // Delete well
            var delete = new Well { Uid = "W" + uid };
            DeleteWell(delete);

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
            var result = GetWell(_well);
            Assert.AreEqual(_well.Country, result.Country);
            Assert.AreEqual(_well.DateTimeSpud, result.DateTimeSpud);

            // Partial delete well
            const string delete = "<country /><dTimSpud />";
            var queryIn = string.Format(DevKit141Aspect.BasicDeleteWellXmlTemplate, _well.Uid, delete);
            var response = _devKit.DeleteFromStore(ObjectTypes.Well, queryIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Assert the well elements has been deleted
            result = GetWell(_well);
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
            var result = GetWell(_well);
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
            result = GetWell(_well);
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
            var result = GetWell(_well);
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
            result = GetWell(_well);
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
            var result = GetWell(_well);
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
            result = GetWell(_well);
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
            var result = GetWell(_well);
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
            result = GetWell(_well);
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

        private void DeleteWell(Well well)
        {
            var response = _devKit.Delete<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Well GetWell(Well well)
        {
            var query = new Well { Uid = well.Uid };

            var results = _devKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }
    }
}
