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

using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Energistics.Etp.v11.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log141EtpTests
    /// </summary>
    public partial class Log141EtpTests
    {
        [TestMethod, Description("Tests that 141 Log Data can be added when Compatibility Setting LogAllowPutObjectWithData is True")]
        public async Task Log141_PutObject_Can_Add_Log_Data_With_LogAllowPutObjectWithData_True()
        {
            const int numRows = 10;
            const bool allowPutData = true;

            await Log141_PutObject_Can_Add_Log_Data_With_LogAllowPutObjectWithData(numRows, allowPutData);
        }

        [TestMethod, Description("Tests that 141 Log Data cannot be added when Compatibility Setting LogAllowPutObjectWithData is False")]
        public async Task Log141_PutObject_Can_Add_Log_Data_With_LogAllowPutObjectWithData_False()
        {
            const int numRows = 10;
            const bool allowPutData = false;

            await Log141_PutObject_Can_Add_Log_Data_With_LogAllowPutObjectWithData(numRows, allowPutData);
        }

        private async Task Log141_PutObject_Can_Add_Log_Data_With_LogAllowPutObjectWithData(int numRows, bool allowPutData)
        {
            AddParents();

            // Allow for Log data to be saved during a Put
            CompatibilitySettings.LogAllowPutObjectWithData = allowPutData;

            await RequestSessionAndAssert();

            var handler = _client.Handler<IStoreCustomer>();
            var uri = Log.GetUri();
            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), numRows);

            var dataObject = CreateDataObject<LogList, Log>(uri, Log);

            // Put Object
            await PutAndAssert(handler, dataObject);

            // Get Object
            var result = DevKit.GetAndAssert<LogList, Log>(DevKit.CreateLog(Log));

            // Verify that the Log was saved
            Assert.IsNotNull(result);

            // Verify the number of Log Data saved.
            var rowsExpected = allowPutData ? numRows : 0;
            var logDataExpected = allowPutData ? 1 : 0;
            Assert.IsNotNull(result.LogData);
            Assert.AreEqual(logDataExpected, result.LogData.Count);

            if (allowPutData)
            {
                Assert.AreEqual(rowsExpected, result.LogData[0].Data.Count);
            }
        }
    }
}
