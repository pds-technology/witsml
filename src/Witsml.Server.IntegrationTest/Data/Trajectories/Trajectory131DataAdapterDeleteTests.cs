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

using System;
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Trajectory131DataAdapterDeleteTests
    /// </summary>
    public partial class Trajectory131DataAdapterDeleteTests
    {

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Delete_Full_Trajectory()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(4, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            DevKit.GetAndAssert(Trajectory);

            // Delete trajectory
            var delete = DevKit.CreateQuery(Trajectory);
            DevKit.DeleteAndAssert<TrajectoryList, Trajectory>(delete);

            // Assert delete results
            DevKit.GetAndAssert(Trajectory, false);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.MagDeclUsed = new PlaneAngleMeasure { Uom = PlaneAngleUom.dega, Value = 20.0 };
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNotNull(result.MagDeclUsed);

            // Delete trajectory header element
            var delete = string.Format(DevKit131Aspect.BasicTrajectoryXmlTemplate, Trajectory.Uid, Trajectory.UidWell,
                Trajectory.UidWellbore, "<magDeclUsed />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNull(result.MagDeclUsed);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_All_Stations()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(4, 0);
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);

            // Delete all trajectory stations
            var delete = string.Format(DevKit131Aspect.BasicTrajectoryXmlTemplate, Trajectory.Uid, Trajectory.UidWell,
                Trajectory.UidWellbore, "<trajectoryStation />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            Assert.IsFalse(result.TrajectoryStation.Any());
        }

        [TestMethod]
        public void Trajectory131DataAdapter_DeleteFromStore_Partial_Delete_Station_Items()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.TrajectoryStation = DevKit.TrajectoryStations(4, 0, inCludeExtra: true);
            DevKit.AddAndAssert(Trajectory);

            var stations = Trajectory.TrajectoryStation;
            var station1 = stations[0];
            var station2 = stations[1];
            var station3 = stations[2];

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.AreEqual(Trajectory.TrajectoryStation.Count, result.TrajectoryStation.Count);
            var resultStation3 = stations.FirstOrDefault(s => s.Uid == station3.Uid);
            Assert.IsNotNull(resultStation3);
            Assert.IsNotNull(resultStation3.MDDelta);

            // Delete trajectory stations and elements
            var delete = "<trajectoryStation uid=\"" + station1.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station2.Uid + "\" />" + Environment.NewLine
                         + "<trajectoryStation uid=\"" + station3.Uid + "\"><mdDelta /></trajectoryStation>";
            var queryIn = string.Format(DevKit131Aspect.BasicTrajectoryXmlTemplate, Trajectory.Uid, Trajectory.UidWell,
                Trajectory.UidWellbore, delete);
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, queryIn);

            // Assert delete results
            result = DevKit.GetAndAssert(Trajectory);
            stations = result.TrajectoryStation;
            Assert.AreEqual(2, stations.Count);
            Assert.IsNull(stations.FirstOrDefault(s => s.Uid == station1.Uid));
            Assert.IsNull(stations.FirstOrDefault(s => s.Uid == station2.Uid));
            resultStation3 = stations.FirstOrDefault(s => s.Uid == station3.Uid);
            Assert.IsNotNull(resultStation3);
            Assert.IsNull(resultStation3.MDDelta);
        }
    }
}