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

namespace PDS.Witsml.Server.Data.Trajectories
{
    /// <summary>
    /// Trajectory131DataAdapterGetTests
    /// </summary>
    public partial class Trajectory131DataAdapterGetTests
    {
        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_All()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.ServiceCompany = "Service Company T";
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var query = DevKit.CreateTrajectory(Trajectory.Uid, null, Trajectory.UidWell, null, Trajectory.UidWellbore, null);
            var result = DevKit.QueryAndAssert<TrajectoryList, Trajectory>(query);
            AssertNames(result, Trajectory);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
            Assert.IsNotNull(result.CommonData);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Id_Only()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.ServiceCompany = "Service Company T";
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var query = DevKit.CreateTrajectory(Trajectory.Uid, null, Trajectory.UidWell, null, Trajectory.UidWellbore, null);
            var result = DevKit.QueryAndAssert<TrajectoryList, Trajectory>(query, optionsIn: OptionsIn.ReturnElements.IdOnly);
            AssertNames(result, Trajectory);
            Assert.IsNull(result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Default()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.ServiceCompany = "Service Company T";
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var query = DevKit.CreateTrajectory(Trajectory.Uid, null, Trajectory.UidWell, null, Trajectory.UidWellbore, null);
            var result = DevKit.QueryAndAssert<TrajectoryList, Trajectory>(query, optionsIn: string.Empty);
            AssertNames(result);
            Assert.IsNull(result.ServiceCompany);
        }

        [TestMethod]
        public void Trajectory131DataAdapter_GetFromStore_Can_Retrieve_Header_Return_Elements_Requested()
        {
            // Add well and wellbore
            AddParents();

            // Add trajectory without stations
            Trajectory.ServiceCompany = "Service Company T";
            DevKit.AddAndAssert(Trajectory);

            // Get trajectory
            var queryIn = string.Format(DevKit131Aspect.BasicTrajectoryXmlTemplate, Trajectory.Uid, Trajectory.UidWell, Trajectory.UidWellbore, "<serviceCompany />");
            var results = DevKit.Query<TrajectoryList, Trajectory>(ObjectTypes.Trajectory, queryIn, null, OptionsIn.ReturnElements.Requested);
            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);
            AssertNames(result);
            Assert.AreEqual(Trajectory.ServiceCompany, result.ServiceCompany);
        }

        private void AssertNames(Trajectory result, Trajectory entity = null)
        {
            if (entity != null)
            {
                Assert.AreEqual(entity.Name, result.Name);
                Assert.AreEqual(entity.NameWell, result.NameWell);
                Assert.AreEqual(entity.NameWellbore, result.NameWellbore);
            }
            else
            {
                Assert.IsNull(result.Name);
                Assert.IsNull(result.NameWell);
                Assert.IsNull(result.NameWellbore);
            }
        }
    }
}