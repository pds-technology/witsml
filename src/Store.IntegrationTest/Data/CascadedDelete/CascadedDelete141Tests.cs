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

using System.Collections.Generic;
using System.Threading.Tasks;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Store.Data.CascadedDelete
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

        [TestMethod]
        public async Task CascadedDelete141Tests_Can_Parallel_Delete_Populated_Wellbores()
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
            Assert.AreEqual(numOfObjects, wellboreList.Wellbore.Count);

            // Delete each wellbore in parallel
            var taskList = new List<Task>();
            wellboreList.Wellbore.ForEach(x =>
            {
                taskList.Add(new Task(() =>
                {
                    var deleteWellbore = new Wellbore { Uid = x.Uid, UidWell = x.UidWell };
                    var result = DevKit.Delete<WellboreList, Wellbore>(deleteWellbore, ObjectTypes.Wellbore,
                        string.Empty, OptionsIn.CascadedDelete.True);
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.Result, 1);

                    // Ensure wellbore does not exist anymore
                    DevKit.GetAndAssert(deleteWellbore, false);
                }));
            });

            taskList.ForEach(x => x.Start());
            await Task.WhenAll(taskList);

            // Get all wellbores remaining under well
            wellbores = DevKit.Get<WellboreList, Wellbore>(DevKit.List(new Wellbore() { UidWell = Well.Uid }),
                ObjectTypes.Wellbore, string.Empty, OptionsIn.ReturnElements.IdOnly);
            Assert.IsNotNull(wellbores);

            wellboreList = EnergisticsConverter.XmlToObject<WellboreList>(wellbores.XMLout);
            Assert.IsNotNull(wellboreList);
            Assert.AreEqual(0, wellboreList.Wellbore.Count);
        }

        [TestMethod]
        public async Task CascadedDelete141Tests_Can_Parallel_Delete_Well_With_Populated_Wellbores()
        {
            // Number of objects to generate
            var numOfObjects = 5;

            var wellList = new List<Well>();

            for (var i = 0; i < numOfObjects; i++)
            {
                wellList.Add(new Well() { Uid = DevKit.Uid(), Name = DevKit.Name(), TimeZone = "-06:00" });
            }

            // Add wells
            wellList.ForEach(x => DevKit.AddAndAssert(x));

            foreach (var well in wellList)
            {
                // Create logs
                var logs = DevKit.GenerateLogs(well.Uid, well.Name, LogIndexType.measureddepth, numOfObjects);

                // Create trajectories
                var trajectories = DevKit.GenerateTrajectories(well.Uid, well.Name, numOfObjects);

                // Add 5 wellbores with data objects
                for (var i = 0; i < numOfObjects; i++)
                {
                    var wellbore = new Wellbore() { Uid = DevKit.Uid(), UidWell = well.Uid, Name = DevKit.Name(), NameWell = well.Name };
                    DevKit.AddAndAssert(wellbore);
                    DevKit.AddListOfLogsToWellbore(logs, wellbore);
                    DevKit.AddListOfTrajectoriesToWellbore(trajectories, wellbore);
                }
            }

            // Delete each well in parallel
            var taskList = new List<Task>();
            wellList.ForEach(x =>
            {
                taskList.Add(new Task(() =>
                {
                    var deleteWell = new Well { Uid = x.Uid };
                    var result = DevKit.Delete<WellList, Well>(deleteWell, ObjectTypes.Well,
                        string.Empty, OptionsIn.CascadedDelete.True);
                    Assert.IsNotNull(result);
                    Assert.AreEqual(result.Result, 1);

                    // Ensure well does not exist anymore
                    DevKit.GetAndAssert(deleteWell, false);
                }));
            });

            taskList.ForEach(x => x.Start());
            await Task.WhenAll(taskList);

            wellList.ForEach(x =>
            {
                var wells = DevKit.Get<WellList, Well>(DevKit.List(new Well() { Uid = x.Uid }));
                Assert.IsNotNull(wells);

                var result = EnergisticsConverter.XmlToObject<WellList>(wells.XMLout);
                Assert.IsNotNull(wells);
                Assert.AreEqual(0, result.Well.Count);
            });
        }
    }
}
