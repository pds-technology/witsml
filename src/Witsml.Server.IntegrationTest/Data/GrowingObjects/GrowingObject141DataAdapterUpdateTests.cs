using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;
using PDS.Witsml.Server.Jobs;

namespace PDS.Witsml.Server.Data.GrowingObjects
{
    [TestClass]
    public class GrowingObject141DataAdapterUpdateTests : MultiObject141TestBase
    {
        private const int GrowingTimeoutPeriod = 10;

        [TestMethod]
        public void GrowingObject141DataAdapter_UpdateInStore_Append_Data_ExpireGrowingObjects()
        {
            //Add parents
            AddParents();

            //Add log
            Log.StartIndex = new GenericMeasure(5, "m");
            AddLogWithData(Log, LogIndexType.measureddepth, 10);

            var addedLog = DevKit.GetAndAssert(Log);
            var wellboreResult = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(addedLog.ObjectGrowing.GetValueOrDefault());
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault());

            // Add trajectory with stations
            AddTrajectoryWithData(Trajectory, 3, 5);

            var addedTrajectory = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);
            Assert.IsFalse(addedTrajectory.ObjectGrowing.GetValueOrDefault());
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault());

            //update log
            CreateAndUpdateLogData(Log, LogIndexType.measureddepth, new GenericMeasure(17, "m"), 6);

            var updatedLog = DevKit.GetAndAssert(Log);
            wellboreResult = DevKit.GetAndAssert(Wellbore);
            Assert.IsTrue(updatedLog.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "Log-Well IsActive");

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
            wellboreResult = DevKit.GetAndAssert(Wellbore);
            Assert.IsTrue(updatedTrajectory.ObjectGrowing.GetValueOrDefault(), "Trajectory ObjectGrowing");
            Assert.IsTrue(wellboreResult.IsActive.GetValueOrDefault(), "Trajectory-Well IsActive");

            //Change settings and wait
            WitsmlSettings.LogGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = GrowingTimeoutPeriod;
            Thread.Sleep(GrowingTimeoutPeriod * 1000);

            //Expire objects
            DevKit.Container.Resolve<ObjectGrowingManager>().ExpireGrowingObjects();

            updatedLog = DevKit.GetAndAssert(Log);
            updatedTrajectory = DevKit.GetAndAssert(Trajectory);
            wellboreResult = DevKit.GetAndAssert(Wellbore);

            Assert.IsFalse(updatedLog.ObjectGrowing.GetValueOrDefault(), "Log ObjectGrowing");
            Assert.IsFalse(updatedTrajectory.ObjectGrowing.GetValueOrDefault(), "Trajectory ObjectGrowing ");
            Assert.IsFalse(wellboreResult.IsActive.GetValueOrDefault(), "IsActive");
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
