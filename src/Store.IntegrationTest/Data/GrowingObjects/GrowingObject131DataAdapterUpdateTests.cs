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
using System.Linq;
using System.Threading;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Jobs;

namespace PDS.WITSMLstudio.Store.Data.GrowingObjects
{
    [TestClass]
    public class GrowingObject131DataAdapterUpdateTests : MultiObject131TestBase
    {
        private const int GrowingTimeoutPeriod = 10;

        [TestMethod]
        public void GrowingObject131DataAdapter_UpdateInStore_Append_Data_ExpireGrowingObjects()
        {
            //Add parents
            AddParents();

            //Add log
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());

            // Add trajectory with stations
            AddTrajectoryWithData(Trajectory, 3, 5);

            var addedTrajectory = DevKit.GetAndAssert(Trajectory);
            Assert.IsFalse(addedTrajectory.ObjectGrowing.GetValueOrDefault());

            //update log
            CreateAndUpdateLogData(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);

            var updatedLog = DevKit.GetAndAssert(Log);
            Assert.IsTrue(updatedLog.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");

            //update trajectory
            var station1 = Trajectory.TrajectoryStation.FirstOrDefault();
            Assert.IsNotNull(station1);
            station1.Azi.Value++;

            var newStation = new TrajectoryStation
            {
                Uid = "sta-4",
                MD = new MeasuredDepthCoord { Uom = MeasuredDepthUom.m, Value = 10 },
                TypeTrajStation = station1.TypeTrajStation,
                Azi = station1.Azi,
            };

            CreateAndUpdateTrajectoryStations(Trajectory, new List<TrajectoryStation> { newStation });

            var updatedTrajectory = DevKit.GetAndAssert(Trajectory);
            Assert.IsTrue(updatedTrajectory.ObjectGrowing.GetValueOrDefault(), "Trajectory ObjectGrowing");

            //Change settings and wait
            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            //Expire objects
            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            updatedLog = DevKit.GetAndAssert(Log);
            updatedTrajectory = DevKit.GetAndAssert(Trajectory);

            Assert.IsFalse(updatedLog.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");
            Assert.IsFalse(updatedTrajectory.ObjectGrowing.GetValueOrDefault(), "Trajectory ObjectGrowing ");
        }

        #region Helper Methods

        private void AddLogWithData(Log log, LogIndexType indexType, int numOfRows, bool hasEmptyChannel = true)
        {
            DevKit.InitHeader(log, indexType);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), numOfRows, hasEmptyChannel: hasEmptyChannel);

            DevKit.AddAndAssert(log);
        }

        private void AddTrajectoryWithData(Trajectory trajectory, int numOfStations, double startMd)
        {
            var stations = DevKit.TrajectoryStations(numOfStations, startMd);
            trajectory.TrajectoryStation = stations;
            DevKit.AddAndAssert(trajectory);
        }

        private void CreateAndUpdateLogData(Log log, LogIndexType indexType, GenericMeasure startIndex, int numOfRows, double factor = 1, bool hasEmptyChannel = true)
        {
            var update = DevKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            update.StartIndex = startIndex;

            DevKit.InitHeader(update, indexType);
            DevKit.InitDataMany(update, DevKit.Mnemonics(update), DevKit.Units(update), numOfRows, factor, hasEmptyChannel: hasEmptyChannel);

            DevKit.UpdateAndAssert(update);
        }

        private void CreateAndUpdateTrajectoryStations(Trajectory trajectory, List<TrajectoryStation> stations)
        {
            var update = new Trajectory
            {
                Uid = trajectory.Uid,
                UidWell = trajectory.UidWell,
                UidWellbore = trajectory.UidWellbore,
                TrajectoryStation = stations
            };

            DevKit.UpdateAndAssert<TrajectoryList, Trajectory>(update);
        }

        #endregion
    }
}
