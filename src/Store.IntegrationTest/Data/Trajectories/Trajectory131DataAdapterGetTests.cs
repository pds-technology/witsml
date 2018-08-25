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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Data.Trajectories;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory131DataAdapterGetTests
    /// </summary>
    [TestClass]
    public partial class Trajectory131DataAdapterGetTests : Trajectory131TestBase
    {
        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_All()
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
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All()
        {
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All(4);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Id_Only()
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
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Default()
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
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Requested()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var queryIn = string.Format(BasicXMLTemplate, Trajectory.UidWell, Trajectory.UidWellbore, Trajectory.Uid, "<serviceCompany />");
            var results = DevKit.Query<TrajectoryList, Trajectory>(ObjectTypes.Trajectory, queryIn, null, OptionsIn.ReturnElements.Requested);
            var result = results.FirstOrDefault();

            Assert.IsNotNull(result);
            DevKit.AssertNames(result);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested()
        {
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested(4);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only()
        {
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only(4);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only()
        {
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only(4);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only(15);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min()
        {
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min(20);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min(20);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max()
        {
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max(20);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max(20);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Filters_Results_With_No_Data()
        {
            Trajectory131DataAdapter_GetFromStore_Filters_Results_With_No_Data(20);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Filters_Results_With_No_Data(20);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering()
        {
            Trajectory131DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering(20);
            TestReset(10);
            Trajectory131DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering(20);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid()
        {
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid(5);
            TestReset(5);
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid(10);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type()
        {
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type(5);
            TestReset(5);
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type(10);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type()
        {
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type(5);
            TestReset(5);
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type(10);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uom()
        {
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uom(5);
            TestReset(5);
            Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uom(10);
        }

        private void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory);

            DevKit.AssertNames(result, Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, "<serviceCompany /><trajectoryStation />", optionsIn: OptionsIn.ReturnElements.Requested);

            DevKit.AssertNames(result);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation, true);
        }

        private void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory, optionsIn: OptionsIn.ReturnElements.DataOnly);

            DevKit.AssertNames(result);
            Assert.IsNull(result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation, true);
        }

        private void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory, optionsIn: OptionsIn.ReturnElements.DataOnly);

            DevKit.AssertNames(result);
            Assert.IsNull(result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            const int start = 15;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = start }
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            const int start = 15;
            const int end = 20;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = start },
                MDMax = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = end }
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start && s.MD.Value <= end).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Filters_Results_With_No_Data(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Query end range before the trajectory structure
            var end = 9;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMax = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = end }
            };
            DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true, isNotNull: false);

            // Query start range after the trajectory structure
            var start = 100;
            query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = start },
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
                MDMin = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = start },
                MDMax = new MeasuredDepthCoord { Uom = Trajectory131Generator.MdUom, Value = end }
            };
            DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true, isNotNull: false);
        }

        private void Trajectory131DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 5);
            DevKit.AddAndAssert(Trajectory);

            const int start = 8;
            var query = new Trajectory
            {
                Uid = Trajectory.Uid,
                UidWell = Trajectory.UidWell,
                UidWellbore = Trajectory.UidWellbore,
                MDMin = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = start },
                TrajectoryStation = new List<TrajectoryStation> { new TrajectoryStation { MD = new MeasuredDepthCoord { Value = 6, Uom = MeasuredDepthUom.m } } }
            };

            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 7);
            DevKit.AddAndAssert(Trajectory);
            var firstStation = Trajectory.TrajectoryStation.First();
            var fifthStation = Trajectory.TrajectoryStation.First(s => s.Uid == "sta-5");

            var queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"/>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            Assert.AreEqual(1, result.TrajectoryStation.Count);
            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation }, result.TrajectoryStation);

            queryIn = $"<trajectoryStation uid=\"{firstStation.Uid}\"/><trajectoryStation uid=\"{fifthStation.Uid}\"/>";
            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation, fifthStation }, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10);
            DevKit.AddAndAssert(Trajectory);
            var firstStation = Trajectory.TrajectoryStation.First();

            var queryIn = "<trajectoryStation><typeTrajStation>tie in point</typeTrajStation></trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            Assert.AreEqual(1, result.TrajectoryStation.Count);
            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation }, result.TrajectoryStation);

            queryIn = @"<trajectoryStation><typeTrajStation>magnetic MWD</typeTrajStation></trajectoryStation>
                        <trajectoryStation><typeTrajStation>tie in point</typeTrajStation></trajectoryStation>";

            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        private void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10);
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

        private void Trajectory131DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uom(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10);
            DevKit.AddAndAssert(Trajectory);
            var lastStation = Trajectory.TrajectoryStation.Last();

            var queryIn = $"<trajectoryStation> <md uom=\"m\">{lastStation.MD.Value}</md></trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { lastStation }, result.TrajectoryStation);
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
                Assert.AreEqual(station.DateTimeStn.ToUnixTimeMicroseconds(), result.DateTimeStn.ToUnixTimeMicroseconds());

                if (!fullStation)
                    continue;

                Assert.AreEqual(station.Mtf?.Value, result.Mtf?.Value);
                Assert.AreEqual(station.MDDelta?.Value, result.MDDelta?.Value);
                Assert.AreEqual(station.StatusTrajStation, result.StatusTrajStation);
            }
        }
    }
}
