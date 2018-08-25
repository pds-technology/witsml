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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.ChangeLogs
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
            var addTime = DateTime.UtcNow;

            DevKit.AddAndAssert(Well);

            Well.Name = "Update";
            DevKit.UpdateAndAssert(Well);

            var updateTime = DateTime.UtcNow;

            Well.Name = "Update2";
            DevKit.UpdateAndAssert(Well);

            DevKit.DeleteAndAssert(new Well() { Uid = Well.Uid });

            var changeTypeQuery = $"<changeLog uidObject=\"{Well.Uid}\">" + Environment.NewLine +
                                  "<changeHistory>" + Environment.NewLine +
                                  "<changeType>{0}</changeType>{1}" + Environment.NewLine +
                                  "</changeHistory>" + Environment.NewLine +
                                  "</changeLog>";

            // Confirm add change history
            var changeType = ChangeInfoType.add;
            var queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, string.Empty));

            var changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            var changeHistories = changeLogs.First().ChangeHistory;
            Assert.AreEqual(1, changeHistories.Count);
            Assert.AreEqual(changeType, changeHistories[0].ChangeType);

            // Confirm update change history
            changeType = ChangeInfoType.update;
            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, string.Empty));

            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.Requested, 1);
            changeHistories = changeLogs.First().ChangeHistory;
            Assert.AreEqual(2, changeHistories.Count);
            Assert.AreEqual(changeType, changeHistories[0].ChangeType);
            Assert.AreEqual(changeType, changeHistories[1].ChangeType);

            // Ensure no other elements were returned
            DevKit.AssertRequestedElements(changeHistories[0], new[] { "changeType" });
            DevKit.AssertRequestedElements(changeHistories[1], new[] { "changeType" });

            // Confirm update change history for last change
            changeType = ChangeInfoType.update;
            var dTimLastChange = $"<dTimChange>{updateTime:O}</dTimChange>";
            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, dTimLastChange));

            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            changeHistories = changeLogs.First().ChangeHistory;
            Assert.AreEqual(1, changeHistories.Count);
            Assert.AreEqual(changeType, changeHistories[0].ChangeType);

            // Confirm delete change history
            changeType = ChangeInfoType.delete;
            queryIn = string.Format(QueryEmptyRootTemplate, string.Format(changeTypeQuery, changeType, string.Empty));

            changeLogs = GetAndAssertChangeLog(queryIn, OptionsIn.ReturnElements.All, 1);
            changeHistories = changeLogs.First().ChangeHistory;
            Assert.AreEqual(1, changeHistories.Count);
            Assert.AreEqual(changeType, changeHistories[0].ChangeType);

            // Specify multiple criteria for dTimChange and changeType
            var multipleCriteriaQuery = $"<changeLog uidObject=\"{Well.Uid}\">" + Environment.NewLine +
                                 "<objectType>well</objectType>" + Environment.NewLine +
                                 "<changeHistory>" + Environment.NewLine +
                                 $"<dTimChange>{updateTime.AddMinutes(1):O}</dTimChange>" + Environment.NewLine +
                                 $"<changeType>{ChangeInfoType.update}</changeType>" + Environment.NewLine +
                                 "</changeHistory>" + Environment.NewLine +
                                 "<changeHistory>" + Environment.NewLine +
                                 $"<dTimChange>{addTime:O}</dTimChange>" + Environment.NewLine +
                                 $"<changeType>{ChangeInfoType.add}</changeType>" + Environment.NewLine +
                                 "</changeHistory>" + Environment.NewLine +
                                 "</changeLog>";

            // Query should only return the change history for add
            changeLogs = GetAndAssertChangeLog(string.Format(QueryEmptyRootTemplate, multipleCriteriaQuery),
                OptionsIn.ReturnElements.All, 1);
            changeHistories = changeLogs.First().ChangeHistory;
            Assert.AreEqual(1, changeHistories.Count);
            Assert.AreEqual(ChangeInfoType.add, changeHistories[0].ChangeType);
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
