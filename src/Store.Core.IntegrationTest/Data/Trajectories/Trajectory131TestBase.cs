//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory131TestBase
    /// </summary>
    public partial class Trajectory131TestBase
    {
        partial void BeforeEachTest()
        {
            Trajectory.ServiceCompany = "Service Company T";
        }

        partial void AfterEachTest()
        {
            WitsmlSettings.MaxStationCount = DevKitAspect.DefaultMaxStationCount;
            WitsmlSettings.TrajectoryMaxDataNodesGet = DevKitAspect.DefaultTrajectoryMaxDataNodesGet;
            WitsmlSettings.TrajectoryMaxDataNodesAdd = DevKitAspect.DefaultTrajectoryMaxDataNodesAdd;
            WitsmlSettings.TrajectoryMaxDataNodesUpdate = DevKitAspect.DefaultTrajectoryMaxDataNodesUpdate;
            WitsmlSettings.TrajectoryMaxDataNodesDelete = DevKitAspect.DefaultTrajectoryMaxDataNodesDelete;
            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = DevKitAspect.DefaultTrajectoryGrowingTimeoutPeriod;
        }

        public void TestReset(int maxStationCount)
        {
            TestCleanUp();
            TestSetUp();
            WitsmlSettings.MaxStationCount = maxStationCount;
        }
    }
}
