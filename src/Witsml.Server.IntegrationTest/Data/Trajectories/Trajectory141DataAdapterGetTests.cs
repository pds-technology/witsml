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
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Data.Trajectories;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Trajectory141DataAdapterGetTests
    /// </summary>
    [TestClass]
    public partial class Trajectory141DataAdapterGetTests : Trajectory141TestBase
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

            // Add trajectory with stations
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
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, "<serviceCompany />", optionsIn: OptionsIn.ReturnElements.Requested);

            DevKit.AssertNames(result);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, "<serviceCompany /><trajectoryStation />", optionsIn: OptionsIn.ReturnElements.Requested);

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

            var commonData = new CommonData { PrivateGroupOnly = true };
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
            var results = DevKit.Query<TrajectoryList, Trajectory>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly + ";" + OptionsIn.RequestPrivateGroupOnly.True);
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

            // Add trajectory with stations
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

            // Add trajectory with stations
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

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(20, 10, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            const int start = 15;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start }
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(20, 10, inCludeExtra: true);
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

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start && s.MD.Value <= end).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filters_Results_With_No_Data()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(20, 10, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Query end range before the trajectory structure
            var end = 9;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMax = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = end }
            };
            DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true, isNotNull: false);

            // Query start range after the trajectory structure
            var start = 100;
            query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start },
            };
            DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true, isNotNull: false);

            // Query range outside the trajectory structure
            start = 2;
            end = 5;
            query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start },
                MDMax = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = end }
            };
            DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true, isNotNull: false);
        }

        [TestMethod, Description("Tests GetFromStore on Trajectory is limited to MaxDataNodes")]
        public void Trajectory141DataAdapter_GetFromStore_Results_Are_Limited_To_MaxDataNodes()
        {
            // Add well and wellbore
            AddParents();
            var maxDataNodes = 5;
            WitsmlSettings.TrajectoryMaxDataNodesGet = maxDataNodes;

            // Add trajectory with 1 station more than MaxDataNodes
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxDataNodes + 1, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            short errorCode;
            var result = DevKit.QueryWithErrorCode<TrajectoryList, Trajectory>(new Trajectory()
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore
            }, out errorCode, ObjectTypes.Trajectory, null,
                OptionsIn.ReturnElements.All);

            Assert.AreEqual((short)ErrorCodes.ParialSuccess, errorCode, "Returning partial data.");
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.Count);

            var traj = result[0];
            Assert.IsNotNull(traj);
            Assert.AreEqual(maxDataNodes, traj.TrajectoryStation.Count);
        }
        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Returns_Data_Only_On_Chunked_Data()
        {
            // Add well and wellbore
            AddParents();
            var maxStationCount = 5;
            WitsmlSettings.MaxStationCount = maxStationCount;

            // Add trajectory with 1 station more than MaxDataNodes
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxStationCount + 1, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            short errorCode;
            var result = DevKit.QueryWithErrorCode<TrajectoryList, Trajectory>(new Trajectory()
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore
            }, out errorCode, ObjectTypes.Trajectory, null,
                OptionsIn.ReturnElements.DataOnly);

            Assert.AreEqual((short)ErrorCodes.Success, errorCode, "Returned all data.");
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.Count);

            var traj = result[0];
            Assert.IsNotNull(traj);
            Assert.AreEqual(maxStationCount + 1, traj.TrajectoryStation.Count);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Returns_Data_Only_Large_Trajectory()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with 1000 stations
            var stationsLoaded = 100;
            var added = false;
            while (stationsLoaded <= 1000)
            {
                Trajectory.TrajectoryStation = DevKit.TrajectoryStations(100, stationsLoaded + 1);
                Trajectory.TrajectoryStation.ForEach(x => x.Uid = DevKit.Uid());
                if (added)
                    DevKit.UpdateAndAssert(Trajectory);
                else
                    DevKit.AddAndAssert(Trajectory);
                added = true;
                stationsLoaded += 100;
            }

            // Get trajectory
            short errorCode;
            var trajectoryQuery = new Trajectory()
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore
            };
            var result = DevKit.QueryWithErrorCode<TrajectoryList, Trajectory>(
                    trajectoryQuery, out errorCode, ObjectTypes.Trajectory, null,
                    OptionsIn.ReturnElements.DataOnly);

            Assert.AreEqual((short)ErrorCodes.Success, errorCode, "Returned all data.");
            Assert.IsNotNull(result);

            Assert.AreEqual(1, result.Count);

            var traj = result[0];
            Assert.IsNotNull(traj);
            Assert.AreEqual(1000, traj.TrajectoryStation.Count);
            traj.TrajectoryStation.ForEach(x => Assert.IsTrue(x.MD.Value > 0));
            Assert.AreEqual(101, traj.TrajectoryStation.Min(x => x.MD.Value));
            Assert.AreEqual(1100, traj.TrajectoryStation.Max(x => x.MD.Value));
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 5);
            DevKit.AddAndAssert(Trajectory);

            const int start = 9;
            const int end = 10;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start },
                MDMax = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = end },
                TrajectoryStation = new List<TrajectoryStation> { new TrajectoryStation { MD = new MeasuredDepthCoord { Value = 6, Uom = MeasuredDepthUom.m } } }
            };

            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start && s.MD.Value <= end).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(5, 20);
            DevKit.AddAndAssert(Trajectory);

            var queryIn = @"<trajectoryStation uid="""">
                                <md uom=""""></md>
                                <azi uom=""""></azi>
                                <incl uom=""""></incl>
                            </trajectoryStation>";

            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn, optionsIn: OptionsIn.ReturnElements.Requested);
            DevKit.AssertNames(result);

            result.TrajectoryStation.ForEach(s => DevKit.AssertRequestedElements(s, new[] { "md", "azi", "incl", "Uid" }));
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Structural_Range()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(15, 30);
            DevKit.AddAndAssert(Trajectory);

            var startRange = 30;
            var endRange = 35;
            var queryIn = @"<mdMn uom=""m"">30</mdMn>
                            <mdMx uom=""m"">35</mdMx>
                            <trajectoryStation uid="""">
                                <md uom=""""></md>
                                <azi uom=""""></azi>
                                <incl uom=""""></incl>
                            </trajectoryStation>";

            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn, optionsIn: OptionsIn.ReturnElements.Requested);

            DevKit.AssertNames(result);
            result.TrajectoryStation.ForEach(s => DevKit.AssertRequestedElements(s, new[] { "md", "azi", "incl", "Uid" }));

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= startRange && s.MD.Value <= endRange).ToList();
            Assert.IsNotNull(stations);
            Assert.AreEqual(stations.Count, result.TrajectoryStation.Count);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(3, 1);
            DevKit.AddAndAssert(Trajectory);
            var firstStation = Trajectory.TrajectoryStation.First();
            var secondStation = Trajectory.TrajectoryStation.First(s => s.Uid == "sta-2");

            var queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"/>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation }, result.TrajectoryStation);

            queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"/><trajectoryStation uid=\"{secondStation.Uid}\"/>";
            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation, secondStation }, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type()
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(3, 1);
            DevKit.AddAndAssert(Trajectory);
            var firstStation = Trajectory.TrajectoryStation.First();

            var queryIn = "<trajectoryStation><typeTrajStation>tie in point</typeTrajStation></trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation }, result.TrajectoryStation);

            queryIn = @"<trajectoryStation><typeTrajStation>magnetic MWD</typeTrajStation></trajectoryStation>
                        <trajectoryStation><typeTrajStation>tie in point</typeTrajStation></trajectoryStation>";

            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type()
        {
            AddParents();

            WitsmlSettings.MaxStationCount = 5;

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(10, 10);
            DevKit.AddAndAssert(Trajectory);
            var firstStation = Trajectory.TrajectoryStation.First();
            var secondStation = Trajectory.TrajectoryStation.Find(s => s.Uid == "sta-2");

            var queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"> <typeTrajStation>tie in point</typeTrajStation> </trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            Assert.AreEqual(1, result.TrajectoryStation.Count);
            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation }, result.TrajectoryStation);

            queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"> <typeTrajStation>magnetic MWD</typeTrajStation> </trajectoryStation>";
            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            Assert.AreEqual(0, result.TrajectoryStation.Count);

            queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"> <typeTrajStation>tie in point</typeTrajStation> </trajectoryStation>" +
                        $"<trajectoryStation uid=\"{secondStation.Uid}\"> <typeTrajStation>magnetic MWD</typeTrajStation> </trajectoryStation>";
            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation, secondStation }, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Md_Value()
        {
            AddParents();

            WitsmlSettings.MaxStationCount = 5;

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(10, 10);
            DevKit.AddAndAssert(Trajectory);
            var lastStation = Trajectory.TrajectoryStation.Last();

            var queryIn = $"<trajectoryStation> <md uom=\"m\">{lastStation.MD.Value}</md></trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { lastStation }, result.TrajectoryStation);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Location()
        {
            AddParents();

            WitsmlSettings.MaxStationCount = 5;

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(35, 15);
            DevKit.AddAndAssert(Trajectory);

            var lastStation = Trajectory.TrajectoryStation.Last();

            var queryIn = $"<trajectoryStation><location><wellCRS>{lastStation.Location.First().WellCRS.Value}</wellCRS></location></trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { lastStation }, result.TrajectoryStation);

            var locations = result.TrajectoryStation
                .Where(x => x.Location != null)
                .SelectMany(x => x.Location)
                .ToList();
            Assert.AreEqual(1, locations.Count, "trajectoryStation/location count");

            var location = locations.FirstOrDefault();
            Assert.IsNotNull(location, "location");

            Assert.AreEqual(lastStation.Location.First().WellCRS.Value, location.WellCRS.Value, "trajectoryStation/location/wellCRS");
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