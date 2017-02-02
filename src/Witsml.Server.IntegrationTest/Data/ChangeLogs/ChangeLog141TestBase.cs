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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.ChangeLogs
{
    public class ChangeLog141TestBase : MultiObject141TestBase
    {
        protected void AssertChangeLog(string uid, CommonData commonData, int expectedHistoryCount, ChangeInfoType expectedChangeType)
        {
            // Assert that the entity has CommonData with a DateTimeLastChange
            Assert.IsNotNull(commonData);
            Assert.IsTrue(commonData.DateTimeLastChange.HasValue);

            // Fetch the changeLog for the entity just added
            var changeLogQuery = new ChangeLog() { UidObject = uid, ObjectType = ObjectTypes.Well };
            var changeLog = DevKit.QueryAndAssert<ChangeLogList, ChangeLog>(changeLogQuery);

            // TODO: Fetch the changeHistory using the entity's DateTimeLastChange
            // TODO: Do this when the common data interface is added.
            // TODO: var changeHistory = changeLog.ChangeHistory.FirstOrDefault(c => c.DateTimeChange == commonData.DateTimeLastChange);
            var changeHistory = changeLog.ChangeHistory.LastOrDefault();

            // Verify that we found a changeHistory for the latest change.
            Assert.IsNotNull(changeHistory);

            // The SourceName of the change log MUST match the Source name of the entity
            Assert.AreEqual(commonData.SourceName, changeLog.SourceName);

            // Verify that the LastChangeType exists and was an Add
            Assert.IsTrue(changeLog.LastChangeType.HasValue);
            Assert.AreEqual(expectedChangeType, changeLog.LastChangeType.Value);

            // Verify that there is only one changeHistory
            Assert.AreEqual(expectedHistoryCount, changeLog.ChangeHistory.Count);

            // The LastChangeType of the changeLog MUST match the ChangeType of the last changeHistory added
            Assert.AreEqual(changeLog.LastChangeType, changeHistory.ChangeType);

            // The LastChangeInfo of the changeLog MUST match the ChangeInfo of the last changeHistory added
            Assert.AreEqual(changeLog.LastChangeInfo, changeHistory.ChangeInfo);

            // If the entity was deleted then we don't have a DateTimeLastChange to compare
            if (expectedChangeType != ChangeInfoType.delete)
            {
                // Verify that the changeHistory has a DateTimeLastChange and it matches
                //... the entity DateTimeLastChange
                Assert.IsTrue(changeHistory.DateTimeChange.HasValue);
                // TODO: Waiting for fix in DbAuditHistoryDataAdapter that requires CommonData interface
                // TODO: Assert.AreEqual(commonData.DateTimeLastChange.Value, changeHistory.DateTimeChange.Value);
            }
        }
    }
}
