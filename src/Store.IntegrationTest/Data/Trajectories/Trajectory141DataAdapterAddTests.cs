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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    [TestClass]
    public partial class Trajectory141DataAdapterAddTests : Trajectory141TestBase
    {
        [TestMethod]
        public void Trajectory141DataAdapter_AddToStore_AddTrajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            DevKit.GetAndAssert(Trajectory);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_AddToStore_AddTrajectory_With_Stations()
        {
            Trajectory141DataAdapter_AddToStore_AddTrajectory_With_Stations(5);
            TestReset(5);
            Trajectory141DataAdapter_AddToStore_AddTrajectory_With_Stations(10);
        }

        [TestMethod, Description("Tests you cannot do AddToStore with more data nodes than specified in Trajectory MaxDataNodes")]
        public void Trajectory141DataAdapter_AddToStore_Error_456_Exceed_MaxDataNodes()
        {
            // Add well and wellbore
            AddParents();
            var maxDataNodes = 5;
            WitsmlSettings.TrajectoryMaxDataNodesAdd = maxDataNodes;

            // Add trajectory with exceeding amount of stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxDataNodes + 1, 0);
            DevKit.AddAndAssert(Trajectory, ErrorCodes.MaxDataExceeded);

            // Add trajetory with max allowed stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(maxDataNodes, 0);
            DevKit.AddAndAssert(Trajectory);
        }

        private void Trajectory141DataAdapter_AddToStore_AddTrajectory_With_Stations(int numberOfStations)
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory with stations         
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numberOfStations, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
        }
    }
}
