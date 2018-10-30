//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Get tests.
    /// </summary>
    [TestClass]
    public partial class Wellbore141DataAdapterGetTests : Wellbore141TestBase
    {
        private Wellbore _wellboreQuery;
        private Wellbore _wellboreQueryUid;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            Wellbore.Number = "123";
            Wellbore.NumGovt = "Gov 123";

            _wellboreQuery = new Wellbore()
            {
                Uid = Wellbore.Uid,
                Name = string.Empty,
                Number = string.Empty
            };

            _wellboreQueryUid = new Wellbore()
            {
                Uid = Wellbore.Uid
            };
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_All()
        {
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            var wellbore = DevKit.GetAndAssert(_wellboreQueryUid, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(_wellboreQuery.Uid, wellbore.Uid);
            Assert.IsNotNull(wellbore.Number);
            Assert.AreEqual(Wellbore.Number, wellbore.Number);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_IdOnly()
        {
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            var wellbore = DevKit.GetAndAssert(_wellboreQueryUid, optionsIn: OptionsIn.ReturnElements.IdOnly);
            Assert.AreEqual(_wellboreQuery.Uid, wellbore.Uid);
            Assert.AreEqual(Wellbore.Name, wellbore.Name);
            Assert.IsNull(wellbore.Number); // Will not exist in an IdOnly query
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_Requested()
        {
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            var wellbore = DevKit.GetAndAssert(_wellboreQuery, optionsIn: OptionsIn.ReturnElements.Requested, queryByExample: true);
            Assert.AreEqual(_wellboreQuery.Uid, wellbore.Uid);
            Assert.AreEqual(Wellbore.Name, wellbore.Name);
            Assert.AreEqual(Wellbore.Number, wellbore.Number);
            Assert.IsNull(wellbore.NumGovt); // Will not exist because it was not requested in the query
        }

        [TestMethod]
        public void Wellbore141DataAdapter_GetFromStore_RequestObjectSelection()
        {
            _wellboreQueryUid.Uid = null;
            var wellbore = DevKit.GetAndAssert(_wellboreQueryUid, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);
            Assert.IsNotNull(wellbore.PurposeWellbore);  // We'd only see this for Request Object Selection Cap
        }
    }
}
