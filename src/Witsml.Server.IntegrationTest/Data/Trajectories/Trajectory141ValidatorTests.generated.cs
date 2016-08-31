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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Trajectories
{
    [TestClass]
    public partial class Trajectory141ValidatorTests
    {
        public const string QueryMissingNamespace = "<trajectorys version=\"1.4.1.1\"><trajectory /></trajectorys>";
        public const string QueryInvalidNamespace = "<trajectorys xmlns=\"www.witsml.org/schemas/123\" version=\"1.4.1.1\"></trajectorys>";
        public const string QueryEmptyRoot = "<trajectorys xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\"></trajectorys>";
        public const string QueryEmptyObject = "<trajectorys xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\"><trajectory /></trajectorys>";

		public Well Well { get; set; }
		public Wellbore Wellbore { get; set; }
        public Trajectory Trajectory { get; set; }
        public DevKit141Aspect DevKit { get; set; }
        public TestContext TestContext { get; set; }
        public List<Trajectory> QueryEmptyList { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
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
			Trajectory = new Trajectory
			{
				Uid = DevKit.Uid(),
				Name = DevKit.Name("Trajectory"),
				UidWell = Well.Uid,
				NameWell = Well.Name,
				UidWellbore = Wellbore.Uid,
				NameWellbore = Wellbore.Name
			};

            QueryEmptyList = DevKit.List(new Trajectory());
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
			"<trajectory xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" + Environment.NewLine +
			"  <trajectory>" + Environment.NewLine +
			"    <name>Test Plural Root Element</name>" + Environment.NewLine +
			"  </trajectory>" + Environment.NewLine +
			"</trajectory>";

        [TestMethod]
        public void Trajectory141Validator_GetFromStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Trajectory, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_AddToStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.AddToStore(ObjectTypes.Trajectory, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_UpdateInStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.UpdateInStore(ObjectTypes.Trajectory, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_DeleteFromStore_Error_401_No_Plural_Root_Element()
        {
            var response = DevKit.DeleteFromStore(ObjectTypes.Trajectory, QueryInvalidPluralRoot, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingPluralRootElement, response?.Result);
        }

		#endregion Error -401

		#region Error -403

        [TestMethod]
        public void Trajectory141Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_MissingNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Trajectory, QueryMissingNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_True_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Trajectory, QueryInvalidNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

        [TestMethod]
        public void Trajectory141Validator_GetFromStore_Error_403_RequestObjectSelectionCapability_None_BadNamespace()
        {
            var response = DevKit.GetFromStore(ObjectTypes.Trajectory, QueryInvalidNamespace, null, optionsIn: OptionsIn.RequestObjectSelectionCapability.None);
            Assert.AreEqual((short)ErrorCodes.MissingDefaultWitsmlNamespace, response.Result);
        }

		#endregion Error -403
	}
}