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
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.ChangeLogs
{
    [TestClass]
    public class ChangeLog141GetTests : MultiObject141TestBase
    {
        public const string QueryEmptyRootTemplate = @"<changeLogs xmlns=""http://www.witsml.org/schemas/1series"" version=""1.4.1.1"">{0}</changeLogs>";       

        [TestMethod]
        public void ChangeLog141GetTests_GetFromStore_Can_Get_ChangeLogs_Without_ChangeHistory_With_Latest_Change_Only_OptionsIn()
        {
            AddParents();

            DevKit.AddAndAssert(Log);
            DevKit.AddAndAssert(Trajectory);

            var queryIn = string.Format(QueryEmptyRootTemplate, "<changeLog />");
            var changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.LatestChangeOnly);

            Assert.IsTrue(changeLogs.Count >= 4);
            changeLogs.ForEach(c => Assert.AreEqual(0, c.ChangeHistory.Count));
        }

        [TestMethod]
        public void ChangeLog141GetTests_GetFromStore_Can_Get_ChangeLogs_With_DateTimeLastChange_Filter()
        {
            var dateFilter = "<changeLog><commonData><dTimLastChange>{0}</dTimLastChange></commonData></changeLog>";

            AddParents();
            var wellbore = DevKit.GetAndAssert<WellboreList, Wellbore>(Wellbore);
            var dateTimeLastChangeAfterWellbore = wellbore.CommonData.DateTimeLastChange;

            DevKit.AddAndAssert(Log);
            DevKit.AddAndAssert(Trajectory);

            var trajectory = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory);
            var datetimeLastChangeAfterTrajectory = trajectory.CommonData.DateTimeLastChange;

            var queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateFilter, datetimeLastChangeAfterTrajectory));
            GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.LatestChangeOnly, 0);

            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateFilter, dateTimeLastChangeAfterWellbore));
            var changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.LatestChangeOnly, 2);

            // No change history returned
            changeLogs.ForEach((c) => Assert.AreEqual(0, c.ChangeHistory.Count));

            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateFilter, datetimeLastChangeAfterTrajectory));
            GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 0);

            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateFilter, dateTimeLastChangeAfterWellbore));
            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 2);

            // Change history returned
            changeLogs.ForEach((c) => Assert.AreEqual(1, c.ChangeHistory.Count));
        }

        [TestMethod]
        public void ChangeLog141GetTests_GetFromStore_Can_Get_ChangeLogs_With_DateTimeLastChange_And_ObjectType_Filters()
        {
            var dateAndObjectTypeFilter = "<changeLog><objectType>{0}</objectType><commonData><dTimLastChange>{1}</dTimLastChange></commonData></changeLog>";

            AddParents();
            var wellbore = DevKit.GetAndAssert<WellboreList, Wellbore>(Wellbore);
            var dateTimeLastChangeAfterWellbore = wellbore.CommonData.DateTimeLastChange;

            DevKit.AddAndAssert(Log);

            var queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateAndObjectTypeFilter, ObjectTypes.Trajectory, dateTimeLastChangeAfterWellbore));
            GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 0);

            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateAndObjectTypeFilter, ObjectTypes.Log, dateTimeLastChangeAfterWellbore));
            GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);

            DevKit.AddAndAssert(Trajectory);

            var trajectory = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory);
            var datetimeLastChangeAfterTrajectory = trajectory.CommonData.DateTimeLastChange;

            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateAndObjectTypeFilter, ObjectTypes.Trajectory, dateTimeLastChangeAfterWellbore));
            var changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);

            Assert.IsTrue(changeLogs.Count == 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory.Count == 1);
            Assert.AreEqual(ObjectTypes.Trajectory, changeLogs.First().ObjectType);

            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(dateAndObjectTypeFilter, ObjectTypes.Trajectory, datetimeLastChangeAfterTrajectory));
            GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 0);
        }

        [TestMethod]
        public void ChangeLog141GetTests_GetFromStore_Query_ChangeLogs_With_Recurring_Filter()
        {
            DevKit.AddAndAssert(Well);

            Well.Name = "Update";
            DevKit.UpdateAndAssert(Well);

            var now = DateTime.UtcNow;

            Well.Name = "Update2";
            DevKit.UpdateAndAssert(Well);

            DevKit.DeleteAndAssert(new Well() { Uid = Well.Uid });

            var changeTypeQuery = $"<changeLog uidObject=\"{Well.Uid}\">" + Environment.NewLine +
                                  "<changeHistory>" + Environment.NewLine +
                                  "<changeType>{0}</changeType>{1}" + Environment.NewLine +
                                  "</changeHistory>" + Environment.NewLine +
                                  "</changeLog>";

            // Confirm add change history
            var changeType = Energistics.DataAccess.WITSML141.ReferenceData.ChangeInfoType.add;
            var queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, string.Empty));

            var changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory.Count == 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory[0].ChangeType == changeType);

            // Confirm update change history
            changeType = Energistics.DataAccess.WITSML141.ReferenceData.ChangeInfoType.update;
            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, string.Empty));

            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory.Count == 2);
            Assert.IsTrue(changeLogs.First().ChangeHistory[0].ChangeType == changeType);
            Assert.IsTrue(changeLogs.First().ChangeHistory[1].ChangeType == changeType);

            // Confirm update change history for last change
            changeType = Energistics.DataAccess.WITSML141.ReferenceData.ChangeInfoType.update;
            var dTimLastChange = $"<dTimChange>{now:O}</dTimChange>";
            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, dTimLastChange));

            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory.Count == 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory[0].ChangeType == changeType);

            // Confirm delete change history
            changeType = Energistics.DataAccess.WITSML141.ReferenceData.ChangeInfoType.delete;
            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, string.Empty));

            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory.Count == 1);
            Assert.IsTrue(changeLogs.First().ChangeHistory[0].ChangeType == changeType);
        }

        private List<ChangeLog> GetAndAssertChangeLog(string queryIn, OptionsIn optionsIn, int? expectedChangeLogCount = null)
        {
            var changeLogs = DevKit.Query<ChangeLogList, ChangeLog>(ObjectTypes.ChangeLog, queryIn, null, optionsIn);

            Assert.IsNotNull(changeLogs);

            if (expectedChangeLogCount.HasValue)
                Assert.AreEqual(expectedChangeLogCount, changeLogs.Count);

            return changeLogs;
        }
    }
}
