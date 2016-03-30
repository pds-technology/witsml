using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Data.Channels;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141QueryTests
    {
        private DevKit141Aspect DevKit;
        private Well _well;
        private Wellbore _wellbore;
        private DatabaseProvider _databaseProvider;
        private ChannelDataAdapter _channelDataAdapter;

        [TestInitialize]
        public void TestSetUp()
        {
            _databaseProvider = new DatabaseProvider(new MongoDbClassMapper());
            _channelDataAdapter = new ChannelDataAdapter(_databaseProvider);

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

        [Ignore]
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
            var columnCountBeforeSave = log.LogData.First().Data.First().Split(',').Length;
            response = DevKit.Add<LogList, Log>(log);

            // Test that a Log was Added successfully
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;
            var mnemonics = log.LogCurveInfo.Select(x => x.Mnemonic.Value).ToList();
            var logData = _channelDataAdapter.GetLogData(uidLog, mnemonics, null, true);

            // Test that LogData was returned
            Assert.IsNotNull(logData);

            var data = logData.Data;
            var firstRow = data.First().Split(',');
            mnemonics = logData.MnemonicList.Split(',').ToList();

            // Test that all of the rows of data saved are returned.
            Assert.AreEqual(row, data.Count);

            // Test that the number of mnemonics matches the number of data values per row
            Assert.AreEqual(firstRow.Length, mnemonics.Count);

            // Update Test to verify that a column of LogData.Data with no values is NOT returned with the results.
            Assert.AreEqual(columnCountBeforeSave - 1, firstRow.Length);
        }

        [Ignore]
        [TestMethod]
        public void Log_column_with_one_value_returned()
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

            // Replace the third data row with a value where there is none
            log.LogData.First().Data[2] = log.LogData.First().Data[2].Replace(",,", ",0,");
            var columnCountBeforeSave = log.LogData.First().Data.First().Split(',').Length;

            // Save the Log
            response = DevKit.Add<LogList, Log>(log);


            var uidLog = response.SuppMsgOut;
            var mnemonics = log.LogCurveInfo.Select(x => x.Mnemonic.Value).ToList();
            var logData = _channelDataAdapter.GetLogData(uidLog, mnemonics, null, true);


            var data = logData.Data;
            var firstRow = data.First().Split(',');

            // Update Test to verify that a column of LogData.Data with no values is NOT returned with the results.
            Assert.AreEqual(columnCountBeforeSave, firstRow.Length);
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
