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

using System;
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.Wellbores
{
    /// <summary>
    /// Wellbore141DataAdapter Add tests.
    /// </summary>
    [TestClass]
    public partial class Wellbore141DataAdapterAddTests : Wellbore141TestBase
    {
        [TestMethod]
        public void Wellbore141DataAdapter_AddToStore_Can_AddWellbore()
        {
            AddParents();
            DevKit.AddAndAssert(Wellbore);
        }

        /// <summary>
        /// Test adding a <see cref="Wellbore"/> successfully with dTimKickoff specified
        /// </summary>
        [TestMethod]
        public void Wellbore141DataAdapter_AddToStore_Can_AddWellbore_with_dTimKickoff()
        {
            Wellbore.DateTimeKickoff = DateTimeOffset.Now;

            AddParents();
            DevKit.AddAndAssert(Wellbore);
        }

        [TestMethod]
        public void Wellbore141DataAdapter_AddToStore_Can_AddWellbore_With_Same_Uid_Under_DifferentWell()
        {
            AddParents();
            DevKit.AddAndAssert(Wellbore);

            var well2 = new Well { Uid = DevKit.Uid(), Name = DevKit.Name("Well-to-add-02"), TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var wellbore2 = new Wellbore()
            {
                Uid = well2.Uid,
                UidWell = response.SuppMsgOut,
                NameWell = well2.Name,
                Name = DevKit.Name("Wellbore 02-01")
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
