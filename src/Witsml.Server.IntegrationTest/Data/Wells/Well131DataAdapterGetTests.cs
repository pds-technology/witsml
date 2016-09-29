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
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Wells
{
    /// <summary>
    /// Well131DataAdapterGetTests
    /// </summary>
    public partial class Well131DataAdapterGetTests
    {
        [TestMethod]
        public void Well131DataAdapter_GetFromStore_Selection_Not_Equal_Comparison_dTimLastChange()
        {
            var well01 = DevKit.CreateFullWell();
            var response = DevKit.Add<WellList, Well>(well01);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid01 = response.SuppMsgOut;

            var query = new Well { Uid = uid01 };
            var result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(uid01, result[0].Uid);

            var wellLastChangeTime = result[0].CommonData.DateTimeLastChange;

            var well02 = DevKit.CreateFullWell();
            well02.CommonData.DateTimeCreation = DateTimeOffset.UtcNow;
            response = DevKit.Add<WellList, Well>(well02);

            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            var uid02 = response.SuppMsgOut;

            query = new Well { CommonData = new CommonData() };
            query.CommonData.DateTimeLastChange = wellLastChangeTime;
            result = DevKit.Query<WellList, Well>(query, ObjectTypes.Well, null, optionsIn: OptionsIn.ReturnElements.All);

            // Section 6.6.4
            Assert.IsTrue(result.Any(x => x.Uid == uid02));
            Assert.IsFalse(result.Any(x => x.Uid == uid01));
        }
    }
}