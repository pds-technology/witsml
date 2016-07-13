//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Channels
{
    [TestClass]
    public class ChannelDataChunkAdapterValidationTests
    {
        private DevKit141Aspect _devKit;
        private Well _well;
        private Wellbore _wellbore;
        private Log _log;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _devKit = new DevKit141Aspect(TestContext);

            _devKit.Store.CapServerProviders = _devKit.Store.CapServerProviders
                .Where(x => x.DataSchemaVersion == OptionsIn.DataVersion.Version141.Value)
                .ToArray();

            _well = new Well { Uid = _devKit.Uid(), Name = _devKit.Name("Well 01"), TimeZone = _devKit.TimeZone };

            _wellbore = new Wellbore()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = new Log()
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                UidWellbore = _wellbore.Uid,
                NameWell = _well.Name,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01")
            };
        }

        [TestMethod]
        public void ChannelDataChunkAdapter_AddToStore_Error_463_Nodes_With_Same_Index()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var logData = _devKit.List<string>();
            _log.LogData = _devKit.List(new LogData() {Data = logData});

            logData.Add("13,13.1,");
            logData.Add("14,14.1,");
            logData.Add("15,15.1,");
            logData.Add("15,16.1,");
            logData.Add("17,17.1,");
            logData.Add("21,,21.2");
            logData.Add("22,,22.2");
            logData.Add("23,,23.2");
            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, response.Result);
        }
    }
}
