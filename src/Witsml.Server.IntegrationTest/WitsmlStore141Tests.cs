using System.Linq;
using System.Xml;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Server.Data;
using PDS.Witsml.Server.Data.Wellbores;
using System;

namespace PDS.Witsml.Server
{
    [TestClass]
    public class WitsmlStore141Tests
    {
        private DevKit141Aspect DevKit;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestMethod]
        public void Can_get_version()
        {
            var request = new WMLS_GetVersionRequest();
            var response = DevKit.Store.WMLS_GetVersion(request);

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
        public void Version_order_oldest_first()
        {
            var request = new WMLS_GetVersionRequest();
            var response = DevKit.Store.WMLS_GetVersion(request);

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
                    if (string.Compare(version, versions[i]) >= 0)
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
        public void Can_get_cap_server()
        {
            var request = new WMLS_GetCapRequest { OptionsIn = "dataVersion=1.4.1.1" };
            var response = DevKit.Store.WMLS_GetCap(request);

            Assert.IsNotNull(response);
            Assert.IsFalse(string.IsNullOrEmpty(response.CapabilitiesOut));
        }
        
        [TestMethod]
        public void Query_OptionsIn_requestObjectSelectionCapability()
        {
            Well well = new Well();
            var result = DevKit.Query<WellList, Well>(well, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            well = result.FirstOrDefault();
            Assert.IsNotNull(well);
            Assert.AreEqual("abc", well.Uid);
            Assert.IsNotNull(well.StatusWell);
            Assert.IsTrue(well.WellLocation.Count == 1);
            Assert.AreEqual(1, well.PercentInterest.Value);
            Assert.IsNotNull(well.CommonData.DateTimeLastChange);
        }

        [TestMethod]
        public void Query_OptionsIn_privateGroupOnly()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            CommonData commonData = new CommonData();
            commonData.PrivateGroupOnly = true;
            well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone, CommonData=commonData };
            response = DevKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uid = response.SuppMsgOut;
            var valid = !string.IsNullOrEmpty(uid);
            Assert.IsTrue(valid);

            well = new Well();
            var result = DevKit.Query<WellList, Well>(well, optionsIn: OptionsIn.ReturnElements.All + ";" + OptionsIn.RequestPrivateGroupOnly.True);
            Assert.IsNotNull(result);

            var notPrivateGroupWells = result.Where(x =>
                {
                    bool isPrivate = x.CommonData.PrivateGroupOnly ?? false;
                    return !isPrivate;
                });
            Assert.IsFalse(notPrivateGroupWells.Any());

            well = result.Where(x => uid.Equals(x.Uid)).FirstOrDefault();
            Assert.IsNotNull(well);
        }

        [TestMethod]
        public void Query_OptionsIn_ReturnElements_IdOnly()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone, NameLegal = "Company Legal Name", Field = "Big Field" };
            var response = DevKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var queryWell = new Well { Name = "Well-to-add-01" };
            var result = DevKit.Get<WellList, Well>(DevKit.List(queryWell), optionsIn: OptionsIn.ReturnElements.IdOnly);
            Assert.IsNotNull(result);

