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
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Data.Trajectories;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
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
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All(5);
            TestReset(10);
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All(15);
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
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested(5);
            TestReset(10);
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested(15);
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
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only(5);
            TestReset(10);
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only(15);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only(5);
            TestReset(10);
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only(15);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min(20);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min(25);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max(20);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max(25);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filters_Results_With_No_Data()
        {
            Trajectory141DataAdapter_GetFromStore_Filters_Results_With_No_Data(20);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Filters_Results_With_No_Data(25);
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
        public void Trajectory141DataAdapter_GetFromStore_Results_Are_Limited_To_MaxReturnNodes_OptionsIn()
        {
            Trajectory141DataAdapter_GetFromStore_Results_Are_Limited_To_MaxReturnNodes_OptionsIn(10);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Results_Are_Limited_To_MaxReturnNodes_OptionsIn(25);
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
            Trajectory141DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering(5);
            TestReset(10);
            Trajectory141DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering(15);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested(5);
            TestReset(10);
            Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested(15);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Structural_Range()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Structural_Range(15);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Structural_Range(25);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Empty_Structural_Range()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Empty_Structural_Range(15);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Empty_Structural_Range(25);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Can_Get_Requested_With_Empty_Structural_Range()
        {
            Trajectory141DataAdapter_GetFromStore_Can_Get_Requested_With_Empty_Structural_Range(15);
            TestReset(20);
            Trajectory141DataAdapter_GetFromStore_Can_Get_Requested_With_Empty_Structural_Range(25);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid()
        {
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid(3);
            TestReset(5);
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid(6);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type()
        {
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type(3);
            TestReset(5);
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type(6);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type()
        {
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type(3);
            TestReset(5);
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type(10);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Md_Value()
        {
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Md_Value(3);
            TestReset(5);
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Md_Value(10);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Location()
        {
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Location(3);
            TestReset(5);
            Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Location(35);
        }

        [TestMethod, Description("Implements test93 from the Energistics certification tests")]
        public void Trajectory141DataAdapter_GetFromStore_with_station_location_only()
        {
            Trajectory141DataAdapter_GetFromStore_with_station_location_only(35);
            TestReset(5);
            Trajectory141DataAdapter_GetFromStore_with_station_location_only(35);
        }

        private void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Structural_Range(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 30);
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
            Assert.IsNotNull(result.MDMin);
            Assert.IsNotNull(result.MDMax);
            Assert.AreEqual(30, result.MDMin.Value);
            Assert.AreEqual(35, result.MDMax.Value);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= startRange && s.MD.Value <= endRange).ToList();
            Assert.IsNotNull(stations);
            Assert.AreEqual(stations.Count, result.TrajectoryStation.Count);
            result.TrajectoryStation.ForEach(s => DevKit.AssertRequestedElements(s, new[] { "md", "azi", "incl", "Uid" }));
        }

        private void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested_With_Empty_Structural_Range(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 30);
            DevKit.AddAndAssert(Trajectory);

            var queryIn = @"<mdMn uom=""""></mdMn>
                            <mdMx uom=""""></mdMx>
                            <trajectoryStation uid="""">
                                <md uom=""""></md>
                                <azi uom=""""></azi>
                                <incl uom=""""></incl>
                            </trajectoryStation>";

            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn, optionsIn: OptionsIn.ReturnElements.Requested);

            DevKit.AssertNames(result);
            Assert.IsNotNull(result.MDMin);
            Assert.IsNotNull(result.MDMax);

            var stations = Trajectory.TrajectoryStation;
            Assert.IsNotNull(stations);
            Assert.IsTrue(result.TrajectoryStation.Count >= 15);
            result.TrajectoryStation.ForEach(s => DevKit.AssertRequestedElements(s, new[] { "md", "azi", "incl", "Uid" }));
        }

        private void Trajectory141DataAdapter_GetFromStore_Can_Get_Requested_With_Empty_Structural_Range(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 30);
            DevKit.AddAndAssert(Trajectory);

            var emptyString = string.Empty;
            var requestedQuery = @" 
                                    <name />
                                    <objectGrowing />
                                    <commonData />
                                    <mdMn uom=""""></mdMn> 
                                    <mdMx uom=""""></mdMx>";

            var queryIn = string.Format(BasicXMLTemplate, emptyString, emptyString, emptyString, requestedQuery);

            var results = DevKit.Query<TrajectoryList, Trajectory>(ObjectTypes.Trajectory, queryIn, null, OptionsIn.ReturnElements.Requested);
            Assert.IsTrue(results.Count > 0);

            var trajectoryStation = results.FirstOrDefault(t => t.Uid == Trajectory.Uid);
            Assert.IsNotNull(trajectoryStation);
            Assert.AreEqual(0, trajectoryStation.TrajectoryStation.Count);
            DevKit.AssertRequestedElements(trajectoryStation, new[] { "mdMin", "mdMax", "uid", "uidWell", "uidWellbore", "name", "objectGrowing", "commonData" });
            Assert.IsNotNull(trajectoryStation.MDMax);
            Assert.IsNotNull(trajectoryStation.MDMin);
            Assert.IsNotNull(trajectoryStation.Name);
            Assert.IsNotNull(trajectoryStation.CommonData);
            Assert.IsNotNull(trajectoryStation.ObjectGrowing);
        }

        private void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 1);
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

        private void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Type(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 1);
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

        private void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Uid_And_Type(int numberOfStations)
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

        private void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Md_Value(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 10);
            DevKit.AddAndAssert(Trajectory);
            var firstStation = Trajectory.TrajectoryStation.First();
            var lastStation = Trajectory.TrajectoryStation.Last();

            var queryIn = $"<trajectoryStation> <md uom=\"m\">{lastStation.MD.Value}</md></trajectoryStation>";
            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { lastStation }, result.TrajectoryStation);

            firstStation.MD = lastStation.MD;
            DevKit.UpdateAndAssert(Trajectory);

            queryIn = $"<trajectoryStation> <md uom=\"m\">{lastStation.MD.Value}</md></trajectoryStation>";
            result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn);

            AssertTrajectoryStations(new List<TrajectoryStation> { firstStation, lastStation }, result.TrajectoryStation);
        }

        private void Trajectory141DataAdapter_GetFromStore_Filter_Recurring_Element_By_Station_Location(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 15);
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

        private void Trajectory141DataAdapter_GetFromStore_with_station_location_only(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 15);
            DevKit.AddAndAssert(Trajectory);

            var objectTemplate = DevKit.CreateQuery<TrajectoryList, Trajectory>(Trajectory);

            var results = DevKit.Query<TrajectoryList, Trajectory>(ObjectTypes.Trajectory, objectTemplate.ToString(), null, OptionsIn.ReturnElements.StationLocationOnly);
            Assert.IsNotNull(results);

            var trajectory = results.FirstOrDefault();
            Assert.IsNotNull(trajectory);

            // Assert that only the expected elements and attributes are on the trajectory
            DevKit.AssertRequestedElements(trajectory, new[] { "uid", "uidWell", "uidWellbore", "trajectoryStation" });

            // Assert that only the expected elements and attributes are on the trajectoryStation
            trajectory.TrajectoryStation.ForEach(
                x => DevKit.AssertRequestedElements(x, new[] { "uid", "md", "tvd", "incl", "azi", "location", "dateTimeStn", "typeTrajStation" }));
        }


        private void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_All(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();
            // Add trajectory with stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(Trajectory);

            DevKit.AssertNames(result, Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);

            AssertTrajectoryStations(Trajectory.TrajectoryStation, result.TrajectoryStation);
        }

        private void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Requested(int numberOfStations)
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

        private void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Data_Only(int numberOfStations)
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

        private void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_Return_Elements_Station_Location_Only(int numberOfStations)
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

        private void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min(int numberOfStations)
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
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start }
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }


        private void Trajectory141DataAdapter_GetFromStore_Can_Retrieve_Data_By_Md_Min_Max(int numberOfStations)
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
                MDMin = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = start },
                MDMax = new MeasuredDepthCoord { Uom = Trajectory141Generator.MdUom, Value = end }
            };
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(query, queryByExample: true);

            DevKit.AssertNames(result, Trajectory);

            var stations = Trajectory.TrajectoryStation.Where(s => s.MD.Value >= start && s.MD.Value <= end).ToList();
            AssertTrajectoryStations(stations, result.TrajectoryStation);
        }

        private void Trajectory141DataAdapter_GetFromStore_Filters_Results_With_No_Data(int numberOfStations)
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

        private void Trajectory141DataAdapter_GetFromStore_Query_Uses_Structural_Range_Value_And_Not_Station_MD_For_Filtering(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 5);
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


        private void Trajectory141DataAdapter_GetFromStore_Can_Filter_Recurring_Elements_Requested(int numberOfStations)
        {
            AddParents();

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 20);
            DevKit.AddAndAssert(Trajectory);

            var queryIn = @"<trajectoryStation uid="""">
                                <md uom=""""></md>
                                <azi uom=""""></azi>
                                <incl uom=""""></incl>
                            </trajectoryStation>";

            var result = DevKit.GetAndAssertWithXml(BasicXMLTemplate, Trajectory, queryIn, optionsIn: OptionsIn.ReturnElements.Requested);
            DevKit.AssertNames(result);

            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            result.TrajectoryStation.ForEach(s => DevKit.AssertRequestedElements(s, new[] { "md", "azi", "incl", "Uid" }));
        }

        private void Trajectory141DataAdapter_GetFromStore_Results_Are_Limited_To_MaxReturnNodes_OptionsIn(int numberOfStations)
        {
            AddParents();
            var maxDataNodes = 5;

            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxDataNodes + numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            short errorCode;
            var result = DevKit.QueryWithErrorCode<TrajectoryList, Trajectory>(
                new Trajectory() { Uid = Trajectory.Uid, UidWell = Trajectory.UidWell, UidWellbore = Trajectory.UidWellbore },
                out errorCode,
                ObjectTypes.Trajectory,
                null,
                OptionsIn.Join(OptionsIn.ReturnElements.All, OptionsIn.MaxReturnNodes.Eq(maxDataNodes)));

            Assert.AreEqual((short)ErrorCodes.ParialSuccess, errorCode, "Returning partial data.");
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);

            var trajectory = result[0];
            Assert.IsNotNull(trajectory);
            Assert.AreEqual(maxDataNodes, trajectory.TrajectoryStation.Count);
        }

        #region Helper Methods

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
        #endregion
    }
}
