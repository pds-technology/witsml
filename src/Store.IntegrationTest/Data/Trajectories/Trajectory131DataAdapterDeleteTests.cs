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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory131DataAdapterDeleteTests
    /// </summary>
    [TestClass]
    public partial class Trajectory131DataAdapterDeleteTests : Trajectory131TestBase
    {
        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Delete_Full_Trajectory()
        {
            Trajectory131DataAdapter_DeleteFromStore_Delete_Full_Trajectory(4);
            TestReset(10);
            Trajectory131DataAdapter_DeleteFromStore_Delete_Full_Trajectory(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Header()
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
            var delete = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid,
                "<magDeclUsed />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNull(result.MagDeclUsed);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations()
        {
            Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations(4);
            TestReset(10);
            Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range()
        {
            Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range(10);
            TestReset(10);
            Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Station_Items()
        {
            Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Station_Items(4);
            TestReset(10);
            Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Station_Items(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Error_417_Delete_With_Empty_UOM_Attribute()
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

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Error_456_Exceed_MaxDataNodes()
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

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Error_464_Duplicate_Station_UIDs()
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

        private void Trajectory131DataAdapter_DeleteFromStore_Delete_Full_Trajectory(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
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

        private void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Stations(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete trajectory stations
            var stations = Trajectory.TrajectoryStation;
            var queryIn = $"<trajectoryStation uid=\"{stations.First().Uid}\"/> <trajectoryStation uid=\"{stations.Last().Uid}\"/>";
            var delete = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, queryIn);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count - 2, result.TrajectoryStation.Count);
        }

        private void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Stations_By_Structural_Range(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete all trajectory stations
            const int start = 5;
            const int end = 8;
            var delete = "<mdMn uom=\"m\">" + start + "</mdMn><mdMx uom=\"m\">" + end + "</mdMx>";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Trajectory.TrajectoryStation.RemoveAll(s => s.MD.Value >= start && s.MD.Value <= end);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
        }

        private void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Station_Items(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            var stations = Trajectory.TrajectoryStation;
            var station1 = stations[0];
            var station2 = stations[1];
            var station3 = stations[2];

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            var resultStation3 = stations.FirstOrDefault(s => s.Uid == station3.Uid);
            Assert.IsNotNull(resultStation3);
            Assert.IsNotNull(resultStation3.MDDelta);

            // Delete trajectory stations and elements
            var delete = "<trajectoryStation uid=\"" + station1.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station2.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station3.Uid + "\"><mdDelta /></trajectoryStation>";
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid,
                delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            stations = result.TrajectoryStation;
            Assert.AreEqual(Trajectory.TrajectoryStation.Count - 2, stations.Count);
            Assert.IsNull(stations.FirstOrDefault(s => s.Uid == station1.Uid));
            Assert.IsNull(stations.FirstOrDefault(s => s.Uid == station2.Uid));
            resultStation3 = stations.FirstOrDefault(s => s.Uid == station3.Uid);
            Assert.IsNotNull(resultStation3);
            Assert.IsNull(resultStation3.MDDelta);
        }
    }
}
