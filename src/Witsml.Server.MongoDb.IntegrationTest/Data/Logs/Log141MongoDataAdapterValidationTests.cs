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

using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    [Ignore] //Tests have been moved to witsml.Server.IntegrationTest.Data.Wells.Log141ValidatorTests
    public class Log141MongoDataAdapterValidationTests
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

        [TestCleanup]
        public void TestCleanup()
        {
            _devKit = null;
        }

        [TestMethod]
        public void Log141DataAdapter_GetFromStore_Error_475_No_Subset_When_Getting_Growing_Object()
        {
            AddParents();

            // Add first log
            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Add second log
            var log2 = _devKit.CreateLog(null, _devKit.Name("Log 02"), _log.UidWell, _log.NameWell, _log.UidWellbore,
                _log.NameWellbore);
            _devKit.InitHeader(log2, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log2);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            // Query
            var query = _devKit.CreateLog(null, null, log2.UidWell, null, log2.UidWellbore, null);

            var result = _devKit.Get<LogList, Log>(_devKit.List(query), ObjectTypes.Log, null,
                OptionsIn.ReturnElements.DataOnly);
            Assert.AreEqual((short)ErrorCodes.MissingSubsetOfGrowingDataObject, result.Result);
        }

        [TestMethod]
        public void MongoDbUpdate_UpdateInStore_Error_484_Empty_Value_For_Mandatory_Field()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            var xmlIn = "<logs version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" +
                "<log uidWell=\"" + _log.UidWell + "\" uidWellbore=\"" + _log.UidWellbore + "\" uid=\"" + uidLog + "\">" +
                    "<nameWell />" +
                "</log>" +
                "</logs>";

            var updateResponse = _devKit.UpdateInStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.MissingRequiredData, updateResponse.Result);
        }

        [TestMethod]
        public void MongoDbUpdate_UpdateInStore_Error_445_Empty_New_Element()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var update = new Log()
            {
                Uid = _log.Uid,
                UidWell = _log.UidWell,
                UidWellbore = _log.UidWellbore
            };

            update.LogCurveInfo = new List<LogCurveInfo>
            {
                new LogCurveInfo { Uid = "ExtraCurve" }
            };

            var updateResponse = _devKit.Update<LogList, Log>(update);
            Assert.AreEqual((short)ErrorCodes.EmptyNewElementsOrAttributes, updateResponse.Result);
        }

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }
    }
}
