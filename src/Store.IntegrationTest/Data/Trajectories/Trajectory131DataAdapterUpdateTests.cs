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
using System.Threading;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Jobs;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory131DataAdapterUpdateTests
    /// </summary>
    [TestClass]
    public partial class Trajectory131DataAdapterUpdateTests : Trajectory131TestBase
    {
        private const int GrowingTimeoutPeriod = 10;

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.MagDeclUsed = new PlaneAngleMeasure { Uom = PlaneAngleUom.dega, Value = 20.0 };
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNull(result.AziRef);

            const string content = "<magDeclUsed /><aziRef>grid north</aziRef>";
            var xmlIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, content);
            DevKit.UpdateAndAssert(ObjectTypes.Trajectory, xmlIn);

            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(AziRef.gridnorth, result.AziRef);
            Assert.IsNull(result.MagDeclUsed);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Data()
        {
            Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Data(4);
            TestReset(10);
            Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Data(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Update_With_Unordered_Stations()
        {
            Trajectory131DataAdapter_UpdateInStore_Update_With_Unordered_Stations(5);
            TestReset(3);
            Trajectory131DataAdapter_UpdateInStore_Update_With_Unordered_Stations(5);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_Set_ObjectGrowing_And_IsActive_State()
        {
            Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_Set_ObjectGrowing_And_IsActive_State(5);
            TestReset(5);
            Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_Set_ObjectGrowing_And_IsActive_State(10);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_ExpireGrowingObjects()
        {
            Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_ExpireGrowingObjects(5);
            TestReset(5);
            Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_ExpireGrowingObjects(10);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Station_Unchanged_ObjectGrowing_State()
        {
            Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Station_Unchanged_ObjectGrowing_State(5);
            TestReset(5);
            Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Station_Unchanged_ObjectGrowing_State(10);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Error_443_Invalid_UOM()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Update trajectory with XML
            var station2 = stations[2];
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.Uid, Trajectory.UidWell,
                Trajectory.UidWellbore,
                $"<nameWell>{Trajectory.NameWell}</nameWell>" + Environment.NewLine +
                $"<nameWellbore>{Trajectory.NameWellbore}</nameWellbore>" + Environment.NewLine +
                $"<name>{Trajectory.Name}</name>" + Environment.NewLine +
                $"<trajectoryStation uid=\"{station2.Uid}\">" + Environment.NewLine +
                "	<typeTrajStation>magnetic MWD</typeTrajStation>" + Environment.NewLine +
                $"	<md uom=\"{station2.MD.Uom}\">{station2.MD.Value}</md>" + Environment.NewLine +
                "	<rawData>" + Environment.NewLine +
                "		<gravAxialRaw uom=\"ft/s2\">0.116</gravAxialRaw>" + Environment.NewLine +
                "		<gravTran1Raw uom=\"ft/s2\">-0.168</gravTran1Raw>" + Environment.NewLine +
                "		<gravTran2Raw uom=\"000\">-1654</gravTran2Raw>" + Environment.NewLine +
                "		<magAxialRaw uom=\"nT\">22.77</magAxialRaw>" + Environment.NewLine +
                "		<magTran1Raw uom=\"nT\">22.5</magTran1Raw>" + Environment.NewLine +
                "		<magTran2Raw uom=\"nT\">27.05</magTran2Raw>" + Environment.NewLine +
                "	</rawData>" + Environment.NewLine +
                "</trajectoryStation>");

            DevKit.UpdateAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.InvalidUnitOfMeasure);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Error_446_Missing_Value_When_UOM_Specified()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Update trajectory with XML
            var station2 = stations[2];
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid,
                $"<nameWell>{Trajectory.NameWell}</nameWell>" + Environment.NewLine +
                $"<nameWellbore>{Trajectory.NameWellbore}</nameWellbore>" + Environment.NewLine +
                $"<name>{Trajectory.Name}</name>" + Environment.NewLine +
                $"<trajectoryStation uid=\"{station2.Uid}\">" + Environment.NewLine +
                "	<typeTrajStation>magnetic MWD</typeTrajStation>" + Environment.NewLine +
                $"	<md uom=\"{station2.MD.Uom}\">{station2.MD.Value}</md>" + Environment.NewLine +
                "	<rawData>" + Environment.NewLine +
                "		<gravAxialRaw uom=\"ft/s2\">0.116</gravAxialRaw>" + Environment.NewLine +
                "		<gravTran1Raw uom=\"ft/s2\">-0.168</gravTran1Raw>" + Environment.NewLine +
                "		<gravTran2Raw uom=\"ft/s2\" />" + Environment.NewLine +
                "		<magAxialRaw uom=\"nT\">22.77</magAxialRaw>" + Environment.NewLine +
                "		<magTran1Raw uom=\"nT\">22.5</magTran1Raw>" + Environment.NewLine +
                "		<magTran2Raw uom=\"nT\">27.05</magTran2Raw>" + Environment.NewLine +
                "	</rawData>" + Environment.NewLine +
                "</trajectoryStation>");

            DevKit.UpdateAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.MissingMeasureDataForUnit);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Error_448_Missing_Station_UID()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Remove UID from station 2
            stations[2].Uid = string.Empty;

            DevKit.UpdateAndAssert(Trajectory, ErrorCodes.MissingElementUidForUpdate);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Error_453_Missing_UOM()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Update trajectory with XML
            var station2 = stations[2];
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.Uid, Trajectory.UidWell,
                Trajectory.UidWellbore,
                $"<nameWell>{Trajectory.NameWell}</nameWell>" + Environment.NewLine +
                $"<nameWellbore>{Trajectory.NameWellbore}</nameWellbore>" + Environment.NewLine +
                $"<name>{Trajectory.Name}</name>" + Environment.NewLine +
                $"<trajectoryStation uid=\"{station2.Uid}\">" + Environment.NewLine +
                "	<typeTrajStation>magnetic MWD</typeTrajStation>" + Environment.NewLine +
                $"	<md uom=\"{station2.MD.Uom}\">{station2.MD.Value}</md>" + Environment.NewLine +
                "	<rawData>" + Environment.NewLine +
                "		<gravAxialRaw uom=\"ft/s2\">0.116</gravAxialRaw>" + Environment.NewLine +
                "		<gravTran1Raw uom=\"ft/s2\">-0.168</gravTran1Raw>" + Environment.NewLine +
                "		<gravTran2Raw>-1654</gravTran2Raw>" + Environment.NewLine +
                "		<magAxialRaw uom=\"nT\">22.77</magAxialRaw>" + Environment.NewLine +
                "		<magTran1Raw uom=\"nT\">22.5</magTran1Raw>" + Environment.NewLine +
                "		<magTran2Raw uom=\"nT\">27.05</magTran2Raw>" + Environment.NewLine +
                "	</rawData>" + Environment.NewLine +
                "</trajectoryStation>");

            DevKit.UpdateAndAssert(ObjectTypes.Trajectory, queryIn, ErrorCodes.MissingUnitForMeasureData);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_UpdateInStore_Error_464_Duplicate_Station_UIDs()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Set station 2 UID to station 3 UID
            stations[2].Uid = stations[3].Uid;

            DevKit.UpdateAndAssert(Trajectory, ErrorCodes.ChildUidNotUnique);
        }

        private void Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Data(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(numberOfStations, 0);
            var station5 = stations.LastOrDefault();
            Assert.IsNotNull(station5);
            stations.Remove(station5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            var station1Update = new TrajectoryStation
            {
                Uid = station1.Uid,
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
                Location = new List<Location>
                {
                    new Location {Uid = "loc-1", Description = "location 1"}
                }
            };

            var update = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                TrajectoryStation = new List<TrajectoryStation> { station1Update, station5 }
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count + 1, result.TrajectoryStation.Count);
            var updatedStation1 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station1.Uid);
            Assert.IsNotNull(updatedStation1);
            Assert.AreEqual(station1.Azi.Value, updatedStation1.Azi.Value);
            Assert.AreEqual(station1Update.Location.Count, updatedStation1.Location.Count);

            var updatedStation5 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station5.Uid);
            Assert.IsNotNull(updatedStation5);
        }

        private void Trajectory131DataAdapter_UpdateInStore_Update_With_Unordered_Stations(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(numberOfStations, 0);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Create 4 new stations
            var updateStations = DevKit.TrajectoryStations(4, 1);

            // Assign new UIDs
            updateStations.ForEach(x => x.Uid = DevKit.Uid());

            // Reverse stations
            updateStations.Reverse();

            Trajectory.TrajectoryStation = updateStations;

            // Update trajectory with reversed stations
            DevKit.UpdateAndAssert(Trajectory);

            // Get trajectory and ensure stations are ordered
            updateStations.Reverse();
            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(stations?.FirstOrDefault()?.MD.Value, result.MDMin.Value);
            Assert.AreEqual(Trajectory.TrajectoryStation?.LastOrDefault()?.MD.Value, result.MDMax.Value);
        }

        private void Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_Set_ObjectGrowing_And_IsActive_State(int numberOfStations)
        {
            AddParents();

            //Add trajectory
            DevKit.AddAndAssert(Trajectory);
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            // Update trajectory with stations
            var stations = DevKit.TrajectoryStations(numberOfStations, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //another update with station
            var newStation = new TrajectoryStation
            {
                Uid = "sta-" + (numberOfStations + 1),
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 1 },
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
            };

            var update = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                TrajectoryStation = new List<TrajectoryStation> { newStation }
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count + 1, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        private void Trajectory131DataAdapter_UpdateInStore_Append_Trajectory_Stations_ExpireGrowingObjects(int numberOfStations)
        {
            AddParents();

            // Add trajectory with stations
            var stations = DevKit.TrajectoryStations(numberOfStations, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //update
            var newStation = new TrajectoryStation
            {
                Uid = "sta-" + (numberOfStations + 1),
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 6.5 },
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
            };

            var update = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                TrajectoryStation = new List<TrajectoryStation> { newStation }
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count + 1, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Trajectory);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }

        private void Trajectory131DataAdapter_UpdateInStore_Update_Trajectory_Station_Unchanged_ObjectGrowing_State(int numberOfStations)
        {
            AddParents();

            // Add trajectory with stations
            var stations = DevKit.TrajectoryStations(numberOfStations, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //update station
            var newStation = new TrajectoryStation
            {
                Uid = "sta-3",
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 6.5 },
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
            };

            var update = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                TrajectoryStation = new List<TrajectoryStation> { newStation }
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
        }
    }
}
