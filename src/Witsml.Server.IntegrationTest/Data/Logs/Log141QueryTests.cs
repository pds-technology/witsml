using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141QueryTests
    {
        private DevKit141Aspect DevKit;
        private Well _well;
        private Wellbore _wellbore;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone };
            _wellbore = new Wellbore()
            {
                NameWell = _well.Name,
                Name = DevKit.Name("Wellbore 01")
            };
        }

        [TestMethod]
        public void Log_can_be_retrieved_with_all_data()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var provider = new DatabaseProvider(new MongoDbClassMapper());
            var adapter = new ChannelDataAdapter(provider);

            var mnemonics = log.LogCurveInfo.Select(x => x.Mnemonic.Value).ToList();
            var logData = adapter.GetLogData(uidLog, mnemonics, null, true);

            Assert.IsNotNull(logData);
            var data = logData.Data;
            var firstRow = data.First().Split(',');
            mnemonics = logData.MnemonicList.Split(',').ToList();
            Assert.AreEqual(row, data.Count);
            Assert.AreEqual(firstRow.Length, mnemonics.Count);

            // TODO: Update Test to verify that a column of LogData.Data with no values is not returned with the results.
        }

        [TestMethod]
        public void Log_can_be_retrieved_with_increasing_log_data()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            var logData = new LogData { MnemonicList = DevKit.Mnemonics(log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                StartIndex = new GenericMeasure(2.0, "m"),
                EndIndex = new GenericMeasure(6.0, "m"),
                LogData = new List<LogData> { logData }
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Log_can_be_retrieved_with_decreasing_log_data()
        {
            var response = DevKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            var row = 10;
            DevKit.InitHeader(log, LogIndexType.measureddepth, false);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), row, 1, true, true, false);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            var logData = new LogData { MnemonicList = DevKit.Mnemonics(log) };
            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore,
                Direction = LogIndexDirection.decreasing,
                StartIndex = new GenericMeasure(-3.0, "m"),
                EndIndex = new GenericMeasure(-6.0, "m"),
                LogData = new List<LogData> { logData }
            };
            var result = DevKit.Query<LogList, Log>(query, ObjectTypes.Log, null, OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result);
        }
    }
}
