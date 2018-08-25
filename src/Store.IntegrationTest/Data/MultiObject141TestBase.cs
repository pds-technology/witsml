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

using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data
{
    public abstract class MultiObject141TestBase : IntegrationTestBase
    {
        public Well Well { get; set; }
        public Wellbore Wellbore { get; set; }
        public Log Log { get; set; }
        public Trajectory Trajectory { get; set; }
        public DevKit141Aspect DevKit { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            Logger.Debug($"Executing {TestContext.TestName}");
            DevKit = new DevKit141Aspect(TestContext);

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            Well = new Well
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Well"),
                TimeZone = DevKit.TimeZone
            };
            Wellbore = new Wellbore
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Wellbore"),
                UidWell = Well.Uid,
                NameWell = Well.Name,
                MD = new MeasuredDepthCoord(0, MeasuredDepthUom.ft)
            };
            Log = new Log
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Log"),
                UidWell = Well.Uid,
                NameWell = Well.Name,
                UidWellbore = Wellbore.Uid,
                NameWellbore = Wellbore.Name
            };
            Trajectory = new Trajectory
            {
                Uid = DevKit.Uid(),
                Name = DevKit.Name("Trajectory"),
                UidWell = Well.Uid,
                NameWell = Well.Name,
                UidWellbore = Wellbore.Uid,
                NameWellbore = Wellbore.Name
            };

            BeforeEachTest();
            OnTestSetUp();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            AfterEachTest();
            OnTestCleanUp();
            DevKit.Container.Dispose();
            DevKit = null;
        }

        private void BeforeEachTest()
        {
            //log
            Log.IndexType = LogIndexType.measureddepth;
            Log.IndexCurve = "MD";

            //trajectory
            Trajectory.ServiceCompany = "Service Company T";
        }

        private void AfterEachTest()
        {
            //log
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.LogMaxDataPointsGet = DevKitAspect.DefaultLogMaxDataPointsGet;
            WitsmlSettings.LogMaxDataPointsUpdate = DevKitAspect.DefaultLogMaxDataPointsAdd;
            WitsmlSettings.LogMaxDataPointsAdd = DevKitAspect.DefaultLogMaxDataPointsUpdate;
            WitsmlSettings.LogMaxDataPointsDelete = DevKitAspect.DefaultLogMaxDataPointsDelete;
            WitsmlSettings.LogMaxDataNodesGet = DevKitAspect.DefaultLogMaxDataNodesGet;
            WitsmlSettings.LogMaxDataNodesAdd = DevKitAspect.DefaultLogMaxDataNodesAdd;
            WitsmlSettings.LogMaxDataNodesUpdate = DevKitAspect.DefaultLogMaxDataNodesUpdate;
            WitsmlSettings.LogMaxDataNodesDelete = DevKitAspect.DefaultLogMaxDataNodesDelete;
            WitsmlSettings.LogGrowingTimeoutPeriod = DevKitAspect.DefaultLogGrowingTimeoutPeriod;
            WitsmlOperationContext.Current = null;

            //trajectory
            WitsmlSettings.MaxStationCount = DevKitAspect.DefaultMaxStationCount;
            WitsmlSettings.TrajectoryMaxDataNodesGet = DevKitAspect.DefaultTrajectoryMaxDataNodesGet;
            WitsmlSettings.TrajectoryMaxDataNodesAdd = DevKitAspect.DefaultTrajectoryMaxDataNodesAdd;
            WitsmlSettings.TrajectoryMaxDataNodesUpdate = DevKitAspect.DefaultTrajectoryMaxDataNodesUpdate;
            WitsmlSettings.TrajectoryMaxDataNodesDelete = DevKitAspect.DefaultTrajectoryMaxDataNodesDelete;
            WitsmlSettings.TrajectoryGrowingTimeoutPeriod = DevKitAspect.DefaultTrajectoryGrowingTimeoutPeriod;
        }

        protected virtual void OnTestSetUp() { }

        protected virtual void OnTestCleanUp() { }

        protected virtual void AddParents()
        {
            DevKit.AddAndAssert<WellList, Well>(Well);
            DevKit.AddAndAssert<WellboreList, Wellbore>(Wellbore);
        }
    }        
}
