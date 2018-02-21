//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.1
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

using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.ChangeLogs
{
    [TestClass]
    public class ChangeLog141DataAdapterAddTests : ChangeLog141TestBase
    {
        [TestMethod]
        public void ChangeLog141DataAdapter_AddToStore_Well()
        {
            var response = DevKit.AddAndAssert<WellList, Well>(Well);
            var uid = response.SuppMsgOut;
            var expectedHistoryCount = 1;
            var expectedChangeType = ChangeInfoType.add;

            var result = DevKit.GetAndAssert(new Well() { Uid = Well.Uid });

            AssertChangeLog(result, expectedHistoryCount, expectedChangeType);
        }

        [TestMethod]
        public void ChangeLog141DataAdapter_AddToStore_Log()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            DevKit.AddAndAssert(Log);

            var expectedHistoryCount = 1;
            var expectedChangeType = ChangeInfoType.add;

            var result = DevKit.GetAndAssert(new Log() { UidWell = Log.UidWell, UidWellbore = Log.UidWellbore, Uid = Log.Uid });

            AssertChangeLog(result, expectedHistoryCount, expectedChangeType);
        }
    }
}
