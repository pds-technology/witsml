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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace PDS.Witsml.Server.Configuration
{
    [TestClass]
    public class WitsmlValidator141Tests
    {
        private DevKit141Aspect DevKit;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

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
        public void WitsmlValidator_GetFromStore_ReturnElement_HeaderOnly_For_Growing_Object()
        {
            var query = new Log { Uid = "", Name = "" };
            var response = DevKit.Get<LogList, Log>(DevKit.List(query), ObjectTypes.Log, optionsIn: OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_HeaderOnly_Not_Growing_Object()
        {
            var query = new Well { Uid = "", Name = "" };
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void WitsmlValidator_GetFromStore_ReturnElement_StationLocationOnly_For_Trajectory()
        {
            var query = new Trajectory { Uid = "", Name = "" };
            var response = DevKit.Get<TrajectoryList, Trajectory>(DevKit.List(query), ObjectTypes.Trajectory, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_425_ReturnElement_StationLocationOnly_Not_Trajectory()
        {
            var query = new Well { Uid = "", Name = "" };
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, optionsIn: OptionsIn.ReturnElements.StationLocationOnly);

            Assert.AreEqual((short)ErrorCodes.InvalidOptionForGrowingObjectOnly, response.Result);
        }

        [TestMethod]
        [Ignore, Description("Not Implemented")]
        public void WitsmlValidator_GetFromStore_ReturnElement_LatestChangeOnly_For_ChangeLog()
        {
            var query = new ChangeLog { Uid = "", NameWell = ""};
            var response = DevKit.Get<ChangeLogList, ChangeLog>(DevKit.List(query), ObjectTypes.ChangeLog, optionsIn: OptionsIn.ReturnElements.LatestChangeOnly);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_476_ReturnElement_LatestChangeOnly_Not_ChangeLog()
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
            var query = new Well {};
            var response = DevKit.Get<WellList, Well>(DevKit.List(query), ObjectTypes.Well, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_MultiChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well/>" + Environment.NewLine +
                              "   <well/>" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_Has_Attribute()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well uid=\"PDS Test Wells\" />" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_BadChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <log/>" + Environment.NewLine +
                              "</wells>";

            var response = DevKit.GetFromStore(ObjectTypes.Well, badQuery, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual((short)ErrorCodes.InvalidMinimumQueryTemplate, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_428_RequestObjectSelectionCapability_True_With_Bad_Minimum_Query_Template_NonEmptyChild()
        {
            string badQuery = "<wells xmlns=\"http://www.witsml.org/schemas/1series\" version = \"1.4.1.1\" >" + Environment.NewLine +
                              "   <well>" + Environment.NewLine +
                              "       <name>PDS Test Wells</name>" + Environment.NewLine +
                              "   </well>" + Environment.NewLine +
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

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_407_Missing_Witsml_Object_Type()
        {
            var well = new Well { Name = "Well-to-query-missing-witsml-type", TimeZone = DevKit.TimeZone };
            var response = DevKit.Get<WellList, Well>(DevKit.List(well), string.Empty);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingWMLtypeIn, response.Result);
        }

        [TestMethod]
        public void WitsmlValidator_GetFromStore_Error_408_Missing_Input_Template()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Well, null, null, null);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingInputTemplate, response.Result);
        }
    }
}
