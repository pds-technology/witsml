using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;

namespace PDS.Witsml.Server
{
    [TestClass]
    public class WitsmlStore141Tests
    {
        private static readonly DevKit141Aspect DevKit = new DevKit141Aspect(null);
        private WitsmlStore _store;

        [TestInitialize]
        public void TestSetUp()
        {
            _store = new WitsmlStore();
            _store.Container = ContainerFactory.Create();
        }

        [TestMethod]
        public void Can_add_well_without_validation()
        {
            var well = new Well { Name = "Well-to-add-01" };
            var wells = new WellList { Well = DevKit.List(well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = ObjectTypes.Well, XMLin = xmlIn };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Adding_duplicate_well_uid_causes_database_error()
        {
            var well = new Well { Name = "Well-to-test-add-error", Uid = DevKit.Uid() };
            var wells = new WellList { Well = DevKit.List(well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = ObjectTypes.Well, XMLin = xmlIn };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.ErrorAddingToDataStore, response.Result);
        }

        [TestMethod]
        public void Using_invalid_version_causes_unsupported_error()
        {
            var well = new Well { Name = "Well-to-test-unsupported-error" };
            var wells = new WellList { Well = DevKit.List(well) };

            // update Version property to an unsupported data schema version
            wells.Version = "1.4.x.y";

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = ObjectTypes.Well, XMLin = xmlIn };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectNotSupported, response.Result);
        }

        [TestMethod]
        public void Using_invalid_object_type_causes_data_types_dont_match_error()
        {
            var well = new Well { Name = "Well-to-test-no-match-error" };
            var wells = new WellList { Well = DevKit.List(well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = ObjectTypes.Wellbore, XMLin = xmlIn };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypesDontMatch, response.Result);
        }

        [TestMethod]
        public void Passing_null_object_type_causes_missing_type_error()
        {
            var well = new Well { Name = "Well-to-test-missing-type-error" };
            var wells = new WellList { Well = DevKit.List(well) };

            var xmlIn = EnergisticsConverter.ObjectToXml(wells);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = null, XMLin = xmlIn };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWMLtypeIn, response.Result);
        }

        [TestMethod]
        public void Passing_null_xml_causes_invalid_input_template_error()
        {
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = ObjectTypes.Well, XMLin = null };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }

        [TestMethod]
        public void Unknown_object_causes_data_type_not_supported_error()
        {
            var entity = new Target { Name = "Entity-to-test-unsupported-error" };
            var list = new TargetList { Target = DevKit.List(entity) };

            var xmlIn = EnergisticsConverter.ObjectToXml(list);
            var request = new WMLS_AddToStoreRequest { WMLtypeIn = "target", XMLin = xmlIn };
            var response = _store.WMLS_AddToStore(request);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.DataObjectTypeNotSupported, response.Result);
        }
    }
}
