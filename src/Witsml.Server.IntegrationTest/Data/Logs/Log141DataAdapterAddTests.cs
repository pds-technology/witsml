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
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log141DataAdapterAddTests
    {
        private const int MicrosecondsPerSecond = 1000000;
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

            _wellbore = new Wellbore
            {
                Uid = _devKit.Uid(),
                UidWell = _well.Uid,
                NameWell = _well.Name,
                Name = _devKit.Name("Wellbore 01")
            };

            _log = _devKit.CreateLog(_devKit.Uid(), _devKit.Name("Log 01"), _well.Uid, _well.Name, _wellbore.Uid, _wellbore.Name);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            WitsmlSettings.DepthRangeSize = DevKitAspect.DefaultDepthChunkRange;
            WitsmlSettings.TimeRangeSize = DevKitAspect.DefaultTimeChunkRange;
            WitsmlSettings.MaxDataPoints = DevKitAspect.DefaultMaxDataPoints;
            WitsmlSettings.MaxDataNodes = DevKitAspect.DefaultMaxDataNodes;
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Depth_Log_Header()
        {
            AddParents();

            // check if log already exists
            var logResults = _devKit.Query<LogList, Log>(_log);
            if (!logResults.Any())
            {
                _devKit.InitHeader(_log, LogIndexType.measureddepth);
                var response = _devKit.Add<LogList, Log>(_log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Time_Log_Header()
        {
            AddParents();

            // check if log already exists
            var logResults = _devKit.Query<LogList, Log>(_log);
            if (!logResults.Any())
            {
                _devKit.InitHeader(_log, LogIndexType.datetime);
                var response = _devKit.Add<LogList, Log>(_log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Depth_Log_With_Data()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Time_Log_With_Data()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.datetime);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10, 1, false);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            GetLog(_log);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Increasing_Depth_Log()
        {
            AddParents();

            _log.StartIndex = new GenericMeasure(13, "ft");
            _log.EndIndex = new GenericMeasure(17, "ft");
            _log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("17,17.1,17.2");
            
            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(5, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;           
            var index = 13;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = int.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Decreasing_Depth_Log()
        {
            AddParents();

            _log.StartIndex = new GenericMeasure(13, "ft");
            _log.EndIndex = new GenericMeasure(17, "ft");
            _log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = _log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("17,17.1,17.2");

            _devKit.InitHeader(_log, LogIndexType.measureddepth, false);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(5, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 17;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = int.Parse(columns[0]);
                Assert.AreEqual(index, outIndex);

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Increasing_Time_Log()
        {
            AddParents();

            _log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            _log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            _log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = _log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");

            _devKit.InitHeader(_log, LogIndexType.datetime);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(6, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 30;
            DateTimeOffset? previousDateTime = null;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeMicroseconds() - previousDateTime.Value.ToUnixTimeMicroseconds()) == 60 * MicrosecondsPerSecond);
                }
                previousDateTime = outIndex;

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Decreasing_Time_Log()
        {
            AddParents();

            _log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            _log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            _log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            var logData = _log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");

            _devKit.InitHeader(_log, LogIndexType.datetime, false);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);
            Assert.AreEqual(1, result.LogData.Count);
            Assert.AreEqual(6, result.LogData[0].Data.Count);

            var resultLogData = result.LogData[0].Data;
            var index = 35;
            DateTimeOffset? previousDateTime = null;
            foreach (var row in resultLogData)
            {
                var columns = row.Split(',');
                var outIndex = DateTimeOffset.Parse(columns[0]);
                Assert.AreEqual(index, outIndex.Minute);
                if (previousDateTime.HasValue)
                {
                    Assert.IsTrue((outIndex.ToUnixTimeMicroseconds() - previousDateTime.Value.ToUnixTimeMicroseconds()) == -60 * MicrosecondsPerSecond);
                }
                previousDateTime = outIndex;

                var outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                var outColumn2 = double.Parse(columns[2]);
                Assert.AreEqual(index + 0.2, outColumn2);
                index--;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Decreasing_Log()
        {
            AddParents();

            _log.RunNumber = "101";
            _log.IndexCurve = "MD";
            _log.IndexType = LogIndexType.measureddepth;
            _log.Direction = LogIndexDirection.decreasing;

            _devKit.InitHeader(_log, _log.IndexType.Value, increasing: false);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 100, 0.9, increasing: false);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var logAdded = GetLog(_log);

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(LogIndexDirection.decreasing, logAdded.Direction);
            Assert.AreEqual(_log.RunNumber, logAdded.RunNumber);

            var logData = logAdded.LogData.FirstOrDefault();
            Assert.IsNotNull(logData);
            var firstIndex = int.Parse(logData.Data[0].Split(',')[0]);
            var secondIndex = int.Parse(logData.Data[1].Split(',')[0]);
            Assert.IsTrue(firstIndex > secondIndex);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Move_Index_Curve_To_First()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            var logCurves = _log.LogCurveInfo;
            var indexCurve = logCurves.First();
            logCurves.Remove(indexCurve);
            logCurves.Add(indexCurve);
            var firstCurve = _log.LogCurveInfo.First();
            Assert.AreNotEqual(indexCurve.Mnemonic.Value, firstCurve.Mnemonic.Value);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
         
            var logAdded = GetLog(_log);
            firstCurve = logAdded.LogCurveInfo.First();
            Assert.AreEqual(indexCurve.Mnemonic.Value, firstCurve.Mnemonic.Value);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Supports_NaN_In_Numeric_Fields()
        {
            AddParents();

            // Add log
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + _log.Uid + "\" uidWell=\"" + _log.UidWell + "\" uidWellbore=\"" + _log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + _well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + _wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Log 01") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        "<bhaRunNumber>NaN</bhaRunNumber>" + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"GR\">" + Environment.NewLine +
                        "  <mnemonic>GR</mnemonic>" + Environment.NewLine +
                        "  <unit>gAPI</unit>" + Environment.NewLine +
                        "  <classIndex>NaN</classIndex>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            var response = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);
            Assert.IsNull(result.BhaRunNumber);
            Assert.AreEqual(2, result.LogCurveInfo.Count);
            Assert.IsNull(result.LogCurveInfo[0].ClassIndex);
            Assert.IsNull(result.LogCurveInfo[1].ClassIndex);
        }

        [TestMethod, Description("To test adding a log with special characters")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Characters()
        {
            AddParents();

            // Add log
            var description = @"~ ! @ # $ % ^ &amp; * ( ) _ + { } | &lt; > ? ; : ' "" , . / \ [ ] and \b \f \n \r \t \"" \\ ";
            var expectedDescription = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' "" , . / \ [ ] and \b \f \n \r \t \"" \\";

            var row = @"~ ! @ # $ % ^ &amp; * ( ) _ + { } | &lt; > ? ; : ' "" . / \ [ ] and \b \f \n \r \t \"" \\ ";   // Comma omitted
            var expectedRow = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' "" . / \ [ ] and \b \f \n \r \t \"" \\";

            var xmlIn = FormatXmlIn(_log, $"<description>{description}</description>", $"<data>5000.1, {row}, 5.1</data>");

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(expectedDescription, returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(expectedRow, channelData[1].Trim());
        }

        [TestMethod, Description("To test adding a log with special character: &amp; (encoded ampersand)")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Characters_Encoded_Ampersand()
        {
            AddParents();

            // Add log          
            var description = "<description>Header &amp; </description>";
            var row = "<data>5000.1, Data &amp; , 5.1</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description.Trim());
            Assert.AreEqual("Header &", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual("Data &", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding a log with special characters &lt; (encoded less than)")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Characters_Encoded_Less_Than()
        {
            AddParents();

            // Add log          
            var description = "<description>Header &lt; </description>";
            var row = "<data>5000.1, Data &lt; , 5.1</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual("Header <", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual("Data <", channelData[1].Trim());
        }

        [TestMethod, Description(@"To test adding log data string channel with \ (backslash).")]
    
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Character_Backslash()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \ </description>";
            var row = @"<data>5000.0, Data \ , 5.0</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null); 
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \", channelData[1].Trim());
        }

        [TestMethod, Description("As comma is a delimiter, this test is served as a reminder of the problem and will need to be updated to the decided response of the server.")]
        [Ignore]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Character_Comma()
        {
            AddParents();

            // Add log          
            var description = "<description>Test special character , (comma) </description>";
            var row = "<data>5000.0, comma ,, 5.0</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);
            Assert.AreEqual(3, returnLog.LogData[0].Data[0].Split(',').Length);
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \f (form feed).")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Character_FormFeed()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \f </description>";
            var row = @"<data>5000.0, Data \f , 5.0</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \f", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \f", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \" (backslash double-quote).")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Character_Backslash_Double_Quote()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \""  </description>";
            var row = @"<data>5000.0, Data \"" , 5.0</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);              
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \""", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \""", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \b (backspace).")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Character_Backspace()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \b  </description>";
            var row = @"<data>5000.0, Data \b , 5.0</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);    
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \b", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \b", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \\ (double backslash).")]
        public void Log141DataAdapter_AddToStore_Add_Log_With_Special_Character_Double_Backslash()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \\ </description>";
            var row = @"<data>5000.0, Data \\ , 5.0</data>";

            var xmlIn = FormatXmlIn(_log, description, row);

            var result = _devKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(_log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \\", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \\", channelData[1].Trim());                       
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Rollback_When_Adding_Invalid_Data()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = new Log()
            {
                Uid = _devKit.Uid(),
                UidWell = _wellbore.UidWell,
                NameWell = _well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = _wellbore.Name,
                Name = _devKit.Name("Log 01"),
                LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() })
            };

            var logData = log.LogData.First();
            logData.Data.Add("997,13.1,");
            logData.Data.Add("998,14.1,");
            logData.Data.Add("999,15.1,");
            logData.Data.Add("1000,16.1,");
            logData.Data.Add("1001,17.1,");
            logData.Data.Add("1002,,21.2");
            logData.Data.Add("1002,,22.2");
            logData.Data.Add("1003,,23.2");
            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, response.Result);

            var query = new Log
            {
                Uid = log.Uid,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_With_Null_Indicator()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = _devKit.CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,-999.25");
            logData.Data.Add("1800.0,18.1,-999.25");
            logData.Data.Add("1900.0,19.1,-999.25");
            logData.Data.Add("2000.0,20.1,-999.25");
            logData.Data.Add("2100.0,21.1,-999.25");
            logData.Data.Add("2200.0,22.1,-999.25");

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = _devKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            Assert.AreEqual(2, results[0].LogData[0].MnemonicList.Split(',').Length);
            double index = 17;
            foreach (string row in resultLogData)
            {
                string[] columns = row.Split(',');
                Assert.AreEqual(2, columns.Length);

                double outIndex = double.Parse(columns[0]);
                Assert.AreEqual(index * 100, outIndex);

                double outColumn1 = double.Parse(columns[1]);
                Assert.AreEqual(index + 0.1, outColumn1);

                index++;
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_Null_Indicator_And_An_Empty_Channel_Of_Blanks()
        {
            var response = _devKit.Add<WellList, Well>(_well);

            _wellbore.UidWell = response.SuppMsgOut;
            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);

            var log = _devKit.CreateLog(null, _devKit.Name("Log 01"), _wellbore.UidWell, _well.Name, response.SuppMsgOut, _wellbore.Name);
            log.LogData = _devKit.List(new LogData() { Data = _devKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,");
            logData.Data.Add("1800.0,18.1,");
            logData.Data.Add("1900.0,19.1,");
            logData.Data.Add("2000.0,20.1,");
            logData.Data.Add("2100.0,21.1,");
            logData.Data.Add("2200.0,22.1,");

            _devKit.InitHeader(log, LogIndexType.measureddepth);

            response = _devKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = _devKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LogData.Count);
            Assert.AreEqual(6, results[0].LogData[0].Data.Count);

            var resultLogData = results[0].LogData[0].Data;
            Assert.AreEqual(2, results[0].LogData[0].MnemonicList.Split(',').Length);

            Assert.IsTrue(resultLogData[0].Equals("1700,17.1"));
            Assert.IsTrue(resultLogData[1].Equals("1800,18.1"));
            Assert.IsTrue(resultLogData[2].Equals("1900,19.1"));
            Assert.IsTrue(resultLogData[3].Equals("2000,20.1"));
            Assert.IsTrue(resultLogData[4].Equals("2100,21.1"));
            Assert.IsTrue(resultLogData[5].Equals("2200,22.1"));
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Structural_Ranges_Ignored()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);

            _log.StartIndex = new GenericMeasure {Uom = "m", Value = 1.0};
            _log.EndIndex = new GenericMeasure { Uom = "m", Value = 10.0 };

            foreach (var curve in _log.LogCurveInfo)
            {
                curve.MinIndex = _log.StartIndex;
                curve.MaxIndex = _log.EndIndex;
            }

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);

            Assert.IsNull(result.StartIndex);
            Assert.IsNull(result.EndIndex);

            Assert.AreEqual(_log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_No_LogCurveInfos()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _log.LogCurveInfo.Clear();
            _log.LogData.Clear();

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_Custom_Data_Delimiter()
        {
            var delimiter = "|";

            AddParents();

            // Set data delimiter to other charactrer than ","
            _log.DataDelimiter = delimiter;

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10, hasEmptyChannel:false);

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(_log);

            // Assert data delimiter
            Assert.AreEqual(delimiter, result.DataDelimiter);

            var data = result.LogData.FirstOrDefault()?.Data;
            Assert.IsNotNull(data);

            var channelCount = _log.LogCurveInfo.Count;

            // Assert data delimiter in log data
            foreach (var row in data)
            {
                var points = ChannelDataReader.Split(row, delimiter);
                Assert.AreEqual(channelCount, points.Length);
            }
        }
       
        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_With_Blank_Unit_In_LogCurveInfo_And_UnitList()
        {
            AddParents();

            _devKit.InitHeader(_log, LogIndexType.measureddepth);
            _devKit.InitDataMany(_log, _devKit.Mnemonics(_log), _devKit.Units(_log), 10);

            // Set the 3rd LogCurveInfo/unit to null and 3rd UnitList entry to an empty string
            _log.LogCurveInfo[2].Unit = null;
            var logData = _log.LogData.First();
            logData.UnitList = "m,m/h,";

            var response = _devKit.Add<LogList, Log>(_log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        #region Helper Methods

        private void AddParents()
        {
            var response = _devKit.Add<WellList, Well>(_well);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            response = _devKit.Add<WellboreList, Wellbore>(_wellbore);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        private Log GetLog(Log log)
        {
            var query = _devKit.CreateLog(log.Uid, null, log.UidWell, null, log.UidWellbore, null);
            var results = _devKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(1, results.Count);

            var result = results.FirstOrDefault();
            Assert.IsNotNull(result);

            return result;
        }

        private string FormatXmlIn(Log log, string description, string row)
        {
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + log.Uid + "\" uidWell=\"" + log.UidWell + "\" uidWellbore=\"" + log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + log.NameWell + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + log.NameWellbore + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + _devKit.Name("Test special characters") + "</name>" + Environment.NewLine +
                        "<indexType>measured depth</indexType>" + Environment.NewLine +
                        "<direction>increasing</direction>" + Environment.NewLine +
                        description + Environment.NewLine +
                        "<indexCurve>MD</indexCurve>" + Environment.NewLine +
                        "<logCurveInfo uid=\"MD\">" + Environment.NewLine +
                        "  <mnemonic>MD</mnemonic>" + Environment.NewLine +
                        "  <unit>m</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"AAA\">" + Environment.NewLine +
                        "  <mnemonic>AAA</mnemonic>" + Environment.NewLine +
                        "  <unit>unitless</unit>" + Environment.NewLine +
                        "  <typeLogData>string</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logCurveInfo uid=\"BBB\">" + Environment.NewLine +
                        "  <mnemonic>BBB</mnemonic>" + Environment.NewLine +
                        "  <unit>s</unit>" + Environment.NewLine +
                        "  <typeLogData>double</typeLogData>" + Environment.NewLine +
                        "</logCurveInfo>" + Environment.NewLine +
                        "<logData>" + Environment.NewLine +
                        "   <mnemonicList>MD,AAA,BBB</mnemonicList>" + Environment.NewLine +
                        "   <unitList>m,unitless,s</unitList>" + Environment.NewLine +
                        row + Environment.NewLine +
                        "</logData>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
               "</logs>";

            return xmlIn;
        }

        #endregion
    }
}
