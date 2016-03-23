using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using System;
using Energistics.DataAccess;

namespace PDS.Witsml.Server
{
    [TestClass]
    public class WitsmlStore141ProxyTests
    {
        private DevKit141Aspect DevKit;
        private Well Well1;
        private Well Well2;
        private Wellbore Wellbore1;
        private Wellbore Wellbore2;
        private Log Log1;
        private Log Log2;

        [TestInitialize]
        public void TestSetUp()
        {
            var url = "http://localhost/Witsml.Web/WitsmlStore.svc"; // IIS
            //var url = "http://localhost:5050/WitsmlStore.svc"; // TestApp
            DevKit = new DevKit141Aspect(url);

            Well1 = new Well() { Name = DevKit.Name("Well 01"), TimeZone = DevKit.TimeZone, Uid = DevKit.Uid() };
            Well2 = new Well() { Name = DevKit.Name("Well 02"), TimeZone = DevKit.TimeZone };

            Wellbore1 = new Wellbore() { UidWell = Well1.Uid, NameWell = Well1.Name, Name = DevKit.Name("Wellbore 01-01"), Uid = DevKit.Uid() };
            Wellbore2 = new Wellbore() { UidWell = Well1.Uid, NameWell = Well1.Name, Name = DevKit.Name("Wellbore 01-02") };

            Log1 = new Log() { UidWell = Well1.Uid, NameWell = Well1.Name, UidWellbore = Wellbore1.Uid, NameWellbore = Wellbore1.Name, Name = DevKit.Name("Log 01"), Uid = DevKit.Uid() };
            Log2 = new Log() { UidWell = Well1.Uid, NameWell = Well1.Name, UidWellbore = Wellbore1.Uid, NameWellbore = Wellbore1.Name, Name = DevKit.Name("Log 02") };

            DevKit.InitHeader(Log1, LogIndexType.measureddepth);
            DevKit.InitHeader(Log2, LogIndexType.datetime);
        }

        [TestMethod]
        public void WitsmlStore_can_get_version()
        {
            var response = DevKit.Proxy.GetVersion();
            Console.WriteLine("DataSchemaVersions: {0}", response);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void WitsmlStore_can_get_capabilities()
        {
            var response = DevKit.Proxy.GetCap<CapServers>(OptionsIn.DataVersion.Version141);
            Console.WriteLine(EnergisticsConverter.ObjectToXml(response));
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void WitsmlStore_can_add_and_get_all_wells()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well2)));

            // Get all Wells
            var query = DevKit.Query<WellList>();
            var result = DevKit.Proxy.Read(query);

