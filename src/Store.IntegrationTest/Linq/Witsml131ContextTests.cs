//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Linq;

namespace PDS.WITSMLstudio.Store.Linq
{
    [TestClass]
    public class Witsml131ContextTests
    {
        public TestContext TestContext { get; set; }
        public Witsml131Context Context { get; set; }
        public DevKit131Aspect DevKit { get; set; }
        public Well Well { get; set; }
        public Wellbore Wellbore { get; set; }    
        public Log Log { get; set; }
        
        [TestInitialize]
        public void TestSetUp()
        {
            var url = "http://localhost/Witsml.Web/WitsmlStore.svc"; // IIS
            //var url = "http://localhost:5050/WitsmlStore.svc"; // TestApp

            DevKit = new DevKit131Aspect(TestContext, url);
            Context = new Witsml131Context(DevKit.ConnectionUrl);

            Well = new Well() { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            Wellbore = new Wellbore() { UidWell = Well.Uid, NameWell = Well.Name, Name = DevKit.Name("Wellbore 01-01"), Uid = DevKit.Uid() };
            Log = new Log() { UidWell = Well.Uid, NameWell = Well.Name, UidWellbore = Wellbore.Uid, NameWellbore = Wellbore.Name, Name = DevKit.Name("Log 01"), Uid = DevKit.Uid() };
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            Context.Dispose();
            DevKit = null;
        }
        
        [TestMethod]
        public void Witsml131Context_Connection_Can_Get_Version()
        {
            var response = Context.Connection.GetVersion();
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void Witsml131Context_Connection_Can_Get_Capabilities()
        {
            var response = Context.Connection.GetCap<CapServers>();
            Assert.IsNotNull(response);
        }
        
        [TestMethod]
        public void Witsml131Context_Wells_Can_Query_For_All_Wells()
        {
            AddParents();
            var witsmlQuery = Context.Wells.With(OptionsIn.ReturnElements.IdOnly);
            var wells = witsmlQuery.ToList();
            Assert.IsTrue(wells.Count > 0);
            Assert.IsNotNull(wells.FirstOrDefault(w => w.Uid == Well.Uid));
        }
        
        [TestMethod]
        public void Witsml131Context_GetAllWells_Can_Query_For_All_Wells()
        {
            AddParents();
            var wells = Context.GetAllWells().ToList();
            Assert.IsTrue(wells.Count > 0);
            Assert.IsNotNull(wells.FirstOrDefault(w => w.Uid == Well.Uid));
        }
        
        [TestMethod]
        public void Witsml131Context_Wellbores_Can_Query_For_All_Wellbores()
        {
            AddParents();
            var witsmlQuery = Context.Wellbores.With(OptionsIn.ReturnElements.IdOnly);
            var wellbores = witsmlQuery.ToList();
            Assert.IsTrue(wellbores.Count > 0);
            Assert.IsNotNull(wellbores.FirstOrDefault(w => w.Uid == Wellbore.Uid));            
        }
        
        [TestMethod]
        public void Witsml131Context_GetAllWellbores_Can_Query_For_All_Wells()
        {
            AddParents();
            var wellbores = Context.GetWellbores(Well.GetUri()).ToList();
            Assert.IsTrue(wellbores.Count > 0);
            Assert.IsNotNull(wellbores.FirstOrDefault(w => w.Uid == Wellbore.Uid));
        }

        [TestMethod]
        public void Witsml131Context_Wellbores_Can_Query_For_All_Logs()
        {
            AddParents();
            AddLog();
            
            var witsmlQuery = Context.Logs.With(OptionsIn.ReturnElements.IdOnly);
            var logs = witsmlQuery.ToList();
            Assert.IsTrue(logs.Count > 0);
            Assert.IsNotNull(logs.FirstOrDefault(l => l.Uid == Log.Uid));
        }

        [TestMethod]
        public void Witsml131Context_Rig_Can_Query_For_All_Rigs()
        {
            AddParents();
            var rig = new Rig { Uid = DevKit.Uid(), Name = DevKit.Name("Rig"), UidWell = Well.Uid, NameWell = Well.Name, UidWellbore = Wellbore.Uid, NameWellbore = Wellbore.Name };
            DevKit.Proxy.Write(DevKit.New<RigList>(x => x.Rig = DevKit.List(rig)));

            var witsmlQuery = Context.Rigs.With(OptionsIn.ReturnElements.IdOnly);
            var rigs = witsmlQuery.ToList();
            Assert.IsTrue(rigs.Count > 0);
            Assert.IsNotNull(rigs.FirstOrDefault(r => r.Uid == rig.Uid));
        }

        [TestMethod]
        public void Witsml131Context_Trajectory_Can_Query_For_All_Trajectories()
        {
            AddParents();
            var trajectory = new Trajectory { Uid = DevKit.Uid(), Name = DevKit.Name("Trajectory"), UidWell = Well.Uid, NameWell = Well.Name, UidWellbore = Wellbore.Uid, NameWellbore = Wellbore.Name };
            DevKit.Proxy.Write(DevKit.New<TrajectoryList>(x => x.Trajectory = DevKit.List(trajectory)));

            var witsmlQuery = Context.Trajectories.With(OptionsIn.ReturnElements.IdOnly);
            var trajectories = witsmlQuery.ToList();
            Assert.IsTrue(trajectories.Count > 0);
            Assert.IsNotNull(trajectories.FirstOrDefault(t => t.Uid == trajectory.Uid));
        }

        [TestMethod]
        public void Witsml131Context_Logs_Can_Query_For_Log_Header()
        {
            AddParents();
            AddLog();

            var logs = Context.Logs
                .Include(x => x.LogCurveInfo = Context.One<LogCurveInfo>())
                .With(OptionsIn.ReturnElements.HeaderOnly).ToList();

            var log = logs.FirstOrDefault(x => x.NameWell == Well.Name && x.NameWellbore == Wellbore.Name && x.Name == Log.Name);

            Assert.IsNotNull(log);
            Assert.AreEqual(Log.Name, log.Name);
            Assert.AreEqual(Log.Uid, log.Uid);
        }        

        [TestMethod]
        public void Witsml131Context_Wells_Can_Query_For_Well_By_Uid()
        {
            AddParents();
            var well = Context.Wells.With(OptionsIn.ReturnElements.IdOnly).GetByUid(Well.Uid);
            Assert.IsNotNull(well);
            Assert.AreEqual(Well.Uid, well.Uid);
        }

        [TestMethod]
        public void Witsml131Context_Wellbores_Can_Query_For_Wellbores_By_Uid()
        {
            AddParents();
            var wellbore = Context.Wellbores.With(OptionsIn.ReturnElements.IdOnly).GetByUid(Wellbore.UidWell, Wellbore.Uid);
            Assert.IsNotNull(wellbore);
            Assert.AreEqual(Wellbore.Uid, wellbore.Uid);
        }

        [TestMethod]
        public void Witsml131Context_Wellbores_Can_Query_For_Logs_By_Uid()
        {
            AddParents();
            AddLog();

            var logs = Context.Logs.With(OptionsIn.ReturnElements.IdOnly).GetByUid(Log.UidWell, Log.UidWellbore, Log.Uid);
            Assert.IsNotNull(logs);
            Assert.AreEqual(Log.Uid, Log.Uid);
        }

        [TestMethod]
        public void Witsml131Context_Wells_Can_Query_For_Well_By_Name()
        {
            AddParents();
            var well = Context.Wells.With(OptionsIn.ReturnElements.All).GetByName(Well.Name);
            Assert.IsNotNull(well);
            Assert.AreEqual(Well.Name, well.Name);
        }

        [TestMethod]
        public void Witsml131Context_Wellbores_Can_Query_For_Wellbores_By_Well_Name()
        {
            AddParents();
            var wellbore = Context.Wellbores.With(OptionsIn.ReturnElements.All).GetByName(Wellbore.NameWell, Wellbore.Name);
            Assert.IsNotNull(wellbore);
            Assert.AreEqual(Wellbore.Name, wellbore.Name);
        }

        [TestMethod]
        public void Witsml131Context_Wellbores_Can_Query_For_Logs_By_Name()
        {
            AddParents();
            AddLog();

            var logs = Context.Logs.With(OptionsIn.ReturnElements.IdOnly).GetByName(Log.NameWell, Log.NameWellbore, Log.Name);
            Assert.IsNotNull(logs);
            Assert.AreEqual(Log.Name, Log.Name);
        }

        [TestMethod]
        public void Witsml131Context_GetWellboreObjects_Can_Query_For_Wellbore_Objects()
        {
            AddParents();
            AddLog();

            var wellboreObjects = Context.GetWellboreObjects(ObjectTypes.Log, Log.GetUri());
            Assert.IsNotNull(wellboreObjects);
            Assert.AreEqual(Log.Uid, wellboreObjects.Where(l => l.Uid == Log.Uid).FirstOrDefault()?.Uid);
        }

        [TestMethod]
        public void Witsml131Context_GetObjectDetails_Can_Query_For_Object_Details()
        {
            AddParents();

            var objectDetails = Context.GetObjectDetails(ObjectTypes.Well, Well.GetUri());
            Assert.IsNotNull(objectDetails);
            Assert.AreEqual(Well.Uid, objectDetails.Uid);
        }

        [TestMethod]
        public void Witsml131Context_GetObjectDetails_Can_Query_For_Object_Details_With_OptionsIn()
        {
            AddParents();

            var objectDetails = Context.GetObjectDetails(ObjectTypes.Wellbore, Wellbore.GetUri(), OptionsIn.ReturnElements.IdOnly);
            Assert.IsNotNull(objectDetails);
            Assert.AreEqual(Wellbore.Uid, objectDetails.Uid);
        }

        [TestMethod]
        public void Witsml131Context_GetObjectIdOnly_Can_Query_For_Object_Ids()
        {
            AddParents();

            var objectIdOnly = Context.GetObjectIdOnly(ObjectTypes.Well, Well.GetUri());
            Assert.IsNotNull(objectIdOnly);
            Assert.AreEqual(Well.Uid, objectIdOnly.Uid);
        }

        [TestMethod]
        public void Witsml131Context_GetGrowingObjectHeaderOnly_Can_Query_For_Growing_Object_Header_Only()
        {
            AddParents();
            AddLog();

            var headerOnly = Context.GetGrowingObjectHeaderOnly(ObjectTypes.Log, Log.GetUri());
            Assert.IsNotNull(headerOnly);
            Assert.AreEqual(Log.Uid, headerOnly.Uid);
        }

        [TestMethod]
        public void Witsml131Context_Wells_Can_Query_For_Well_Hiererchy()
        {
            AddParents();
            AddLog();

            var well = Context.Wells.With(OptionsIn.ReturnElements.IdOnly).GetByUid(Well.Uid);
            Assert.IsNotNull(well);
            Assert.AreEqual(Well.Uid, well.Uid);

            var wellbores = Context.Wellbores.With(OptionsIn.ReturnElements.IdOnly).Where(x => x.UidWell == Well.Uid).ToList();
            Assert.IsTrue(wellbores.Count > 0);
            Assert.AreEqual(Well.Uid, wellbores.FirstOrDefault()?.UidWell);
            Assert.IsNotNull(wellbores.Find(wb => wb.Uid == Wellbore.Uid));

            var logs = Context.Logs.With(OptionsIn.ReturnElements.IdOnly).Where(x => x.UidWell == Well.Uid && x.UidWellbore == Wellbore.Uid).ToList();
            Assert.IsTrue(logs.Count > 0);
            Assert.AreEqual(Well.Uid, logs.FirstOrDefault()?.UidWell);
            Assert.AreEqual(Wellbore.Uid, logs.FirstOrDefault()?.UidWellbore);
            Assert.IsNotNull(logs.Find(l => l.Uid == Log.Uid));
        }

        private void AddParents()
        {
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well)));
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore)));
        }

        private void AddLog()
        {
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log)));
        }

    }
}
