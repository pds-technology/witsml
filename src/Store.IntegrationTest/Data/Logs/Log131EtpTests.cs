//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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

using System.Threading.Tasks;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ReferenceData;
using Energistics.Protocol;
using Energistics.Protocol.Store;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Compatibility;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    /// <summary>
    /// Log131EtpTests
    /// </summary>
    public partial class Log131EtpTests
    {

        [TestMethod, Description("Tests that 131 Log Data can be added when Compatibility Setting LogAllowPutObjectWithData is True")]
        public async Task Log131_PutObject_Can_Add_Log_Data_With_LogAllowPutObjectWithData_True()
        {
            AddParents();

            var numRows = 10;

            // Allow for Log data to be saved during a Put
            CompatibilitySettings.LogAllowPutObjectWithData = true;

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

            // Verify that the Log Data was saved.
            Assert.IsNotNull(result.LogData);
            Assert.AreEqual(numRows, result.LogData.Count);
        }
    }
}
