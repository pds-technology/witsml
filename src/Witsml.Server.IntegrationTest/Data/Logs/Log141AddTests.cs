using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141AddTests
    {
        private DevKit141Aspect DevKit;
        private Well Well;
        private Wellbore Wellbore;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            Well = new Well { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone };

            Wellbore = new Wellbore()
            {
                NameWell = Well.Name,
                Name = DevKit.Name("Wellbore 01")
            };
        }

        [TestMethod]
        public void Log_can_be_added_without_data()
        {
            Well.Uid = "804415d0-b5e7-4389-a3c6-cdb790f5485f";
            Well.Name = "Test Well 01";

            // check if well already exists
            var results = DevKit.Query<WellList, Well>(Well);
            if (results.Any()) return;

            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.Uid = "d3e7d4bf-0f29-4c2b-974d-4871cf8001fd";
            Wellbore.Name = "Test Wellbore 01";
            Wellbore.UidWell = Well.Uid;
            Wellbore.NameWell = Well.Name;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                Uid = "e2401b72-550f-4695-ab27-d5b0589bde17",
                Name = "Test Log 01",
                UidWell = Well.Uid,
                NameWell = Well.Name,
                UidWellbore = Wellbore.Uid,
                NameWellbore = Wellbore.Name,
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log_can_be_added_with_depth_data()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log_can_be_added_with_time_data()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.datetime);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10, 1, false);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Test_append_log_data()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                StartIndex = new GenericMeasure(5, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = new Log()
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog,
                StartIndex = new GenericMeasure(17, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 6);

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Data.Count);
        }

        [TestMethod]
        public void Test_prepend_log_data()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                StartIndex = new GenericMeasure(17, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = new Log()
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog,
                StartIndex = new GenericMeasure(5, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 6);

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(16, logData.Data.Count);
        }

        [TestMethod]
        public void Test_update_overlapping_log_data()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                StartIndex = new GenericMeasure(1, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 8);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = new Log()
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog,
                StartIndex = new GenericMeasure(4.1, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 3, 0.9);

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            var logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(9, logData.Data.Count);
        }

        [TestMethod]
        public void Test_overwrite_log_data_chunk()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                StartIndex = new GenericMeasure(17, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 6);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = new Log()
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog,
                StartIndex = new GenericMeasure(4.1, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 3, 0.9);

            var logData = log.LogData.First();
            logData.Data.Add("21.5, 1, 21.7");

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(5, logData.Data.Count);
        }

        [TestMethod]
        public void Test_update_log_data_with_different_range_for_each_channel()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                StartIndex = new GenericMeasure(15, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 8);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = log.UidWellbore;
            var uidLog = response.SuppMsgOut;

            log = new Log()
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog,
                StartIndex = new GenericMeasure(13, "m")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 6, 0.9);

            var logData = log.LogData.First();
            logData.Data.Clear();

            logData.Data.Add("13,13.1,");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,");
            logData.Data.Add("16,16.1,");
            logData.Data.Add("17,17.1,");
            logData.Data.Add("20,20.1,20.2");
            logData.Data.Add("21,,21.2");
            logData.Data.Add("22,,22.2");
            logData.Data.Add("23,,23.2");

            var updateResponse = DevKit.Update<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.First();
            logData = result.LogData.FirstOrDefault();

            Assert.IsNotNull(logData);
            Assert.AreEqual(11, logData.Data.Count);

            var data = logData.Data;
            Assert.AreEqual("15,15.1,15", data[2]);
        }

        [TestMethod]
        public void Test_update_log_header()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                Description = "Not updated field",
                RunNumber = "101",
                BhaRunNumber = 1
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(log.Description, logAdded.Description);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);
            Assert.AreEqual(log.BhaRunNumber, logAdded.BhaRunNumber);
            Assert.IsNull(logAdded.CommonData.ItemState);

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                CommonData = new CommonData { ItemState = ItemState.actual }
            };

            update.RunNumber = "102";
            update.BhaRunNumber = 2;

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(logAdded.Description, logUpdated.Description);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
            Assert.AreEqual(update.BhaRunNumber, logUpdated.BhaRunNumber);
            Assert.AreEqual(update.CommonData.ItemState, logUpdated.CommonData.ItemState);
        }

        [TestMethod]
        public void Test_update_log_header_update_curve()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                Description = "Not updated field",
                RunNumber = "101",
                BhaRunNumber = 1
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            log.LogCurveInfo.RemoveAt(2);
            log.LogData.Clear();
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(log.Description, logAdded.Description);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);
            Assert.AreEqual(log.BhaRunNumber, logAdded.BhaRunNumber);
            Assert.IsNull(logAdded.CommonData.ItemState);
            var logCurve = logAdded.LogCurveInfo.FirstOrDefault(c => c.Uid == "ROP");
            Assert.IsNull(logCurve.CurveDescription);

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                CommonData = new CommonData { ItemState = ItemState.actual }
            };

            update.RunNumber = "102";
            update.BhaRunNumber = 2;

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();
            var updateCurve = update.LogCurveInfo.First();
            updateCurve.CurveDescription = "Updated description";

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(logAdded.Description, logUpdated.Description);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
            Assert.AreEqual(update.BhaRunNumber, logUpdated.BhaRunNumber);
            Assert.AreEqual(update.CommonData.ItemState, logUpdated.CommonData.ItemState);
            logCurve = logUpdated.LogCurveInfo.FirstOrDefault(c => c.Uid == "ROP");
            Assert.AreEqual(updateCurve.CurveDescription, logCurve.CurveDescription);
        }

        [TestMethod]
        public void Test_update_log_header_add_curve()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            log.LogCurveInfo.RemoveRange(1, 2);
            log.LogData.Clear();

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(1, logAdded.LogCurveInfo.Count);
            Assert.AreEqual(log.LogCurveInfo.Count, logAdded.LogCurveInfo.Count);

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            update.LogCurveInfo.RemoveAt(2);
            update.LogCurveInfo.RemoveAt(0);
            update.LogData.Clear();

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(2, logUpdated.LogCurveInfo.Count);
        }

        [TestMethod]
        public void Test_log_index_direction_default_and_update()
        {
            var response = DevKit.Add<WellList, Well>(Well);
            Wellbore.UidWell = response.SuppMsgOut;

            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            var uidWellbore = response.SuppMsgOut;

            var log = new Log()
            {
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = uidWellbore,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                RunNumber = "101",
                IndexCurve = "MD",
                IndexType = LogIndexType.measureddepth
            };

            DevKit.InitHeader(log, log.IndexType.Value);
            log.Direction = null;

            Assert.IsFalse(log.Direction.HasValue);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Uid = uidLog
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logAdded = results.FirstOrDefault();

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(LogIndexDirection.increasing, logAdded.Direction);
            Assert.AreEqual(log.RunNumber, logAdded.RunNumber);

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Direction = LogIndexDirection.decreasing,
                RunNumber = "102"
            };

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            var logUpdated = results.FirstOrDefault();

            Assert.IsNotNull(logUpdated);
            Assert.AreEqual(LogIndexDirection.increasing, logAdded.Direction);
            Assert.AreEqual(update.RunNumber, logUpdated.RunNumber);
        }
    }
}
