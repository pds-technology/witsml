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
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory141DataAdapterDeleteTests
    /// </summary>
    [TestClass]
    public partial class Trajectory141DataAdapterDeleteTests : Trajectory141TestBase
    {
        [TestMethod]
        public void Trajectory141DataAdapter_DeleteFromStore_Delete_Full_Trajectory()
        {
            Trajectory141DataAdapter_DeleteFromStore_Delete_Full_Trajectory(4);
            TestReset(5);
            Trajectory141DataAdapter_DeleteFromStore_Delete_Full_Trajectory(10);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations()
        {
            Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations(4);
            TestReset(5);
            Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations(10);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range()
        {
            Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range(10);
            TestReset(5);
            Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range(30);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.MagDeclUsed = new PlaneAngleMeasure { Uom = PlaneAngleUom.dega, Value = 20.0 };
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNotNull(result.MagDeclUsed);

            // Delete trajectory header element
            var delete = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, "<magDeclUsed />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNull(result.MagDeclUsed);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_Partial_Delete_Trajectory_Header()
        {
            AddParents();

            // Add trajectory without stations, add change history with objectGrowingState set to false
            Trajectory.GridConUsed = new PlaneAngleMeasure { Uom = PlaneAngleUom.dega, Value = 90 };
            Trajectory.MagDeclUsed = new PlaneAngleMeasure { Uom = PlaneAngleUom.dega, Value = 90 };
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNotNull(result.GridConUsed);
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault());

            // Delete trajectory header element, add change history with updatedHeader set to true
            var deleteXml = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, "<gridConUsed />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, deleteXml);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            DevKit.AssertChangeHistoryFlags(changeHistory, true, false);
            Assert.IsNull(changeHistory.StartIndex);
            Assert.IsNull(changeHistory.EndIndex);

            // Add new station when object is not growing, add change history with objectGrowingState set to true with start and end index
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 4);
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(1, result.TrajectoryStation.Count);
            DevKit.AssertChangeLog(result, 3, ChangeInfoType.update);
            DevKit.AssertChangeHistoryFlags(changeHistory, true, true);
            DevKit.AssertChangeHistoryIndexRange(changeHistory, 4, 4);

            // Delete trajectory header element when object is growing, no entry to change log
            deleteXml = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, "<magDeclUsed />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, deleteXml);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistoryList = DevKit.GetAndAssertChangeLogHistory(result.GetUri(), false);
            changeHistory = changeHistoryList.Last();

            // No changes to changelog
            Assert.AreEqual(3, changeHistoryList.Count);
            DevKit.AssertChangeHistoryFlags(changeHistory, true, true);
            DevKit.AssertChangeHistoryIndexRange(changeHistory, 4, 4);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_Partial_Delete_Trajectory_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations, add change history with objectGrowingState set to false
            var stations = DevKit.TrajectoryStations(3, 6);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault());
            Assert.IsNull(changeHistory.StartIndex);
            Assert.IsNull(changeHistory.EndIndex);

            // Delete trajectory station when object not growing, add change history with objectGrowingState set to false with start and end index
            var stationToDelete = Trajectory.TrajectoryStation.First();
            var delete = @"<trajectoryStation uid=""" + stationToDelete.Uid + @"""></trajectoryStation>";
            var queryXml = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryXml);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(2, result.TrajectoryStation.Count);
            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            DevKit.AssertChangeHistoryFlags(changeHistory, true, false);
            DevKit.AssertChangeHistoryIndexRange(changeHistory, 6, 6);

            // Add new station when object is not growing, add change history with objectGrowingState set to true with start and end index
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 4);
            var firstStation = Trajectory.TrajectoryStation.First();
            firstStation.Uid = "Sta-4";
            firstStation.MD.Value = 3;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(3, result.TrajectoryStation.Count);
            DevKit.AssertChangeLog(result, 3, ChangeInfoType.update);
            DevKit.AssertChangeHistoryFlags(changeHistory, true, true);
            DevKit.AssertChangeHistoryIndexRange(changeHistory, 3, 3);

            // Delete stations when object is growing, no entry to change log
            stationToDelete = Trajectory.TrajectoryStation.First();
            delete = @"<trajectoryStation uid=""" + stationToDelete.Uid + @"""></trajectoryStation>";
            queryXml = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryXml);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistoryList = DevKit.GetAndAssertChangeLogHistory(result.GetUri(), false);
            changeHistory = changeHistoryList.Last();

            // No changes to changelog
            Assert.AreEqual(3, changeHistoryList.Count);
            Assert.AreEqual(2, result.TrajectoryStation.Count);
            DevKit.AssertChangeHistoryFlags(changeHistory, true, true);
            DevKit.AssertChangeHistoryIndexRange(changeHistory, 3, 3);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the trajectory uid")]
        public void Log141DataAdapter_DeleteFromStore_Error_415_Delete_Without_Specifing_UID()
        {
            AddParents();

            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, string.Empty, string.Empty);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.DataObjectUidMissing);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore without specifying the trajectory station uid")]
        public void Log141DataAdapter_DeleteFromStore_Error_416_Delete_Without_Specifing_Station_UID()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(4, 0);
            DevKit.AddAndAssert(Trajectory);

            // Delete trajectory stations and elements
            var delete = "<trajectoryStation uid=\"\" />";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.EmptyUidSpecified);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_DeleteFromStore_Error_417_Delete_With_Empty_UOM_Attribute()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 1);
            DevKit.AddAndAssert(Trajectory);

            //empty uom in header
            var delete = "<magDeclUsed uom=\"\">1</magDeclUsed>";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.EmptyUomSpecified);

            //empty uom in station
            var firstStation = Trajectory.TrajectoryStation.First();
            delete = $"<trajectoryStation uid=\"{firstStation.Uid}\"><tvd uom=\"\">1</tvd></trajectoryStation>";
            queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.EmptyUomSpecified);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with more stations than specified in Trajectory MaxDataNodes")]
        public void Trajectory141DataAdapter_DeleteFromStore_Error_456_Exceed_MaxDataNodes()
        {
            // Add well and wellbore
            AddParents();
            var maxDataNodes = 2;
            WitsmlSettings.TrajectoryMaxDataNodesDelete = maxDataNodes;

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(4, 0);
            DevKit.AddAndAssert(Trajectory);

            var stations = Trajectory.TrajectoryStation;
            var station1 = stations[0];
            var station2 = stations[1];
            var station3 = stations[2];

            // Delete trajectory stations and elements
            var delete = "<trajectoryStation uid=\"" + station1.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station2.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station3.Uid + "\" />";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.MaxDataExceeded);
        }

        [TestMethod, Description("Tests you cannot do DeleteFromStore with duplicate station UIDs")]
        public void Trajectory141DataAdapter_DeleteFromStore_Error_464_Duplicate_Station_UIDs()
        {
            // Add well and wellbore
            AddParents();
            var maxDataNodes = 2;
            WitsmlSettings.TrajectoryMaxDataNodesDelete = maxDataNodes;

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(4, 0);
            DevKit.AddAndAssert(Trajectory);

            var stations = Trajectory.TrajectoryStation;
            var station1 = stations[0];

            // Delete trajectory stations and elements
            var delete = "<trajectoryStation uid=\"" + station1.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station1.Uid + "\" />";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.ChildUidNotUnique);
        }

        private void Trajectory141DataAdapter_DeleteFromStore_Delete_Full_Trajectory(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            DevKit.GetAndAssert(Trajectory);

            // Delete trajectory
            var delete = DevKit.CreateQuery(Trajectory);
            DevKit.DeleteAndAssert<TrajectoryList, Trajectory>(delete);

            // Assert delete results
            DevKit.GetAndAssert(Trajectory, false);
        }

        private void Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete all trajectory stations
            var stations = Trajectory.TrajectoryStation;
            var queryIn = $"<trajectoryStation uid=\"{stations.First().Uid}\"/> <trajectoryStation uid=\"{stations.Last().Uid}\"/>";
            var delete = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, queryIn);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count - 2, result.TrajectoryStation.Count);
        }

        private void Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete all trajectory stations with start and end
            var start = 1;
            var end = 10;
            var delete = "<mdMn uom=\"m\">" + start + "</mdMn><mdMx uom=\"m\">" + end + "</mdMx>";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Trajectory.TrajectoryStation.RemoveAll(s => s.MD.Value >= start && s.MD.Value <= end);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Get trajectory
            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete all trajectory stations with mdMn
            start = 20;
            delete = $"<mdMn uom=\"m\">{start}</mdMn>";
            queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Trajectory.TrajectoryStation.RemoveAll(s => s.MD.Value >= start);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete all trajectory stations with mdMx
            end = 10;
            delete = $"<mdMx uom=\"m\">{end}</mdMx>";
            queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Trajectory.TrajectoryStation.RemoveAll(s => s.MD.Value <= end);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
        }
    }
}
