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
                        "<bhaRunNumber>123</bhaRunNumber>" + Environment.NewLine +                       
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +                                            
                        "  <classIndex>1</classIndex>" + Environment.NewLine +                       
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"GR\">" + Environment.NewLine +                                       
                        "  <classIndex>2</classIndex>" + Environment.NewLine +                       
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
    }
}
