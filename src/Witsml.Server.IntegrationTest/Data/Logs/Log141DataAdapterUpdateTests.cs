//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log141DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public class Log141DataAdapterUpdateTests
    {
        //[TestMethod]
        //public void Log141DataAdapter_MethodName_ExpectedBehavior()
        //{
        //}

        private DevKit141Aspect DevKit;
        private Well Well;
        private Wellbore Wellbore;
        private Log Log;

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

            Log = new Log()
            {
                NameWell = Well.Name,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01")
            };
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Supports_NaN_In_Numeric_Fields()
        {
            // Add well
            var response = DevKit.Add<WellList, Well>(Well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add wellbore
            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidWellbore = response.SuppMsgOut;

            // Add log
            Log.UidWell = Wellbore.UidWell;
            Log.UidWellbore = uidWellbore;
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 3);
            Log.BhaRunNumber = 123;
            Log.LogCurveInfo[0].ClassIndex = 1;
            Log.LogCurveInfo[1].ClassIndex = 2;

            response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Update log
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + uidLog + "\" uidWell=\"" + Wellbore.UidWell + "\" uidWellbore=\"" + uidWellbore + "\">" + Environment.NewLine +                        
                        "<bhaRunNumber>NaN</bhaRunNumber>" + Environment.NewLine +                       
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +                                            
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +                       
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"GR\">" + Environment.NewLine +                                       
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +                       
                        "</logCurveInfo>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var result = DevKit.UpdateInStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var query = DevKit.CreateLog(uidLog, null, Wellbore.UidWell, null, uidWellbore, null);

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsTrue(results.Any());

            Assert.AreEqual((short)123, results.First().BhaRunNumber);
            Assert.AreEqual(3, results.First().LogCurveInfo.Count);
            Assert.AreEqual((short)1, results.First().LogCurveInfo[0].ClassIndex);
            Assert.AreEqual((short)2, results.First().LogCurveInfo[1].ClassIndex);
        }

        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Rollback_When_Updating_Invalid_Data()
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
            var logData = log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("14,14.1,");
            logData.Data.Add("15,15.1,15.2");

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var query = new Log
            {
                Uid = uidLog,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);            

            var logAdded = results.First();
            Assert.IsNull(logAdded.Description);

            var logDataAdded = logAdded.LogData.First();
            for (var i = 0; i < logData.Data.Count; i++)
            {
                Assert.AreEqual(logData.Data[i], logDataAdded.Data[i]);
            }

            var update = new Log()
            {
                Uid = uidLog,
                UidWell = Wellbore.UidWell,
                UidWellbore = uidWellbore,
                Description = "Should not be updated"
            };

            DevKit.InitHeader(update, LogIndexType.measureddepth);
            logData = update.LogData.First();          
            logData.Data.Add("17,17.1,17.2");
            logData.Data.Add("21,21.1,21.2");
            logData.Data.Add("21,22.1,22.2");

            var updateResponse = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, updateResponse.Result);

            results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var logUpdated = results.First();
            Assert.IsNull(logUpdated.Description);

            var logDataUpdated = logUpdated.LogData.First();
            for (var i = 0; i < logDataAdded.Data.Count; i++)
            {
                Assert.AreEqual(logDataAdded.Data[i], logDataUpdated.Data[i]);
            }
        }

        /// <summary>
        /// To test concurrency lock for update: open 2 visual studio and debug the following test at the same time;
        /// lock one test, i.e. break at the commit statement and check if the 2nd thread is repeatedly checking if
        /// the transaction has been released every 2 seconds
        /// </summary>
        [TestMethod]
        public void Log141DataAdapter_UpdateInStore_Lock_Transaction()
        {
            Well.Uid = "Parent Well - Testing Lock";
            Wellbore.UidWell = Well.Uid;
            Wellbore.Uid = "Parent Wellbore - Testing Lock";           
            Log.UidWell = Well.Uid;
            Log.UidWellbore = Wellbore.Uid;
            Log.Uid = "Log - Testing Lock";
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var queryWell = new Well { Uid = Well.Uid };
            var resultWell = DevKit.Query<WellList, Well>(queryWell, optionsIn: OptionsIn.ReturnElements.All);
            if (resultWell.Count == 0)
            {
                var addWell = DevKit.Add<WellList, Well>(Well);
            }

            var queryWellbore = new Wellbore { Uid = Wellbore.Uid, UidWell = Wellbore.UidWell };
            var resultWellbore = DevKit.Query<WellboreList, Wellbore>(queryWellbore, optionsIn: OptionsIn.ReturnElements.All);
            if (resultWellbore.Count == 0)
            {
                var addWellbore = DevKit.Add<WellboreList, Wellbore>(Wellbore);
            }

            var queryLog = new Log { Uid = Log.Uid, UidWell = Log.UidWell, UidWellbore = Log.UidWellbore };
            var resultLog = DevKit.Query<LogList, Log>(queryLog, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            if (resultLog.Count == 0)
            {
                var addLog = DevKit.Add<LogList, Log>(Log);
            }

            var update = new Log
            {
                Uid = Log.Uid,
                UidWell = Log.UidWell,
                UidWellbore = Log.UidWellbore,
                Description = "Update Description"
            };

            var response = DevKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
