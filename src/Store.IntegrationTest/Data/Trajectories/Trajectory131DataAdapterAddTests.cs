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

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    [TestClass]
    public partial class Trajectory131DataAdapterAddTests : Trajectory131TestBase
    {
        [TestMethod]
        public void Trajectory131DataAdapter_AddToStore_AddTrajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            DevKit.GetAndAssert(Trajectory);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_AddToStore_AddTrajectory_With_Stations()
        {
            Trajectory131DataAdapter_AddToStore_AddTrajectory_With_Stations(4);
            TestReset(10);
            Trajectory131DataAdapter_AddToStore_AddTrajectory_With_Stations(15);
        }

        private void Trajectory131DataAdapter_AddToStore_AddTrajectory_With_Stations(int numberOfStations)
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
