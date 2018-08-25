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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ReferenceData;
using System;
using Energistics.DataAccess;

namespace PDS.WITSMLstudio.Store
{
    [TestClass]
    public class WitsmlStore131ProxyTests
    {
        private DevKit131Aspect _devKit;
        private Well _well1;
        private Well _well2;
        private Wellbore _wellbore1;
        private Wellbore _wellbore2;
        private Log _log1;
        private Log _log2;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            var url = "http://localhost/Witsml.Web/WitsmlStore.svc"; // IIS
            //var url = "http://localhost:5050/WitsmlStore.svc"; // TestApp
            _devKit = new DevKit131Aspect(TestContext, url);

            _well1 = new Well() { Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone, Uid = _devKit.Uid() };
            _well2 = new Well() { Name = _devKit.Name("Well 02"), TimeZone = _devKit.TimeZone };

            _wellbore1 = new Wellbore() { UidWell = _well1.Uid, NameWell = _well1.Name, Name = _devKit.Name("Wellbore 01-01"), Uid = _devKit.Uid() };
            _wellbore2 = new Wellbore() { UidWell = _well1.Uid, NameWell = _well1.Name, Name = _devKit.Name("Wellbore 01-02") };

            _log1 = new Log() { UidWell = _well1.Uid, NameWell = _well1.Name, UidWellbore = _wellbore1.Uid, NameWellbore = _wellbore1.Name, Name = _devKit.Name("Log 01"), Uid = _devKit.Uid() };
            _log2 = new Log() { UidWell = _well1.Uid, NameWell = _well1.Name, UidWellbore = _wellbore1.Uid, NameWellbore = _wellbore1.Name, Name = _devKit.Name("Log 02") };

            _devKit.InitHeader(_log1, LogIndexType.measureddepth);
            _devKit.InitHeader(_log2, LogIndexType.datetime);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_GetVersion_Can_Get_Version()
        {
            var response = _devKit.Proxy.GetVersion();
            Console.WriteLine("DataSchemaVersions: {0}", response);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_GetCap_Can_Get_Cap_Server()
        {
            var response = _devKit.Proxy.GetCap<CapServers>();
            Console.WriteLine(EnergisticsConverter.ObjectToXml(response));
            Assert.IsNotNull(response);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Write_And_Query_All_Wells()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well2)));

            // Get all Wells
            var query = _devKit.Query<WellList>();
            var result = _devKit.Proxy.Read(query);