            var xmlout = result.XMLout;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlout);
            XmlElement wells = doc.DocumentElement;

            bool uidExists = false;
            foreach (XmlNode node in wells.ChildNodes)
            {
                uidExists = true;
                Assert.IsTrue(node.Attributes.Count == 1);
                Assert.IsTrue(node.HasChildNodes);
                Assert.AreEqual(1, node.ChildNodes.Count);
                Assert.AreEqual("name", node.ChildNodes[0].Name);
            }
            Assert.IsTrue(uidExists);
        }

        [TestMethod]
        public void Query_OptionsIn_ReturnElements_Requested()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone, NameLegal = "Company Legal Name", Field = "Big Field" };
            var response = DevKit.Add<WellList, Well>(well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            string uid = response.SuppMsgOut;

            var queryWell = new Well { Uid = uid, Name = "Well-to-add-01", NameLegal = "", Field = "" };
            var result = DevKit.Get<WellList, Well>(DevKit.List(queryWell), optionsIn: OptionsIn.ReturnElements.Requested);
            Assert.IsNotNull(result);

            var xmlout = result.XMLout;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlout);
            XmlElement wells= doc.DocumentElement;

            bool uidExists = false;
            foreach (XmlNode node in wells.ChildNodes)
            {
                Assert.AreEqual(1, node.Attributes.Count);
                Assert.IsTrue(node.HasChildNodes);
                Assert.IsTrue(node.ChildNodes.Count <= 3);
                Assert.AreEqual("name", node.ChildNodes[0].Name);
                Assert.AreEqual("Well-to-add-01", node.ChildNodes[0].InnerText);
                if (uid.Equals(node.Attributes[0].InnerText))
                {
                    uidExists = true;
                    Assert.AreEqual("nameLegal", node.ChildNodes[1].Name);
                    Assert.AreEqual("Company Legal Name", node.ChildNodes[1].InnerText);
                    Assert.AreEqual("field", node.ChildNodes[2].Name);
                    Assert.AreEqual("Big Field", node.ChildNodes[2].InnerText);
                }
            }
            Assert.IsTrue(uidExists);
        }

        [TestMethod]
        public void Can_add_wellbore_without_validation()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore = new Wellbore { Name = "Wellbore-to-add-01", NameWell = well.Name, UidWell = response.SuppMsgOut };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Adding_wellbore_database_configuration_error()
        {
            var well = new Well { Name = "Well-to-add-02", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var dbProvider = new DatabaseProvider(new MongoDbClassMapper(), string.Empty);
            var wellboreAdapter = new Wellbore141DataAdapter(dbProvider);
            wellboreAdapter.Container = ContainerFactory.Create();

            var caught = false;
            WitsmlException exception = null;

            try
            {
                var wellbore = new Wellbore { Name = "Wellbore-to-test-add-error", NameWell = well.Name, UidWell = response.SuppMsgOut };
                wellboreAdapter.Add(wellbore);
            }
            catch (WitsmlException ex)
            {
                caught = true;
                exception = ex;
            }

            Assert.IsTrue(caught);
            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorCodes.ErrorAddingToDataStore, exception.ErrorCode);
        }

        [TestMethod]
        public void Test_error_code_401_missing_plural_root_element_xmlIn()
        {
            var list = new WellList();
            var xmlIn = EnergisticsConverter.ObjectToXml(list).Replace("wells", "well");
            var response = DevKit.GetFromStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [Ignore]
        [TestMethod]
        public void Test_error_code_404_invalid_schema_version()
        {
            var client = new CapClient { ApiVers = "1.4.1.1", SchemaVersion = "1.4.1.1,1.3.1.1" };
            var clients = new CapClients { Version = "1.4.1.1", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = "Well-to-add-invalid-schema-version" };
            var response = DevKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidClientSchemaVersion, response.Result);
        }

        [TestMethod]
        public void Test_error_code_413_unsupported_data_object()
        {
            var well = new Well { Name = "Well-to-add-unsupported-error" };
            var wells = new WellList { Well = DevKit.List(well) };

            // update Version property to an unsupported data schema version
            wells.Version = "1.4.x.y";

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotSupported, response.Result);
        }

        [TestMethod]
        public void Test_error_code_423_unsupported_data_version()
        {
            var request = new WMLS_GetCapRequest { OptionsIn = "dataVersion=1.6.1.1" };
            var response = DevKit.Store.WMLS_GetCap(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataVersionNotSupported, response.Result);
        }

        [TestMethod]
        public void Test_error_code_424_data_version_not_supplies()
        {
            var request = new WMLS_GetCapRequest();
            var response = DevKit.Store.WMLS_GetCap(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataVersion, response.Result);
        }

        [Ignore]
        [TestMethod]
        public void Test_error_code_465_api_version_not_match()
        {
            var client = new CapClient { ApiVers = "1.3.1.1", SchemaVersion = "1.3.1.1" };
            var clients = new CapClients { Version = "1.4.1.1", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = "Well-to-add-apiVers-not-match", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ApiVersionNotMatch, response.Result);
        }

        [Ignore]
        [TestMethod]
        public void Test_error_code_466_non_conforming_capabilities_in()
        {
            var well = new Well { Name = "Well-to-add-invalid-capabilitiesIn", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, ObjectTypes.Well, "<capClients />");

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.CapabilitiesInNonConforming, response.Result);
        }

        [Ignore]
        [TestMethod]
        public void Test_error_code_467_unsupported_data_schema_version()
        {
            var client = new CapClient { ApiVers = "1.4.1.1"};
            var clients = new CapClients { Version = "1.4.x.y", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = "Well-to-add-unsupported-schema-version", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ApiVersionNotSupported, response.Result);
        }

        [TestMethod]
        public void Test_error_code_468_missing_version_attribute()
        {
            var well = new Well { Name = "Well-to-add-missing-version-attribute" };
            var wells = new WellList { Well = DevKit.List(well) };

            // update Version property to an unsupported data schema version
            wells.Version = null;

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var response = DevKit.AddToStore(ObjectTypes.Well, xmlIn, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingDataSchemaVersion, response.Result);
        }

        [Ignore]
        [TestMethod]
        public void Test_error_code_473_schema_version_not_match()
        {
            var client = new CapClient { ApiVers = "1.4.1.1", SchemaVersion = "1.3.1.1" };
            var clients = new CapClients { Version = "1.4.1.1", CapClient = client };
            var capabilitiesIn = EnergisticsConverter.ObjectToXml(clients);
            var well = new Well { Name = "Well-to-add-schema-version-not-match", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well, capClient: capabilitiesIn);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.SchemaVersionNotMatch, response.Result);
        }
    }
}
