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

using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.CascadedDelete
{
    [TestClass]
    public class CascadedDelete141Tests : MultiObject141TestBase
    {
        [TestMethod]
        public void CascadedDelete141Tests_Can_Delete_Well_With_Empty_Wellbores()
        {
            DevKit.AddAndAssert(Well);

            // Number of objects to generate
            var numOfObjects = 5;

            // Add 5 empty wellbores
            for (var i = 0; i < numOfObjects; i++)
            {
                var wellbore = new Wellbore() { Uid = DevKit.Uid(), UidWell = Well.Uid, Name = DevKit.Name(), NameWell = Well.Name };
                DevKit.AddAndAssert(wellbore);
            }

            // Delete well with cascadedDelete options in
            var deleteWell = new Well { Uid = Well.Uid };
            var result = DevKit.Delete<WellList, Well>(deleteWell, ObjectTypes.Well, string.Empty, OptionsIn.CascadedDelete.True);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Result, 1);

            // Ensure well does not exist anymore
            DevKit.GetAndAssert(Well, false);
        }

        [TestMethod]
        public void CascadedDelete141Tests_Can_Delete_Well_With_Populated_Wellbores()
        {
            DevKit.AddAndAssert(Well);

            // Number of objects to generate
            var numOfObjects = 5;

            // Create logs
            var logs = DevKit.GenerateLogs(Well.Uid, Well.Name, LogIndexType.measureddepth, numOfObjects);

            // Create trajectories
            var trajectories = DevKit.GenerateTrajectories(Well.Uid, Well.Name, numOfObjects);

            // Add 5 wellbores with data objects
            for (var i = 0; i < numOfObjects; i++)
            {
                var wellbore = new Wellbore() { Uid = DevKit.Uid(), UidWell = Well.Uid, Name = DevKit.Name(), NameWell = Well.Name };
                DevKit.AddAndAssert(wellbore);
                DevKit.AddListOfLogsToWellbore(logs, wellbore);
                DevKit.AddListOfTrajectoriesToWellbore(trajectories, wellbore);
            }

            // Delete well with cascadedDelete options in
            var deleteWell = new Well { Uid = Well.Uid };
            var result = DevKit.Delete<WellList, Well>(deleteWell, ObjectTypes.Well, string.Empty, OptionsIn.CascadedDelete.True);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Result, 1);

            // Ensure well does not exist anymore
            DevKit.GetAndAssert(Well, false);
        }

        [TestMethod]
        public void CascadedDelete141Tests_Can_Delete_Populated_Wellbores()
        {
            DevKit.AddAndAssert(Well);

            // Number of objects to generate
            var numOfObjects = 5;

            // Create logs
            var logs = DevKit.GenerateLogs(Well.Uid, Well.Name, LogIndexType.measureddepth, numOfObjects);

            // Create trajectories
            var trajectories = DevKit.GenerateTrajectories(Well.Uid, Well.Name, numOfObjects);

            // Add 5 wellbores with data objects
            for (var i = 0; i < numOfObjects; i++)
            {
                var wellbore = new Wellbore() { Uid = DevKit.Uid(), UidWell = Well.Uid, Name = DevKit.Name(), NameWell = Well.Name };
                DevKit.AddAndAssert(wellbore);
                DevKit.AddListOfLogsToWellbore(logs, wellbore);
                DevKit.AddListOfTrajectoriesToWellbore(trajectories, wellbore);
            }

            // Delete well with cascadedDelete options in
            var wellbores = DevKit.Get<WellboreList, Wellbore>(DevKit.List(new Wellbore() { UidWell = Well.Uid }),
                ObjectTypes.Wellbore, string.Empty, OptionsIn.ReturnElements.IdOnly);

            Assert.IsNotNull(wellbores);

            var wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(wellbores.XMLout);
            Assert.IsNotNull(wellboreList);
            Assert.AreEqual(5, wellboreList.Wellbore.Count);

            // Delete each wellbore individually
            wellboreList.Wellbore.ForEach(x =>
            {
                var deleteWellbore = new Wellbore { Uid = x.Uid, UidWell = x.UidWell };
                var result = DevKit.Delete<WellboreList, Wellbore>(deleteWellbore, ObjectTypes.Wellbore, string.Empty, OptionsIn.CascadedDelete.True);
                Assert.IsNotNull(result);
                Assert.AreEqual(result.Result, 1);

                // Ensure wellbore does not exist anymore
                DevKit.GetAndAssert(deleteWellbore, false);
            });

            // Get all wellbores remaining under well
            wellbores = DevKit.Get<WellboreList, Wellbore>(DevKit.List(new Wellbore() { UidWell = Well.Uid }),
                ObjectTypes.Wellbore, string.Empty, OptionsIn.ReturnElements.IdOnly);
            Assert.IsNotNull(wellbores);

            wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(wellbores.XMLout);
            Assert.IsNotNull(wellboreList);
            Assert.AreEqual(0, wellboreList.Wellbore.Count);
        }
    }
}
