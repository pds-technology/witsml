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

            // Assert null for get
            DevKit.GetAndAssert(Trajectory, false);
        }

        [TestMethod]
        public void Trajectory141DataAdapter_DeleteFromStore_Partial_Delete_Trajectory_Header()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.MagDeclUsed = new PlaneAngleMeasure { Uom = PlaneAngleUom.dega, Value = 20.0 };
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNotNull(result.MagDeclUsed);

            // Delete trajectory
            var delete = string.Format(DevKit131Aspect.BasicTrajectoryXmlTemplate, Trajectory.Uid, Trajectory.UidWell,
                Trajectory.UidWellbore, "<magDeclUsed />");
            DevKit.DeleteAndAssert(ObjectTypes.Trajectory, delete);

            // Assert null for get
            result = DevKit.GetAndAssert(Trajectory);
            Assert.IsNull(result.MagDeclUsed);
        }
    }
}