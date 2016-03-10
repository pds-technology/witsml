using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace PDS.Witsml.Server.Configuration
{
    [TestClass]
    public class WitsmlValidator141Tests
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
        public void WitsmlValidator_GetFromStore_Error_440_Option_Keyword_Not_Supported()
        {
            string queryIn = "<wells  xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, "optionNotExists=BadValue");

            Assert.AreEqual((short)ErrorCodes.KeywordNotSupportedByFunction, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_441_Invalid_Keyword_Value()
        {
            string queryIn = "<wells  xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, "returnElements=BadValue");

            Assert.AreEqual((short)ErrorCodes.InvalidKeywordValue, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_HeaderOnly_For_Growing_Object()
        {
            var query = new Well { Uid = "", Name = "" };
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_For_ChangeLog()
        {
            var query = new Well { Uid = "", Name = "" };
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);

            Assert.AreEqual((short)ErrorCodes.InvalidOptionForChangeLogOnly, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_427_RequestObjectSelectionCapability_True_More_Than_One_Keyword()
        {
            var query = new Well { Uid = "", Name = "" };
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, optionsIn: OptionsIn.RequestObjectSelectionCapability.True + ";" + OptionsIn.ReturnElements.All);

            Assert.AreEqual((short)ErrorCodes.InvalidOptionsInCombination, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_RequestObjectSelectionCapability_True_Minimum_Query_Template()
        {
            var query = new Well { Uid = "", Name = "" };
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_Minimum_Query_Template()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            string queryIn = "<wells version = \"1.4.1.1\" >" + Environment.NewLine +
                            "    <well/>" + Environment.NewLine +
                            "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, queryIn, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            string badQuery = "<wells xmlns=\"www.witsml.org/schemas/131\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            string badQuery = "<wells xmlns=\"www.witsml.org/schemas/131\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);

            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_RequestObjectSelectionCapability_None_Minimum_Query_Template()
        {
            var well = new Well { Uid = "", Name = "" };
            var response=  DevKit.Get<WellList, Well>(DevKit.List(well), ObjectTypes.Well, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_409_RequestObjectSelectionCapability_None_Minimum_Query_Template()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);

            Assert.AreEqual((short)ErrorCodes.InputTemplateNonConforming, response.Result);
        }
    }
}