            Assert.IsNotNull(result.Well);
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Name == _well1.Name));
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Name == _well2.Name));
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Well_By_Uid()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well2)));

            // Get Well by Uid
            var query = _devKit.Query<WellList>();
            query.Well = _devKit.One<Well>(x => x.Uid = _well1.Uid);

            var result = _devKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Well.Count);
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Uid == _well1.Uid));
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Well_By_Name()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well2)));

            // Get Well by Name
            var query = _devKit.Query<WellList>();
            query.Well = _devKit.One<Well>(x => x.Name = _well2.Name);

            var result = _devKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Well.Count);
            Assert.IsNotNull(result.Well.SingleOrDefault(x => x.Name == _well2.Name));
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Write_And_Query_All_Wellbores()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well2)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore2)));

            // Get All Wellbores
            var query = _devKit.Query<WellboreList>();
            var result = _devKit.Proxy.Read(query);

            Assert.IsNotNull(result.Wellbore);
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Name == _wellbore1.Name));
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Name == _wellbore2.Name));
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Wellbore_By_Uid()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore2)));

            // Get Wellbore by Uid
            var query = _devKit.Query<WellboreList>();
            query.Wellbore = _devKit.One<Wellbore>(x => x.Uid = _wellbore1.Uid);

            var result = _devKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Wellbore.Count);
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Uid == _wellbore1.Uid));
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Wellbore_For_A_Well_By_Name()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore2)));

            // Get Wellbore by Name
            var query = _devKit.Query<WellboreList>();
            query.Wellbore = _devKit.List(new Wellbore() { Name = _wellbore2.Name, NameWell = _wellbore2.NameWell });

            var result = _devKit.Proxy.Read(query);

            Assert.AreEqual(1, result.Wellbore.Count);
            Assert.IsNotNull(result.Wellbore.SingleOrDefault(x => x.Name == _wellbore2.Name && x.NameWell == _wellbore2.NameWell));
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Write_And_Query_All_Log_Headers()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log2)));

            // Get Log header by Uid
            var query = _devKit.Query<LogList>();
            query.Log = _devKit.One<Log>(x => x.Uid = _log1.Uid);

            var result = _devKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == _log1.Uid));

            Assert.IsNotNull(result.Log[0].LogCurveInfo);
            Assert.AreEqual(3, result.Log[0].LogCurveInfo.Count);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Log_Header_For_A_Wellbore_By_Name()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log2)));

            // Get Log header by Name (must be by Well, Wellbore and Log name to be unique)
            var query = _devKit.Query<LogList>();
            query.Log = _devKit.One<Log>(x => 
            {
                x.NameWell = _log1.NameWell;
                x.NameWellbore = _log1.NameWellbore;
                x.Name = _log1.Name;
            });

            var result = _devKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == _log1.Uid));

            Assert.IsNotNull(result.Log[0].LogCurveInfo);
            Assert.AreEqual(3, result.Log[0].LogCurveInfo.Count);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Write_And_Query_Log_Data()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitData(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0, null, 0);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log1)));

            // Get Log by Uid
            var query = _devKit.Query<LogList>();
            query.Log = _devKit.One<Log>(x => { x.Uid = _log1.Uid; x.UidWell = _log1.UidWell; x.UidWellbore = _log1.UidWellbore; });

            var result = _devKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == _log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(1, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Append_Log_Data()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitData(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0, null, 0);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log1)));

            // Update Log with appended LogData
            var log2 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitData(log2, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0.1, null, 1);
            _devKit.InitData(log2, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0.2, null, 2);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log2)));

            // Get Log by Uid
            var query = _devKit.Query<LogList>();
            query.Log = _devKit.One<Log>(x => { x.Uid = _log1.Uid; x.UidWell = _log1.UidWell; x.UidWellbore = _log1.UidWellbore; });

            var result = _devKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == _log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(3, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("0.1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("0.2,2", result.Log[0].LogData[2]);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Update_Log_Data()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitData(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0, null, 0);
            _devKit.InitData(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0.1, null, 1);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log1)));

            // Update Log with updated LogData
            var log2 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitData(log2, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 0.1, 10, 1.1);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log2)));

            // Get Log by Uid
            var query = _devKit.Query<LogList>();
            query.Log = _devKit.One<Log>(x => { x.Uid = _log1.Uid; x.UidWell = _log1.UidWell; x.UidWellbore = _log1.UidWellbore; });

            var result = _devKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == _log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(2, result.Log[0].LogData.Count);
            Assert.AreEqual("0,,0", result.Log[0].LogData[0]);
            Assert.AreEqual("0.1,10,1.1", result.Log[0].LogData[1]);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Log_Data_By_StartIndex_EndIndex()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitDataMany(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 10);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log1)));

            // StartIndex and End Index above range
            LogList result = _devKit.QueryLogByRange(log1, -2, -1);
            Assert.AreEqual(0, result.Log.Count);

            // EndIndex on start of range
            result = _devKit.QueryLogByRange(log1, -2, 0);
            Assert.AreEqual(1, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);

            // StartIndex and EndIndex spans range
            result = _devKit.QueryLogByRange(log1, -2, 2.5);
            Assert.AreEqual(3, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("2,2", result.Log[0].LogData[2]);

            // StartIndex on start of range
            result = _devKit.QueryLogByRange(log1, 0, 1.5);
            Assert.AreEqual(2, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);

            // StartIndex and EndIndex within range
            result = _devKit.QueryLogByRange(log1, 1.5, 3.5);
            Assert.AreEqual(2, result.Log[0].LogData.Count);
            Assert.AreEqual("2,2", result.Log[0].LogData[0]);
            Assert.AreEqual("3,3", result.Log[0].LogData[1]);

            // EndIndex on end of range
            result = _devKit.QueryLogByRange(log1, 7.5, 9);
            Assert.AreEqual(2, result.Log[0].LogData.Count);
            Assert.AreEqual("8,8", result.Log[0].LogData[0]);
            Assert.AreEqual("9,9", result.Log[0].LogData[1]);

            // StartIndex and EndIndex span range
            result = _devKit.QueryLogByRange(log1, 7.5, 11);
            Assert.AreEqual(2, result.Log[0].LogData.Count);
            Assert.AreEqual("8,8", result.Log[0].LogData[0]);
            Assert.AreEqual("9,9", result.Log[0].LogData[1]);

            // StartIndex on end of range
            result = _devKit.QueryLogByRange(log1, 9, 11);
            Assert.AreEqual(1, result.Log[0].LogData.Count);
            Assert.AreEqual("9,9", result.Log[0].LogData[0]);

            // StartIndex and End Index below range
            result = _devKit.QueryLogByRange(log1, 10, 11);
            Assert.AreEqual(0, result.Log.Count);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Log_Data_By_StartIndex()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitDataMany(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 10);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log1)));

            // StartIndex is above range
            LogList result = _devKit.QueryLogByRange(log1, -2, null);
            Assert.AreEqual(10, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("2,2", result.Log[0].LogData[2]);
            Assert.AreEqual("3,3", result.Log[0].LogData[3]);
            Assert.AreEqual("4,4", result.Log[0].LogData[4]);
            Assert.AreEqual("5,5", result.Log[0].LogData[5]);
            Assert.AreEqual("6,6", result.Log[0].LogData[6]);
            Assert.AreEqual("7,7", result.Log[0].LogData[7]);
            Assert.AreEqual("8,8", result.Log[0].LogData[8]);
            Assert.AreEqual("9,9", result.Log[0].LogData[9]);

            // StartIndex is on range
            result = _devKit.QueryLogByRange(log1, 0, null);
            Assert.AreEqual(10, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("2,2", result.Log[0].LogData[2]);
            Assert.AreEqual("3,3", result.Log[0].LogData[3]);
            Assert.AreEqual("4,4", result.Log[0].LogData[4]);
            Assert.AreEqual("5,5", result.Log[0].LogData[5]);
            Assert.AreEqual("6,6", result.Log[0].LogData[6]);
            Assert.AreEqual("7,7", result.Log[0].LogData[7]);
            Assert.AreEqual("8,8", result.Log[0].LogData[8]);
            Assert.AreEqual("9,9", result.Log[0].LogData[9]);

            // StartIndex is within range
            result = _devKit.QueryLogByRange(log1, 7.5, null);
            Assert.AreEqual(2, result.Log[0].LogData.Count);
            Assert.AreEqual("8,8", result.Log[0].LogData[0]);
            Assert.AreEqual("9,9", result.Log[0].LogData[1]);

            // StartIndex is on end of range
            result = _devKit.QueryLogByRange(log1, 9, null);
            Assert.AreEqual(1, result.Log[0].LogData.Count);
            Assert.AreEqual("9,9", result.Log[0].LogData[0]);

            // StartIndex is below end of range
            result = _devKit.QueryLogByRange(log1, 11, null);
            Assert.AreEqual(0, result.Log.Count);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Query_Log_Data_By_EndIndex()
        {
            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Add Log header
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Update Log with new LogData
            var log1 = new Log() { Uid = _log1.Uid, UidWell = _log1.UidWell, UidWellbore = _log1.UidWellbore, LogCurveInfo = _log1.LogCurveInfo };
            _devKit.InitDataMany(log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), 10);
            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(log1)));

            // EndIndex is above range
            LogList result = _devKit.QueryLogByRange(log1, null, -1);
            Assert.AreEqual(0, result.Log.Count);

            // EndIndex is on start of range
            result = _devKit.QueryLogByRange(log1, null, 0);
            Assert.AreEqual(1, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);

            // EndIndex is within range
            result = _devKit.QueryLogByRange(log1, null, 3.5);
            Assert.AreEqual(4, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("2,2", result.Log[0].LogData[2]);
            Assert.AreEqual("3,3", result.Log[0].LogData[3]);

            // EndIndex is on end of range
            result = _devKit.QueryLogByRange(log1, null, 9);
            Assert.AreEqual(10, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("2,2", result.Log[0].LogData[2]);
            Assert.AreEqual("3,3", result.Log[0].LogData[3]);
            Assert.AreEqual("4,4", result.Log[0].LogData[4]);
            Assert.AreEqual("5,5", result.Log[0].LogData[5]);
            Assert.AreEqual("6,6", result.Log[0].LogData[6]);
            Assert.AreEqual("7,7", result.Log[0].LogData[7]);
            Assert.AreEqual("8,8", result.Log[0].LogData[8]);
            Assert.AreEqual("9,9", result.Log[0].LogData[9]);

            // EndIndex is below end of range
            result = _devKit.QueryLogByRange(log1, null, 10);
            Assert.AreEqual(10, result.Log[0].LogData.Count);
            Assert.AreEqual("0,0", result.Log[0].LogData[0]);
            Assert.AreEqual("1,1", result.Log[0].LogData[1]);
            Assert.AreEqual("2,2", result.Log[0].LogData[2]);
            Assert.AreEqual("3,3", result.Log[0].LogData[3]);
            Assert.AreEqual("4,4", result.Log[0].LogData[4]);
            Assert.AreEqual("5,5", result.Log[0].LogData[5]);
            Assert.AreEqual("6,6", result.Log[0].LogData[6]);
            Assert.AreEqual("7,7", result.Log[0].LogData[7]);
            Assert.AreEqual("8,8", result.Log[0].LogData[8]);
            Assert.AreEqual("9,9", result.Log[0].LogData[9]);
        }

        [TestMethod]
        public void WITSMLWebServiceConnection_Can_Write_Bulk_Log_Data()
        {
            var dataRowsAdded = 500;

            // Add Well
            _devKit.Proxy.Write(_devKit.New<WellList>(x => x.Well = _devKit.List(_well1)));

            // Add Wellbore
            _devKit.Proxy.Write(_devKit.New<WellboreList>(x => x.Wellbore = _devKit.List(_wellbore1)));

            // Update Log with new LogData
            _devKit.InitDataMany(_log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), dataRowsAdded);

            // Add Log header and Data
            _devKit.Proxy.Write(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            var firstFactor = _log1.LogData.Skip(1).ToList();

            _log1.LogData.Clear();
            _devKit.InitDataMany(_log1, _devKit.Mnemonics(_log1), _devKit.Units(_log1), dataRowsAdded, 1.1);

            _devKit.Proxy.Update(_devKit.New<LogList>(x => x.Log = _devKit.List(_log1)));

            // Get Log by Uid
            var query = _devKit.Query<LogList>();
            query.Log = _devKit.One<Log>(x => { x.Uid = _log1.Uid; x.UidWell = _log1.UidWell; x.UidWellbore = _log1.UidWellbore; });

            var result = _devKit.Proxy.Read(query, OptionsIn.ReturnElements.All);

            Assert.IsNotNull(result.Log);
            Assert.AreEqual(1, result.Log.Count);
            Assert.IsNotNull(result.Log.SingleOrDefault(x => x.Uid == _log1.Uid));

            Assert.IsNotNull(result.Log[0].LogData);
            Assert.AreEqual(dataRowsAdded, result.Log[0].LogData.Count);

            var secondFactor = result.Log[0].LogData.Skip(1).ToList();

            Assert.AreEqual(firstFactor.Count, firstFactor.Except(secondFactor).Count());
            CollectionAssert.AreNotEqual(firstFactor, secondFactor);
        }
    }
}
