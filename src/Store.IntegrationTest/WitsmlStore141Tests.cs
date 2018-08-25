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
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store
{
    [TestClass]
    public class WitsmlStore141Tests
    {
        private DevKit141Aspect _devKit;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            WitsmlSettings.TruncateXmlOutDebugSize = DevKitAspect.DefaultXmlOutDebugSize;
        }

        [TestMethod]
        public void WitsmlStore_GetVersion_Can_Get_Version()
        {
            var request = new WMLS_GetVersionRequest();
            var response = _devKit.Store.WMLS_GetVersion(request);

            Assert.IsNotNull(response);
            if (!string.IsNullOrEmpty(response.Result))
            {
                var versions = response.Result.Split(',');
                Assert.IsNotNull(versions);
                Assert.IsTrue(versions.Length > 0);

                foreach (var version in versions)
                    Assert.IsFalse(string.IsNullOrEmpty(version));
            }
        }

        [TestMethod]
        public void WitsmlStore_GetVersion_Version_Ordered_Oldest_First()
        {
            var request = new WMLS_GetVersionRequest();
            var response = _devKit.Store.WMLS_GetVersion(request);

            Assert.IsNotNull(response);
            var ordered = true;
            if (!string.IsNullOrEmpty(response.Result))
            {
                var versions = response.Result.Split(',');
                Assert.IsNotNull(versions);
                Assert.IsTrue(versions.Length > 0);

                var version = versions[0];
                Assert.IsFalse(string.IsNullOrEmpty(version));

                for (var i = 1; i < versions.Length; i++)
                {
                    if (String.CompareOrdinal(version, versions[i]) >= 0)
                    {
                        ordered = false;
                        break;
                    }
                    version = versions[i];
                }
            }

            Assert.IsTrue(ordered);
        }

        [TestMethod]
        public void WitsmlStore_GetCap_Can_Get_Cap_Server()
        {
            var request = new WMLS_GetCapRequest { OptionsIn = "dataVersion=1.4.1.1" };
            var response = _devKit.Store.WMLS_GetCap(request);

            Assert.IsNotNull(response);
            Assert.IsFalse(string.IsNullOrEmpty(response.CapabilitiesOut));
        }

        [TestMethod]
        public void Witsml_Store_GetFromStore_Error_401_Missing_Plural_Root_Element()
        {
            var list = new LogList();
            var xmlIn = EnergisticsConverter.ObjectToXml(list).Replace("logs", "log");
            var response = _devKit.GetFromStore(ObjectTypes.Log, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlStore_AddToStore_Error_404_Invalid_Schema_Version()
        {
            var client = new CapClient { ApiVers = "1.4.1.1", SchemaVersion = "1.4.1.1,1.3.1.1" };
            var clients = new CapClients { Version = "1.4.1.1", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = "Well-to-add-invalid-schema-version" };
            var response = _devKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidClientSchemaVersion, response.Result);
        }

        [TestMethod]
        public void WitsmlStore_GetCap_Error_423_Unsupported_Data_Version()
        {
            var request = new WMLS_GetCapRequest { OptionsIn = "dataVersion=1.6.1.1" };
            var response = _devKit.Store.WMLS_GetCap(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataVersionNotSupported, response.Result);
        }

        [TestMethod]
        public void WitsmlStore_GetCap_Error_424_Missing_Data_Version()
        {
            var request = new WMLS_GetCapRequest();
            var response = _devKit.Store.WMLS_GetCap(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataVersion, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlStore_AddToStore_Error_465_Api_Version_Not_Match()
        {
            var client = new CapClient { ApiVers = "1.3.1.1", SchemaVersion = "1.3.1.1" };
            var clients = new CapClients { Version = "1.4.1.1", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = _devKit.Name("Well-to-add-apiVers-not-match"), TimeZone = _devKit.TimeZone };
            var response = _devKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ApiVersionNotMatch, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlStore_AddToStore_Error_467_Unsupported_Data_Schema_Version()
        {
            var client = new CapClient { ApiVers = "1.4.1.1"};
            var clients = new CapClients { Version = "1.4.x.y", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = _devKit.Name("Well-to-add-unsupported-schema-version"), TimeZone = _devKit.TimeZone };
            var response = _devKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ApiVersionNotSupported, response.Result);
        }

        [Ignore, Description("Not Implemented")]
        [TestMethod]
        public void WitsmlStore_AddToStore_Error_473_Schema_Version_Not_Match()
        {
            var client = new CapClient { ApiVers = "1.4.1.1", SchemaVersion = "1.3.1.1" };
            var clients = new CapClients { Version = "1.4.1.1", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = _devKit.Name("Well-to-add-schema-version-not-match"), TimeZone = _devKit.TimeZone };
            var response = _devKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.SchemaVersionNotMatch, response.Result);
        }

        [TestMethod]
        public void WitsmlStore_GetBaseMsg_Can_Return_Message()
        {
            var request = new WMLS_GetBaseMsgRequest {ReturnValueIn = (short) ErrorCodes.InputTemplateNonConforming };
            var response = _devKit.Store.WMLS_GetBaseMsg(request);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.AreEqual("The input template must conform to the appropriate derived schema.", response.Result);
        }

        [TestMethod]
        public void WitsmlStore_GetBaseMsg_Error_422_ReturnValueIn_Is_UnSet()
        {
            var request = new WMLS_GetBaseMsgRequest();
            var response = _devKit.Store.WMLS_GetBaseMsg(request);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Result);
            Assert.IsTrue(response.Result.Contains("-422"));
        }

        [TestMethod]
        public void WitsmlStore_GetBaseMsg_Can_Return_Null_On_Invalid_ReturnValueIn()
        {
            var request = new WMLS_GetBaseMsgRequest { ReturnValueIn = 12345 };
            var response = _devKit.Store.WMLS_GetBaseMsg(request);

            Assert.IsNotNull(response);
            Assert.IsNull(response.Result);
        }
    }
}
