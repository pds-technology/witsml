using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141Tests
    {
        private DevKit141Aspect DevKit;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit141Aspect();

            DevKit.Store.CapServerProviders = DevKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();
        }

        [TestMethod]
        public void Test_add_depth_log_with_data()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            var log = new Log()
            {
                UidWell = wellbore.UidWell,
                NameWell = well.Name, UidWellbore = response.SuppMsgOut,
                NameWellbore = wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.measureddepth);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Test_add_time_log_with_data()
        {
            var well = new Well { Name = "Well-to-add-01", TimeZone = DevKit.TimeZone };
            var response = DevKit.Add<WellList, Well>(well);

            var wellbore = new Wellbore()
            {
                UidWell = response.SuppMsgOut,
                NameWell = "Well 01",
                Name = "Wellbore 01-01"
            };
            response = DevKit.Add<WellboreList, Wellbore>(wellbore);

            var log = new Log()
            {
                UidWell = wellbore.UidWell,
                NameWell = well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = wellbore.Name,
                Name = DevKit.Name("Log 01")
            };

            DevKit.InitHeader(log, LogIndexType.datetime);
            DevKit.InitDataMany(log, DevKit.Mnemonics(log), DevKit.Units(log), 10, 1, false);
            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
