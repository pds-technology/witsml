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


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    [TestClass]
    public partial class Wellbore200DataAdapterAddTests : Wellbore200TestBase
    {
        [TestMethod]
        public void Wellbore200DataAdapter_Can_Add_And_Get_Wellbore()
        {
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            var addedWellbore = DevKit.GetAndAssert(Wellbore);
            Assert.AreEqual(Wellbore.Citation.Title, addedWellbore.Citation.Title);
        }
    }
}
