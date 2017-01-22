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

using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    /// <summary>
    /// Log131DataAdapter Get tests.
    /// </summary>
    [TestClass]
    public partial class Log131DataAdapterGetTests : Log131TestBase
    {
        protected override void OnTestSetUp()
        {
            // Sets the depth and time chunk size
            WitsmlSettings.DepthRangeSize = 1000;
            WitsmlSettings.TimeRangeSize = 86400000000; // Number of microseconds equals to one day
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_Header_Only_No_Data()
        {
            int numRows = 10;

            // Add the Setup Well, Wellbore and Log to the store.
            var logResponse = AddSetupWellWellboreLog(numRows, isDepthLog: true, hasEmptyChannel: false, increasing: true);
            Assert.AreEqual((short)ErrorCodes.Success, logResponse.Result);

            var queryHeaderOnly = DevKit.CreateLog(logResponse.SuppMsgOut, null, Log.UidWell, null, Log.UidWellbore, null);

            // Perform a GetFromStore with multiple log queries
            var result = DevKit.Get<LogList, Log>(
                DevKit.List(queryHeaderOnly),
                ObjectTypes.Log,
                null,
                OptionsIn.ReturnElements.HeaderOnly);

            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            var logList = EnergisticsConverter.XmlToObject<LogList>(result.XMLout);
            Assert.IsNotNull(logList);
            Assert.AreEqual(1, logList.Log.Count);
            Assert.AreEqual(0, logList.Log[0].LogData.Count);
        }

        [TestMethod]
        public void Log131DataAdapter_GetFromStore_Request_Multiple_Recurring_Items_With_Empty_Value()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);
            var responseAddLog = DevKit.Add<LogList, Log>(Log);
            Assert.IsNotNull(responseAddLog);
            Assert.AreEqual((short)ErrorCodes.Success, responseAddLog.Result);

            var queryIn = @"
                            <logs xmlns=""http://www.witsml.org/schemas/131"" version=""1.3.1.1"">      
                                <log uid=""" + Log.Uid + @""">
                                    <logCurveInfo> 
                                        <mnemonic>ROP</mnemonic> 
                                        <unit /> 
                                    </logCurveInfo>
                                    <logCurveInfo>
                                        <mnemonic>{0}</mnemonic> 
                                        <unit /> 
                                    </logCurveInfo>
                                </log>
                            </logs>";

            var response = DevKit.GetFromStore(ObjectTypes.Log, string.Format(queryIn, "GR"), null, null);
            Assert.IsNotNull(response);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var responseError = DevKit.GetFromStore(ObjectTypes.Log, string.Format(queryIn, ""), null, null);
            Assert.IsNotNull(responseError);
            Assert.AreEqual((short)ErrorCodes.RecurringItemsEmptySelection, responseError.Result);
        }

        #region Helper Methods

        private WMLS_AddToStoreResponse AddSetupWellWellboreLog(int numRows, bool isDepthLog, bool hasEmptyChannel, bool increasing)
        {
            DevKit.Add<WellList, Well>(Well);
            DevKit.Add<WellboreList, Wellbore>(Wellbore);
            DevKit.InitHeader(Log, LogIndexType.measureddepth, increasing);

            var startIndex = new GenericMeasure { Uom = "m", Value = 100 };
            Log.StartIndex = startIndex;
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), numRows, 1, isDepthLog, hasEmptyChannel, increasing);

            // Add a log
            return DevKit.Add<LogList, Log>(Log);
        }
        #endregion Helper Methods
    }
}
