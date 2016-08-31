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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WbGeometry = Energistics.DataAccess.WITSML131.StandAloneWellboreGeometry;
using WbGeometryList = Energistics.DataAccess.WITSML131.WellboreGeometryList;

namespace PDS.Witsml.Server.Data.WbGeometries
{
    [TestClass]
    public partial class WbGeometry131ValidatorTests
    {
        public const string QueryMissingNamespace = "<wbGeometrys version=\"1.3.1.1\"><wbGeometry /></wbGeometrys>";
        public const string QueryInvalidNamespace = "<wbGeometrys xmlns=\"www.witsml.org/schemas/123\" version=\"1.3.1.1\"></wbGeometrys>";
        public const string QueryEmptyRoot = "<wbGeometrys xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"></wbGeometrys>";
        public const string QueryEmptyObject = "<wbGeometrys xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\"><wbGeometry /></wbGeometrys>";

		public Well Well { get; set; }
		public Wellbore Wellbore { get; set; }
        public WbGeometry WbGeometry { get; set; }
        public DevKit131Aspect DevKit { get; set; }
        public TestContext TestContext { get; set; }
        public List<WbGeometry> QueryEmptyList { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit131Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            Well = new Well
			{
				Uid = DevKit.Uid(),
				Name = DevKit.Name("Well"),
				TimeZone = DevKit.TimeZone
			};
            Wellbore = new Wellbore
			{
				Uid = DevKit.Uid(),
				Name = DevKit.Name("Wellbore"),
				UidWell = Well.Uid,
				NameWell = Well.Name
			};
			WbGeometry = new WbGeometry
			{
				Uid = DevKit.Uid(),
				Name = DevKit.Name("WbGeometry"),
				UidWell = Well.Uid,
				NameWell = Well.Name,
				UidWellbore = Wellbore.Uid,
				NameWellbore = Wellbore.Name
			};

            QueryEmptyList = DevKit.List(new WbGeometry());
			OnTestSetUp();
        }

        [TestCleanup]
        public void TestCleanup()
        {
			OnTestCleanUp();
            DevKit = null;
        }

		partial void OnTestSetUp();

		partial void OnTestCleanUp();

		#region Error -401

		public static readonly string QueryInvalidPluralRoot =
			"<wbGeometry xmlns=\"http://www.witsml.org/schemas/131\" version=\"1.3.1.1\">" + Environment.NewLine +
			"  <wbGeometry>" + Environment.NewLine +
			"    <name>Test Plural Root Element</name>" + Environment.NewLine +
			"  </wbGeometry>" + Environment.NewLine +
			"</wbGeometry>";

        [TestMethod]
        public void WbGeometry131Validator_GetFromStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.GetFromStore(ObjectTypes.WbGeometry, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod]
        public void WbGeometry131Validator_AddToStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.AddToStore(ObjectTypes.WbGeometry, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        [TestMethod]
        public void WbGeometry131Validator_UpdateInStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.WbGeometry, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        [TestMethod]
        public void WbGeometry131Validator_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.DeleteFromStore(ObjectTypes.WbGeometry, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

		#endregion Error -401

		#region Error -403

        [TestMethod]
        public void WbGeometry131Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.WbGeometry, QueryMissingNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WbGeometry131Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.WbGeometry, QueryInvalidNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void WbGeometry131Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.WbGeometry, QueryInvalidNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

		#endregion Error -403
	}
}