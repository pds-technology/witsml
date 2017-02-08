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
using System.Threading;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Datatypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Jobs;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Trajectory141DataAdapterUpdateTests
    /// </summary>
    [TestClass]
    public partial class Trajectory141DataAdapterUpdateTests : Trajectory141TestBase
    {
        private const int GrowingTimeoutPeriod = 10;

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Update_Trajectory_Header()
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
        public void Trajectory141DataAdapter_UpdateInStore_Update_Trajectory_Data()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
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
                ExtensionNameValue = new List<ExtensionNameValue> { DevKit.ExtensionNameValue("Ext-1", "1.0", "m") }
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
            Assert.AreEqual(5, result.TrajectoryStation.Count);
            var updatedStation1 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station1.Uid);
            Assert.IsNotNull(updatedStation1);
            Assert.AreEqual(station1.Azi.Value, updatedStation1.Azi.Value);
            Assert.AreEqual(station1Update.ExtensionNameValue.Count, updatedStation1.ExtensionNameValue.Count);

            var updatedStation5 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station5.Uid);
            Assert.IsNotNull(updatedStation5);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Update_With_Unordered_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(1, 0);
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

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Update_Trajectory_With_Existing_ExtensionNameValue()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            var stations = DevKit.TrajectoryStations(5, 0);
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
            var ext1 = new List<ExtensionNameValue>
            {
                DevKit.ExtensionNameValue("Ext-1", "1.0", "m")
            };
            var station1Update = new TrajectoryStation
            {
                Uid = station1.Uid,
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
                ExtensionNameValue = ext1
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
            Assert.AreEqual(5, result.TrajectoryStation.Count);
            var updatedStation1 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station1.Uid);
            Assert.IsNotNull(updatedStation1);
            Assert.AreEqual(station1.Azi.Value, updatedStation1.Azi.Value);
            Assert.AreEqual(station1Update.ExtensionNameValue.Count, updatedStation1.ExtensionNameValue.Count);
            var updatedStation5 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station5.Uid);
            Assert.IsNotNull(updatedStation5);

            // Modify ExtensionNameValue's value and update
            ext1[0].Value.Value = "2.0";
            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(5, result.TrajectoryStation.Count);
            updatedStation1 = result.TrajectoryStation.FirstOrDefault(s => s.Uid == station1.Uid);
            Assert.IsNotNull(updatedStation1);
            Assert.AreEqual(station1.Azi.Value, updatedStation1.Azi.Value);
            Assert.AreEqual(station1Update.ExtensionNameValue.Count, updatedStation1.ExtensionNameValue.Count);
            var updatedExtensionNameValue = updatedStation1.ExtensionNameValue[0];
            Assert.IsNotNull(updatedExtensionNameValue);
            Assert.AreEqual("2.0", updatedExtensionNameValue.Value?.Value);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Append_Trajectory_Stations_Set_ObjectGrowing_And_IsActive_State()
        {
            AddParents();

            //Add trajectory
            DevKit.AddAndAssert(Trajectory);
            var result = DevKit.GetAndAssert(Trajectory);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            // Update trajectory with stations
            var stations = DevKit.TrajectoryStations(3, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //another update with station
            var newStation = new TrajectoryStation
            {
                Uid = "sta-4",
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
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Append_Trajectory_Stations_ExpireGrowingObjects()
        {
            AddParents();

            // Add trajectory with stations
            var stations = DevKit.TrajectoryStations(3, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //update
            var newStation = new TrajectoryStation
            {
                Uid = "sta-4",
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
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Update_Trajectory_Station_Unchanged_ObjectGrowing_State()
        {
            AddParents();

            // Add trajectory with stations
            var stations = DevKit.TrajectoryStations(3, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

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
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(3, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Append_Update_Trajectory_Station_Set_ObjectGrowing_State()
        {
            AddParents();

            // Add trajectory with stations
            var stations = DevKit.TrajectoryStations(3, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //append station
            var newStation = new TrajectoryStation
            {
                Uid = "sta-4",
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
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");

            station1.Azi.Value++;

            //update station
            var newStation2 = new TrajectoryStation
            {
                Uid = "sta-4",
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 6.5 },
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
            };

            update = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                TrajectoryStation = new List<TrajectoryStation> { newStation2 }
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
        }

        [TestMethod]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_ObjectGrowing_State()
        {
            AddParents();

            // Add trajectory with stations
            var stations = DevKit.TrajectoryStations(3, 5);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);
            Assert.IsFalse(changeHistory.ObjectGrowingState.GetValueOrDefault());

            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            //update with station, set change history object growing flag
            var newStation = new TrajectoryStation
            {
                Uid = "sta-4",
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
            wellboreResult = DevKit.GetAndAssert(Wellbore);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault());

            station1.Azi.Value++;

            //update2 with new station and object growing, no entry in change log 
            var newStation2 = new TrajectoryStation
            {
                Uid = "sta-5",
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 10 },
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
            };

            update = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                TrajectoryStation = new List<TrajectoryStation> { newStation2 }
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);

            result = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(5, result.TrajectoryStation.Count);
            Assert.IsTrue(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault());

            //expire growing objects, add change history with object growing set to false
            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            result = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault(), "ObjectGrowing");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
            DevKit.AssertChangeLog(result, 3, ChangeInfoType.update);
            Assert.IsFalse(changeHistory.ObjectGrowingState.GetValueOrDefault());
        }

        [TestMethod]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_Update_To_Trajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations, add change history with objectGrowingState set to false
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault());

            //update header info, add change history with updatedHeader set to true
            Trajectory.ServiceCompany = "Test Company";
            Trajectory.TrajectoryStation = null;
            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            Assert.IsTrue(changeHistory.UpdatedHeader.GetValueOrDefault(), "updatedHeader");
            Assert.IsFalse(changeHistory.ObjectGrowingState.GetValueOrDefault(), "objectGrowingState");

            // Update trajectory with stations, add change history with objectGrowingState set to true
            var stations = DevKit.TrajectoryStations(3, 6);
            Trajectory.TrajectoryStation = stations;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            DevKit.AssertChangeLog(result, 3, ChangeInfoType.update);
            Assert.IsNotNull(changeHistory);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault(), "objectGrowingState");

            //update header info again when object is growing, no entry to change log
            Trajectory.ServiceCompany = "Testing Company again";
            Trajectory.TrajectoryStation = null;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistoryList = DevKit.GetAndAssertChangeLogHistory(result.GetUri(), false);

            //no changes to changelog
            Assert.AreEqual(3, changeHistoryList.Count);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault());
        }

        [TestMethod]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_Update_To_Trajectory_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations, add change history with objectGrowingState set to false
            var stations = DevKit.TrajectoryStations(3, 6);
            Trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault());

            // Update trajectory station, add change history with objectGrowingState set to false with start and end index
            var stationUpdate = Trajectory.TrajectoryStation.First();
            stationUpdate.MD.Value = 6.5;
            Trajectory.TrajectoryStation = new List<TrajectoryStation> { stationUpdate };
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            Assert.IsNotNull(changeHistory);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsFalse(changeHistory.ObjectGrowingState.GetValueOrDefault());
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(6.5, changeHistory.StartIndex.Value);
            Assert.AreEqual(8, changeHistory.EndIndex.Value);

            // Update trajectory station agian with index change, add change history with objectGrowingState set to false with start and end index
            stationUpdate = Trajectory.TrajectoryStation.First();
            stationUpdate.MD.Value = 10;
            stationUpdate.Azi = new PlaneAngleMeasure(10, PlaneAngleUom.dega);
            Trajectory.TrajectoryStation = new List<TrajectoryStation> { stationUpdate };
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            DevKit.AssertChangeLog(result, 3, ChangeInfoType.update);
            Assert.IsNotNull(changeHistory);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsFalse(changeHistory.ObjectGrowingState.GetValueOrDefault());
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(7, changeHistory.StartIndex.Value);
            Assert.AreEqual(10, changeHistory.EndIndex.Value);

            // Add new station when object is not growing, add change history with objectGrowingState set to true with start and end index
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 4);
            Trajectory.TrajectoryStation.First().Uid = "Sta-4";
            Trajectory.TrajectoryStation.First().MD.Value = 3;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            DevKit.AssertChangeLog(result, 4, ChangeInfoType.update);
            Assert.IsNotNull(changeHistory);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault(), "objectGrowingState");
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(3, changeHistory.StartIndex.Value);
            Assert.AreEqual(10, changeHistory.EndIndex.Value);

            // Update station, no entry to change log
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 4);
            Trajectory.TrajectoryStation.First().Uid = "Sta-4";
            Trajectory.TrajectoryStation.First().MD.Value = 20;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();
            var changeHistoryList = DevKit.GetAndAssertChangeLogHistory(result.GetUri(), false);

            // No changes to changelog
            Assert.AreEqual(4, changeHistoryList.Count);
            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault());
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(3, changeHistory.StartIndex.Value);
            Assert.AreEqual(10, changeHistory.EndIndex.Value);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_Append_To_Trajectory_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations, add change history with objectGrowingState set to false
            DevKit.AddAndAssert(Trajectory);

            var result = DevKit.GetAndAssert(Trajectory);
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault());

            // Update trajectory with stations, add change history with objectGrowingState set to true with start and end index
            var stations = DevKit.TrajectoryStations(3, 6);
            Trajectory.TrajectoryStation = stations;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            Assert.IsNotNull(changeHistory);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault(), "objectGrowingState");
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(6, changeHistory.StartIndex.Value);
            Assert.AreEqual(8, changeHistory.EndIndex.Value);

            // Add new station when object is growing, no entry to change log
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 4);
            Trajectory.TrajectoryStation.First().Uid = "Sta-4";
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistoryList = DevKit.GetAndAssertChangeLogHistory(result.GetUri(), false);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            // No changes to changelog
            Assert.AreEqual(2, changeHistoryList.Count);
            Assert.AreEqual(4, result.TrajectoryStation.Count);
            Assert.IsFalse(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault());
            Assert.IsNotNull(changeHistory.StartIndex);
            Assert.IsNotNull(changeHistory.EndIndex);
            Assert.AreEqual(6, changeHistory.StartIndex.Value);
            Assert.AreEqual(8, changeHistory.EndIndex.Value);
        }

        [TestMethod]
        [Ignore]
        public void Trajectory141DataAdapter_ChangeLog_Tracks_Update_To_Trajectory_Header_And_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations, add change history with objectGrowingState set to false
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            DevKit.AssertChangeLog(result, 1, ChangeInfoType.add);
            Assert.IsFalse(result.ObjectGrowing.GetValueOrDefault());

            //update header info and add stations, add change history with updatedHeader set to true and object growing to true with start/end index
            Trajectory.ServiceCompany = "Test Company";
            Trajectory.TrajectoryStation = DevKit.TrajectoryGenerator.GenerationStations(2, 4);
            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            var changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();

            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            DevKit.AssertChangeLog(result, 2, ChangeInfoType.update);
            Assert.IsTrue(changeHistory.UpdatedHeader.GetValueOrDefault(), "updatedHeader");
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault(), "objectGrowingState");


            // Update header and add trajectory with station with object growing, no entry to change log
            Trajectory.ServiceCompany = "Testing Company";
            var station = DevKit.TrajectoryStations(1, 6);
            station.First().Uid = "Sta-3";
            Trajectory.TrajectoryStation = station;
            DevKit.UpdateAndAssert(Trajectory);

            result = DevKit.GetAndAssert(Trajectory);
            changeHistory = DevKit.GetAndAssertChangeLogHistory(result.GetUri()).First();
            var changeHistoryList = DevKit.GetAndAssertChangeLogHistory(result.GetUri(), false);

            //no changes to changelog
            Assert.AreEqual(3, result.TrajectoryStation.Count);
            Assert.AreEqual(2, changeHistoryList.Count);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            Assert.IsTrue(changeHistory.UpdatedHeader.GetValueOrDefault());
            Assert.IsTrue(changeHistory.ObjectGrowingState.GetValueOrDefault());
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Error_443_Invalid_UOM()
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
                "		<gravTran2Raw uom=\"000\">-1654</gravTran2Raw>" + Environment.NewLine +
                "		<magAxialRaw uom=\"nT\">22.77</magAxialRaw>" + Environment.NewLine +
                "		<magTran1Raw uom=\"nT\">22.5</magTran1Raw>" + Environment.NewLine +
                "		<magTran2Raw uom=\"nT\">27.05</magTran2Raw>" + Environment.NewLine +
                "	</rawData>" + Environment.NewLine +
                "</trajectoryStation>");

            var response = DevKit.UpdateInStore(ObjectTypes.Trajectory, queryIn, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.InvalidUnitOfMeasure, response.Result);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Error_446_Missing_Value_When_UOM_Specified()
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

            var response = DevKit.UpdateInStore(ObjectTypes.Trajectory, queryIn, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingMeasureDataForUnit, response.Result);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Error_448_Missing_Station_UID()
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
        public void Trajectory141DataAdapter_UpdateInStore_Error_453_Missing_UOM()
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
                "		<gravTran2Raw>-1654</gravTran2Raw>" + Environment.NewLine +
                "		<magAxialRaw uom=\"nT\">22.77</magAxialRaw>" + Environment.NewLine +
                "		<magTran1Raw uom=\"nT\">22.5</magTran1Raw>" + Environment.NewLine +
                "		<magTran2Raw uom=\"nT\">27.05</magTran2Raw>" + Environment.NewLine +
                "	</rawData>" + Environment.NewLine +
                "</trajectoryStation>");

            var response = DevKit.UpdateInStore(ObjectTypes.Trajectory, queryIn, null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.MissingUnitForMeasureData, response.Result);
        }

        [TestMethod, Description("Tests you cannot do UpdateInStore with more data nodes than specified in Trajectory MaxDataNodes")]
        public void Trajectory141DataAdapter_UpdateInStore_Error_456_Exceed_MaxDataNodes()
        {
            // Add well and wellbore
            AddParents();
            var maxDataNodes = 5;
            WitsmlSettings.TrajectoryMaxDataNodesUpdate = maxDataNodes;

            // Add trajectory with 1 station
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(1, 0);
            DevKit.AddAndAssert(Trajectory);

            // Update trajectory with exceeding amount of stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxDataNodes + 1, 1);
            DevKit.UpdateAndAssert(Trajectory, ErrorCodes.MaxDataExceeded);

            // Add trajetory with max allowed stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxDataNodes, 1);
            DevKit.UpdateAndAssert(Trajectory);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_UpdateInStore_Error_464_Duplicate_Station_UIDs()
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
    }
}