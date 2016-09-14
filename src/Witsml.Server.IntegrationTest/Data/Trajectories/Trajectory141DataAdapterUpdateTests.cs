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

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Trajectory141DataAdapterUpdateTests
    /// </summary>
    public partial class Trajectory141DataAdapterUpdateTests
    {
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
            var xmlIn = string.Format(DevKit141Aspect.BasicTrajectoryXmlTemplate, Trajectory.Uid, Trajectory.UidWell, Trajectory.UidWellbore, content);
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
                TrajectoryStation = new List<TrajectoryStation> {station1Update, station5}
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
    }
}