            Assert.IsNotNull(result.Well);
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Name == Well1.Name));
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Name == Well2.Name));
        }

        [TestMethod]
        public void WitsmlStore_can_get_well_by_uid()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well2)));

            // Get Well by Uid
            var query = DevKit.Query<WellList>();
            query.Well = DevKit.One<Well>(x => x.Uid = Well1.Uid);

            var result = DevKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Well.Count);
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Uid == Well1.Uid));
        }

        [TestMethod]
        public void WitsmlStore_can_get_well_by_name()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well2)));

            // Get Well by Name
            var query = DevKit.Query<WellList>();
            query.Well = DevKit.One<Well>(x => x.Name = Well2.Name);

            var result = DevKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Well.Count);
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Name == Well2.Name));
        }

        [TestMethod]
        public void WitsmlStore_can_get_well_RequestObjectSelectionCapability()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well2)));

            // Get Well by Name
            var query = DevKit.Query<WellList>();
            var result = DevKit.Proxy.Read(query, OptionsIn.RequestObjectSelectionCapability.True);

            Assert.AreEqual(1, result.Well.Count);
        }

        [TestMethod]
        public void WitsmlStore_can_add_and_get_all_wellbores()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well2)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore2)));

            // Get All Wellbores
            var query = DevKit.Query<WellboreList>();
            var result = DevKit.Proxy.Read(query);

            Assert.IsNotNull(result.Wellbore);
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Name == Wellbore1.Name));
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Name == Wellbore2.Name));
        }

        [TestMethod]
        public void WitsmlStore_can_get_wellbore_by_uid()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore2)));

            // Get Wellbore by Uid
            var query = DevKit.Query<WellboreList>();
            query.Wellbore = DevKit.One<Wellbore>(x => { x.Uid = Wellbore1.Uid; x.UidWell = Wellbore1.UidWell; });

            var result = DevKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Wellbore.Count);
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Uid == Wellbore1.Uid));
        }

        [TestMethod]
        public void WitsmlStore_can_get_wellbore_by_name()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore2)));

            // Get Wellbore by Name
            var query = DevKit.Query<WellboreList>();
            query.Wellbore = DevKit.List(new Wellbore() { Name = Wellbore2.Name, NameWell = Wellbore2.NameWell });

            var result = DevKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Wellbore.Count);
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Name == Wellbore2.Name && x.NameWell == Wellbore2.NameWell));
        }

        [TestMethod]
        public void WitsmlStore_can_add_and_get_log_header()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log2)));

            // Get Log header by Uid
            var query = DevKit.Query<LogList>();
            query.Log = DevKit.One<Log>(x => { x.Uid = Log1.Uid; x.UidWell = Log1.UidWell; x.UidWellbore = Log1.UidWellbore; });

            var result = DevKit.Proxy.Read(query, OptionsIn.ReturnElements.HeaderOnly);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == Log1.Uid));

            Assert.IsNotNull(result.Log[0].LogCurveInfo);
            Assert.AreEqual(3, result.Log[0].LogCurveInfo.Count);
        }

        [TestMethod]
        public void WitsmlStore_can_add_and_get_log_header_by_name()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log2)));

            // Get Log header by Name (must be by Well, Wellbore and Log name to be unique)
            var query = DevKit.Query<LogList>();
            query.Log = DevKit.One<Log>(x => 
            {
                x.NameWell = Log1.NameWell;
                x.NameWellbore = Log1.NameWellbore;
                x.Name = Log1.Name;
            });

            var result = DevKit.Proxy.Read(query, OptionsIn.ReturnElements.HeaderOnly);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == Log1.Uid));

            Assert.IsNotNull(result.Log[0].LogCurveInfo);
            Assert.AreEqual(3, result.Log[0].LogCurveInfo.Count);
        }

        [TestMethod]
        public void WitsmlStore_can_add_log_data()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitData(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0, null, 0);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log1)));

            // Get Log by Uid
            var query = DevKit.Query<LogList>();
            query.Log = DevKit.One<Log>(x => { x.Uid = Log1.Uid; x.UidWell = Log1.UidWell; x.UidWellbore = Log1.UidWellbore; });

            var result = DevKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == Log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(1, result.Log[0].LogData.Count);

            Assert.IsNotNull(result.Log[0].LogData[0].Data);
            Assert.AreEqual(1, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
        }

        [TestMethod]
        public void WitsmlStore_can_append_log_data()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitData(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0, null, 0);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log1)));

            // Update Log with appended LogData
            var log2 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitData(log2, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0.1, null, 1);
            DevKit.InitData(log2, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0.2, null, 2);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log2)));

            // Get Log by Uid
            var query = DevKit.Query<LogList>();
            query.Log = DevKit.One<Log>(x => { x.Uid = Log1.Uid; x.UidWell = Log1.UidWell; x.UidWellbore = Log1.UidWellbore; });

            var result = DevKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == Log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(1, result.Log[0].LogData.Count);

            Assert.IsNotNull(result.Log[0].LogData[0].Data);
            Assert.AreEqual(3, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("0.1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("0.2,,2", result.Log[0].LogData[0].Data[2]);
        }

        [TestMethod]
        public void WitsmlStore_can_update_log_data()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitData(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0, null, 0);
            DevKit.InitData(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0.1, null, 1);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log1)));

            // Update Log with updated LogData
            var log2 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitData(log2, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 0.1, 10, 1.1);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log2)));

            // Get Log by Uid
            var query = DevKit.Query<LogList>();
            query.Log = DevKit.One<Log>(x => { x.Uid = Log1.Uid; x.UidWell = Log1.UidWell; x.UidWellbore = Log1.UidWellbore; });

            var result = DevKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == Log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(1, result.Log[0].LogData.Count);

            Assert.IsNotNull(result.Log[0].LogData[0].Data);
            Assert.AreEqual(2, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("0.1,10,1.1", result.Log[0].LogData[0].Data[1]);
        }

        [TestMethod]
        public void WitsmlStore_can_query_log_data_by_startIndex_endIndex()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitDataMany(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 10);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log1)));


            // StartIndex and End Index above range
            LogList result = DevKit.QueryLogByRange(log1, -2, -1);
            Assert.AreEqual(0, result.Log[0].LogData[0].Data.Count);

            // EndIndex on start of range
            result = DevKit.QueryLogByRange(log1, -2, 0);
            Assert.AreEqual(1, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);

            // StartIndex and EndIndex spans range
            result = DevKit.QueryLogByRange(log1, -2, 2.5);
            Assert.AreEqual(3, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[2]);

            // StartIndex on start of range
            result = DevKit.QueryLogByRange(log1, 0, 1.5);
            Assert.AreEqual(2, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);

            // StartIndex and EndIndex within range
            result = DevKit.QueryLogByRange(log1, 1.5, 3.5);
            Assert.AreEqual(2, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("3,,3", result.Log[0].LogData[0].Data[1]);

            // EndIndex on end of range
            result = DevKit.QueryLogByRange(log1, 7.5, 9);
            Assert.AreEqual(2, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[1]);

            // StartIndex and EndIndex span range
            result = DevKit.QueryLogByRange(log1, 7.5, 11);
            Assert.AreEqual(2, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[1]);

            // StartIndex on end of range
            result = DevKit.QueryLogByRange(log1, 9, 11);
            Assert.AreEqual(1, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[0]);

            // StartIndex and End Index below range
            result = DevKit.QueryLogByRange(log1, 10, 11);
            Assert.AreEqual(0, result.Log[0].LogData[0].Data.Count);
        }

        [TestMethod]
        public void WitsmlStore_can_query_log_data_by_startIndex()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitDataMany(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 10);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log1)));

            // StartIndex is above range
            LogList result = DevKit.QueryLogByRange(log1, -2, null);
            Assert.AreEqual(10, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[2]);
            Assert.AreEqual("3,,3", result.Log[0].LogData[0].Data[3]);
            Assert.AreEqual("4,,4", result.Log[0].LogData[0].Data[4]);
            Assert.AreEqual("5,,5", result.Log[0].LogData[0].Data[5]);
            Assert.AreEqual("6,,6", result.Log[0].LogData[0].Data[6]);
            Assert.AreEqual("7,,7", result.Log[0].LogData[0].Data[7]);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[8]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[9]);

            // StartIndex is on range
            result = DevKit.QueryLogByRange(log1, 0, null);
            Assert.AreEqual(10, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[2]);
            Assert.AreEqual("3,,3", result.Log[0].LogData[0].Data[3]);
            Assert.AreEqual("4,,4", result.Log[0].LogData[0].Data[4]);
            Assert.AreEqual("5,,5", result.Log[0].LogData[0].Data[5]);
            Assert.AreEqual("6,,6", result.Log[0].LogData[0].Data[6]);
            Assert.AreEqual("7,,7", result.Log[0].LogData[0].Data[7]);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[8]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[9]);

            // StartIndex is within range
            result = DevKit.QueryLogByRange(log1, 7.5, null);
            Assert.AreEqual(2, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[1]);

            // StartIndex is on end of range
            result = DevKit.QueryLogByRange(log1, 9, null);
            Assert.AreEqual(1, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[0]);

            // StartIndex is below end of range
            result = DevKit.QueryLogByRange(log1, 11, null);
            Assert.AreEqual(0, result.Log[0].LogData[0].Data.Count);
        }

        [TestMethod]
        public void WitsmlStore_can_query_log_data_by_endIndex()
        {
            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Add Log header
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = Log1.Uid, UidWell = Log1.UidWell, UidWellbore = Log1.UidWellbore };
            DevKit.InitDataMany(log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), 10);
            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(log1)));

            // EndIndex is above range
            LogList result = DevKit.QueryLogByRange(log1, null, -1);
            Assert.AreEqual(0, result.Log[0].LogData[0].Data.Count);

            // EndIndex is on start of range
            result = DevKit.QueryLogByRange(log1, null, 0);
            Assert.AreEqual(1, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);

            // EndIndex is within range
            result = DevKit.QueryLogByRange(log1, null, 3.5);
            Assert.AreEqual(4, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[2]);
            Assert.AreEqual("3,,3", result.Log[0].LogData[0].Data[3]);

            // EndIndex is on end of range
            result = DevKit.QueryLogByRange(log1, null, 9);
            Assert.AreEqual(10, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[2]);
            Assert.AreEqual("3,,3", result.Log[0].LogData[0].Data[3]);
            Assert.AreEqual("4,,4", result.Log[0].LogData[0].Data[4]);
            Assert.AreEqual("5,,5", result.Log[0].LogData[0].Data[5]);
            Assert.AreEqual("6,,6", result.Log[0].LogData[0].Data[6]);
            Assert.AreEqual("7,,7", result.Log[0].LogData[0].Data[7]);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[8]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[9]);

            // EndIndex is below end of range
            result = DevKit.QueryLogByRange(log1, null, 10);
            Assert.AreEqual(10, result.Log[0].LogData[0].Data.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0].Data[0]);
            Assert.AreEqual("1,,1", result.Log[0].LogData[0].Data[1]);
            Assert.AreEqual("2,,2", result.Log[0].LogData[0].Data[2]);
            Assert.AreEqual("3,,3", result.Log[0].LogData[0].Data[3]);
            Assert.AreEqual("4,,4", result.Log[0].LogData[0].Data[4]);
            Assert.AreEqual("5,,5", result.Log[0].LogData[0].Data[5]);
            Assert.AreEqual("6,,6", result.Log[0].LogData[0].Data[6]);
            Assert.AreEqual("7,,7", result.Log[0].LogData[0].Data[7]);
            Assert.AreEqual("8,,8", result.Log[0].LogData[0].Data[8]);
            Assert.AreEqual("9,,9", result.Log[0].LogData[0].Data[9]);
        }

        [TestMethod]
        public void WitsmlStore_bulk_add_data()
        {
            var dataRowsAdded = 500;

            // Add Well
            DevKit.Proxy.Write(DevKit.New<WellList>(x => x.Well = DevKit.List(Well1)));

            // Add Wellbore
            DevKit.Proxy.Write(DevKit.New<WellboreList>(x => x.Wellbore = DevKit.List(Wellbore1)));

            // Update Log with new LogData
            DevKit.InitDataMany(Log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), dataRowsAdded);

            // Add Log header and Data
            DevKit.Proxy.Write(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            var firstFactor = Log1.LogData[0].Data.Skip(1).ToList();

            Log1.LogData[0].Data.Clear();
            DevKit.InitDataMany(Log1, DevKit.Mnemonics(Log1), DevKit.Units(Log1), dataRowsAdded, 1.1);

            DevKit.Proxy.Update(DevKit.New<LogList>(x => x.Log = DevKit.List(Log1)));

            // Get Log by Uid
            var query = DevKit.Query<LogList>();
            query.Log = DevKit.One<Log>(x => { x.Uid = Log1.Uid; x.UidWell = Log1.UidWell; x.UidWellbore = Log1.UidWellbore; });

            var result = DevKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == Log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(1, result.Log[0].LogData.Count);

            Assert.IsNotNull(result.Log[0].LogData[0].Data);
            Assert.AreEqual(dataRowsAdded, result.Log[0].LogData[0].Data.Count);

            var secondFactor = result.Log[0].LogData[0].Data.Skip(1).ToList();

            Assert.AreEqual(firstFactor.Count, firstFactor.Except(secondFactor).Count());
            CollectionAssert.AreNotEqual(firstFactor, secondFactor);
        }

    }
}
