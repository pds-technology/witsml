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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Get tests.
    /// </summary>
    [TestClass]
    public class Wellbore141DataAdapterGetTests
    {
        private DevKit141Aspect DevKit;
        private Well _well;
        private Wellbore _wellbore;
        private Wellbore _wellboreQuery;
        private Wellbore _wellboreQueryUid;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well() { Name = DevKit.Name("Well for Wellbore Get Test"), TimeZone = DevKit.TimeZone };
            _wellbore = new Wellbore() { Name = DevKit.Name("Wellbore Get Test"), NameWell = _well.Name, Number = "123", NumGovt = "Gov 123" };

            // Add a well
            var response = DevKit.Add<WellList, Well>(_well);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellbore.UidWell = response.SuppMsgOut;

            // Add a wellbore to query later
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            _wellboreQuery = new Wellbore()
            {
                Uid = response.SuppMsgOut,
                Name = string.Empty,
                Number = string.Empty
            };

            _wellboreQueryUid = new Wellbore() {Uid = response.SuppMsgOut};
        }

        [TestCleanup]
        public void TestCleanup()
        {
            DevKit = null;
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_All()
        {
            var wellbore = GetWellboreQueryWithOptionsIn(_wellboreQueryUid, OptionsIn.ReturnElements.All);
            Assert.AreEqual(_wellboreQuery.Uid, wellbore.Uid);
            Assert.IsNotNull(wellbore.Number);
            Assert.AreEqual(_wellbore.Number, wellbore.Number);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_IdOnly()
        {
            var wellbore = GetWellboreQueryWithOptionsIn(_wellboreQueryUid, OptionsIn.ReturnElements.IdOnly);
            Assert.AreEqual(_wellboreQuery.Uid, wellbore.Uid);
            Assert.AreEqual(_wellbore.Name, wellbore.Name);
            Assert.IsNull(wellbore.Number); // Will not exist in an IdOnly query
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_Requested()
        {
            var wellbore = GetWellboreQueryWithOptionsIn(_wellboreQuery, OptionsIn.ReturnElements.Requested);
            Assert.AreEqual(_wellboreQuery.Uid, wellbore.Uid);
            Assert.AreEqual(_wellbore.Name, wellbore.Name);
            Assert.AreEqual(_wellbore.Number, wellbore.Number);
            Assert.IsNull(wellbore.NumGovt); // Will not exist because it was not requested in the query
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_RequestObjectSelection()
        {
            _wellboreQueryUid.Uid = null;
            var result = DevKit.Query<WellboreList, Wellbore>(_wellboreQueryUid, ObjectTypes.Wellbore, null, optionsIn: OptionsIn.ReturnElements.RequestObjectSelectionCapability.True);
            Assert.AreEqual(1, result.Count);

            var returnWell = result.FirstOrDefault();
            Assert.IsNotNull(returnWell);
            Assert.IsNotNull(returnWell.PurposeWellbore);  // We'd only see this for Request Object Selection Cap
        }

        private Wellbore GetWellboreQueryWithOptionsIn(Wellbore query, string optionsIn)
        {
            var result = DevKit.Query<WellboreList, Wellbore>(query, ObjectTypes.Wellbore, null, optionsIn: optionsIn);
            Assert.AreEqual(1, result.Count);

            var returnWellbore = result.FirstOrDefault();
            Assert.IsNotNull(returnWellbore);
            return returnWellbore;
        }
    }
}
