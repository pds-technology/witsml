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

using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;

namespace PDS.WITSMLstudio.Store.Data.Trajectories
{
    /// <summary>
    /// Trajectory141EtpTests
    /// </summary>
    public partial class Trajectory141EtpTests
    {
        [TestMethod, Description("Tests that 141 Trajectory Data can be added when Compatibility Setting TrajectoryAllowPutObjectWithData is True")]
        public async Task Trajectory141_PutObject_Can_Add_Trajectory_Data_With_TrajectoryAllowPutObjectWithData_True()
        {
            const int numStations = 10;
            const bool allowPutData = true;

            await Trajectory141_PutObject_Can_Add_Trajectory_Data_With_TrajectoryAllowPutObjectWithData(numStations, allowPutData);
        }

        [TestMethod, Description("Tests that 141 Trajectory Data cannot be added when Compatibility Setting TrajectoryAllowPutObjectWithData is False")]
        public async Task Trajectory141_PutObject_Can_Add_Trajectory_Data_With_TrajectoryAllowPutObjectWithData_False()
        {
            const int numStations = 10;
            const bool allowPutData = false;

            await Trajectory141_PutObject_Can_Add_Trajectory_Data_With_TrajectoryAllowPutObjectWithData(numStations, allowPutData);
        }

        private async Task Trajectory141_PutObject_Can_Add_Trajectory_Data_With_TrajectoryAllowPutObjectWithData(int numStations, bool allowPutData)
        {
            AddParents();

            // Allow for Log data to be saved during a Put
            CompatibilitySettings.TrajectoryAllowPutObjectWithData = allowPutData;

            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Trajectory.GetUri();
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(numStations, 0);

            var dataObject = CreateDataObject<TrajectoryList, Trajectory>(uri, Trajectory);

            // Put Object
            await PutAndAssert(handler, dataObject);

            // Get Object
            var result = DevKit.GetAndAssert<TrajectoryList, Trajectory>(DevKit.CreateTrajectory(Trajectory));

            // Verify that the Trajectory was saved.
            Assert.IsNotNull(result);

            // Verify the number of Stations saved.
            var stationsExpected = allowPutData ? numStations : 0;
            Assert.IsNotNull(result.TrajectoryStation);
            Assert.AreEqual(stationsExpected, result.TrajectoryStation.Count);
        }
    }
}
