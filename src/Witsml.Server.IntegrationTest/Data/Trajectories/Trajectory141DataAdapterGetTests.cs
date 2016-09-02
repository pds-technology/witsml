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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Data.Trajectories;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Trajectory141DataAdapterGetTests
    /// </summary>
    public partial class Trajectory141DataAdapterGetTests
    {
        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_All()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory);

            DevKit.AssertNames(result, Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            Assert.IsNotNull(result.CommonData);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory);

            DevKit.AssertNames(result, Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Id_Only()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory, optionsIn: OptionsIn.ReturnElements.IdOnly);

            DevKit.AssertNames(result, Trajectory);
            Assert.IsNull(result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Default()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory, optionsIn: string.Empty);

            DevKit.AssertNames(result);
            Assert.IsNull(result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Requested()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.ServiceCompany = "Service Company T";
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssertWithXml(Trajectory, "<serviceCompany />", optionsIn: OptionsIn.ReturnElements.Requested);

            DevKit.AssertNames(result);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssertWithXml(Trajectory, "<serviceCompany /><trajectoryStation />", optionsIn: OptionsIn.ReturnElements.Requested);

            DevKit.AssertNames(result);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation, true);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Query_OptionsIn_requestObjectSelectionCapability()
        {
            var trajectory = new Trajectory();
            var result = DevKit.GetAndAssert(trajectory, optionsIn: OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual("abc", result.Uid);
            Assert.AreEqual(1, result.TrajectoryStation.Count);
            Assert.IsNotNull(result.CommonData.DateTimeLastChange);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Query_OptionsIn_PrivateGroupOnly()
        {
            AddParents();

            var commonData = new CommonData {PrivateGroupOnly = true};
            Trajectory.CommonData = commonData;

            DevKit.AddAndAssert(Trajectory);

            // Add a non-private trajectory
            var trajectory2 = new Trajectory
            {
                Uid = DevKit.Uid(),
                Name = "Trajectory public",
                UidWell = Trajectory.UidWell,
                NameWell = Trajectory.NameWell,
                UidWellbore = Trajectory.UidWellbore,
                NameWellbore = Trajectory.NameWellbore
            };

            DevKit.AddAndAssert(trajectory2);

            var query = new Trajectory();
            var results = DevKit.Query<TrajectoryList, Trajectory>(query, optionsIn: OptionsIn.ReturnElements.All + ";" + OptionsIn.RequestPrivateGroupOnly.True);
            Assert.IsNotNull(results);

            var notPrivateGroupsTrajectory = results.Where(x =>
            {
                var isPrivate = x.CommonData.PrivateGroupOnly ?? false;
                return !isPrivate;
            });
            Assert.IsFalse(notPrivateGroupsTrajectory.Any());

            var result = results.FirstOrDefault(x => x.Uid.Equals(Trajectory.Uid));
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory, optionsIn: OptionsIn.ReturnElements.DataOnly);

            DevKit.AssertNames(result);
            Assert.IsNull(result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation, true);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory, optionsIn: OptionsIn.ReturnElements.DataOnly);

            DevKit.AssertNames(result);
            Assert.IsNull(result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(20, 10.2, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            const int start = 15;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord {Uom = Trajectory141Generator.MdUom, Value = start}
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value > start).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(20, 10.2, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            const int start = 15;
            const int end = 20;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start },
                MDMax = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = end }
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value > start && s.MD.Value < end).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        private void AssertTrajectoryStations(List<TrajectoryStation> stations, List<TrajectoryStation> results, bool fullStation = false)
        {
            Assert.AreEqual(stations.Count, results.Count);

            foreach (var station in stations)
            {
                var result = results.FirstOrDefault(s => s.Uid == station.Uid);
                Assert.IsNotNull(result);
                Assert.AreEqual(station.TypeTrajStation, result.TypeTrajStation);
                Assert.AreEqual(station.MD?.Value, result.MD?.Value);
                Assert.AreEqual(station.Tvd?.Value, result.Tvd?.Value);
                Assert.AreEqual(station.Azi?.Value, result.Azi?.Value);
                Assert.AreEqual(station.Incl?.Value, result.Incl?.Value);
                Assert.AreEqual(station.DateTimeStn, result.DateTimeStn);

                if (!fullStation)
                    continue;

                Assert.AreEqual(station.Mtf?.Value, result.Mtf?.Value);
                Assert.AreEqual(station.MDDelta?.Value, result.MDDelta?.Value);
                Assert.AreEqual(station.StatusTrajStation, result.StatusTrajStation);
            }
        }
    }
}