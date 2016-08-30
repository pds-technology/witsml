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

using System.Linq;
using Energistics.DataAccess.WITSML131;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Trajectories
{
    [TestClass]
    public class Trajectory131DataAdapterAddTests
    {
        private DevKit131Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Trajectory _trajectory;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit131Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version131.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _trajectory = _devKit.CreateTrajectory(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.MaxStationCount = DevKitAspect.DefaultMaxStationCount;
        }

        [TestMethod]
        public void Trajectory141DataAdapter_AddToStore_Add_Trajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            _devKit.AddAndAssert(_trajectory);

            // Get trajectory
            _devKit.GetOneAndAssert(_trajectory);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_AddToStore_Add_Trajectory_With_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations         
            _trajectory.TrajectoryStation = _devKit.TrajectoryStations(4, 0);
            _devKit.AddAndAssert(_trajectory);

            // Get trajectory
            var result = _devKit.GetOneAndAssert(_trajectory);
            Assert.AreEqual(_trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
