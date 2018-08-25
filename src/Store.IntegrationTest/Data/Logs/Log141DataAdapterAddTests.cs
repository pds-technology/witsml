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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Compatibility;
using PDS.WITSMLstudio.Data.Channels;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    [TestClass]
    public partial class Log141DataAdapterAddTests : Log141TestBase
    {
        private const int MicrosecondsPerSecond = 1000000;

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_DepthLog_Header()
        {
            AddParents();

            // check if log already exists
            var logResults = DevKit.Query<LogList, Log>(Log);
            if (!logResults.Any())
            {
                DevKit.InitHeader(Log, LogIndexType.measureddepth);
                var response = DevKit.Add<LogList, Log>(Log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_TimeLog_Header()
        {
            AddParents();

            // check if log already exists
            var logResults = DevKit.Query<LogList, Log>(Log);
            if (!logResults.Any())
            {
                DevKit.InitHeader(Log, LogIndexType.datetime);
                var response = DevKit.Add<LogList, Log>(Log);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_DepthLog_With_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_TimeLog_With_Data()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.datetime);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10, 1, false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            GetLog(Log);
        }

        [TestMethod]
        [Ignore, Description("For Benchmarking")]
        public void Log141DataAdapter_AddToStore_Benchmark_Add_TimeLog_With_Data()
        {
            AddParents();
            var sw = new System.Diagnostics.Stopwatch();
            var times = new List<long>();

            DevKit.InitHeader(Log, LogIndexType.datetime);

            // Add 40 more mnemonics
            for (int i = 0; i < 40; i++)
            {
                Log.LogCurveInfo.Add(DevKit.LogGenerator.CreateDoubleLogCurveInfo($"Curve{i}", "m"));
            }

            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 50, 1, false, false);

            for (int i = 0; i < Log.LogData[0].Data.Count; i++)
            {
                for (int x = 0; x < Log.LogCurveInfo.Count - 3; x++)
                {
                    Log.LogData[0].Data[i] += $",{i}";
                }
            }

            for (int i = 0; i < 50; i++)
            {
                Log.Uid = DevKit.Uid();
                Log.Name = DevKit.Name($"Benchmark{i}");

                // Measure time taken to add log to store
                sw.Restart();
                var response = DevKit.Add<LogList, Log>(Log);
                sw.Stop();

                // Ignore first add in case server was restarted
                if (i != 0)
                    times.Add(sw.ElapsedMilliseconds);
                Assert.AreEqual((short)ErrorCodes.Success, response.Result);
            }

            Assert.IsTrue(times.Max() < 5000);
            Assert.IsTrue(times.Average() < 5000);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Increasing_DepthLog()
        {
            AddParents();

            Log.StartIndex = new GenericMeasure(13, "ft");
            Log.EndIndex = new GenericMeasure(17, "ft");
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("17,17.1,17.2");
            
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);
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
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Decreasing_DepthLog()
        {
            AddParents();

            Log.StartIndex = new GenericMeasure(13, "ft");
            Log.EndIndex = new GenericMeasure(17, "ft");
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("13,13.1,13.2");
            logData.Data.Add("16,16.1,16.2");
            logData.Data.Add("15,15.1,15.2");
            logData.Data.Add("14,14.1,14.2");
            logData.Data.Add("17,17.1,17.2");

            DevKit.InitHeader(Log, LogIndexType.measureddepth, false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);
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
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Increasing_TimeLog()
        {
            AddParents();

            Log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            Log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");

            DevKit.InitHeader(Log, LogIndexType.datetime);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);
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
        public void Log141DataAdapter_AddToStore_Add_Unsequenced_Decreasing_TimeLog()
        {
            AddParents();

            Log.StartDateTimeIndex = new Energistics.DataAccess.Timestamp();
            Log.EndDateTimeIndex = new Energistics.DataAccess.Timestamp();
            Log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            var logData = Log.LogData.First();
            logData.Data.Add("2016-04-13T15:30:42.0000000-05:00,30.1,30.2");
            logData.Data.Add("2016-04-13T15:35:42.0000000-05:00,35.1,35.2");
            logData.Data.Add("2016-04-13T15:31:42.0000000-05:00,31.1,31.2");
            logData.Data.Add("2016-04-13T15:32:42.0000000-05:00,32.1,32.2");
            logData.Data.Add("2016-04-13T15:33:42.0000000-05:00,33.1,33.2");
            logData.Data.Add("2016-04-13T15:34:42.0000000-05:00,34.1,34.2");

            DevKit.InitHeader(Log, LogIndexType.datetime, false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);
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
        public void Log141DataAdapter_AddToStore_Add_DecreasingLog()
        {
            AddParents();

            Log.RunNumber = "101";
            Log.IndexCurve = "MD";
            Log.IndexType = LogIndexType.measureddepth;
            Log.Direction = LogIndexDirection.decreasing;

            DevKit.InitHeader(Log, Log.IndexType.Value, increasing: false);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 100, 0.9, increasing: false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var logAdded = GetLog(Log);

            Assert.IsNotNull(logAdded);
            Assert.AreEqual(LogIndexDirection.decreasing, logAdded.Direction);
            Assert.AreEqual(Log.RunNumber, logAdded.RunNumber);

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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            var logCurves = Log.LogCurveInfo;
            var indexCurve = logCurves.First();
            logCurves.Remove(indexCurve);
            logCurves.Add(indexCurve);
            var firstCurve = Log.LogCurveInfo.First();
            Assert.AreNotEqual(indexCurve.Mnemonic.Value, firstCurve.Mnemonic.Value);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
         
            var logAdded = GetLog(Log);
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
                    "<log uid=\"" + Log.Uid + "\" uidWell=\"" + Log.UidWell + "\" uidWellbore=\"" + Log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + Well.Name + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + Wellbore.Name + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + DevKit.Name("Log 01") + "</name>" + Environment.NewLine +
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

            var response = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);
            Assert.IsNull(result.BhaRunNumber);
            Assert.AreEqual(2, result.LogCurveInfo.Count);
            Assert.IsNull(result.LogCurveInfo[0].ClassIndex);
            Assert.IsNull(result.LogCurveInfo[1].ClassIndex);
        }

        [TestMethod, Description("To test adding a log with special characters")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Characters()
        {
            AddParents();

            // Add log
            var description = @"~ ! @ # $ % ^ &amp; * ( ) _ + { } | &lt; > ? ; : ' "" , . / \ [ ] and \b \f \n \r \t \"" \\ ";
            var expectedDescription = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' "" , . / \ [ ] and \b \f \n \r \t \"" \\";

            var row = @"~ ! @ # $ % ^ &amp; * ( ) _ + { } | &lt; > ? ; : ' "" . / \ [ ] and \b \f \n \r \t \"" \\ ";   // Comma omitted
            var expectedRow = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' "" . / \ [ ] and \b \f \n \r \t \"" \\";

            var xmlIn = FormatXmlIn(Log, $"<description>{description}</description>", $"<data>5000.1, {row}, 5.1</data>");

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(expectedDescription, returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(expectedRow, channelData[1].Trim());
        }

        [TestMethod, Description("To test adding a log with special character: &amp; (encoded ampersand)")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Characters_Encoded_Ampersand()
        {
            AddParents();

            // Add log          
            var description = "<description>Header &amp; </description>";
            var row = "<data>5000.1, Data &amp; , 5.1</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description.Trim());
            Assert.AreEqual("Header &", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual("Data &", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding a log with special characters &lt; (encoded less than)")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Characters_Encoded_Less_Than()
        {
            AddParents();

            // Add log          
            var description = "<description>Header &lt; </description>";
            var row = "<data>5000.1, Data &lt; , 5.1</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual("Header <", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual("Data <", channelData[1].Trim());
        }

        [TestMethod, Description(@"To test adding log data string channel with \ (backslash).")]
    
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Character_Backslash()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \ </description>";
            var row = @"<data>5000.0, Data \ , 5.0</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null); 
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \", channelData[1].Trim());
        }

        [TestMethod, Description("As comma is a delimiter, this test is served as a reminder of the problem and will need to be updated to the decided response of the server.")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Character_Comma()
        {
            AddParents();

            // Add log          
            var description = "<description>Test special character , (comma) </description>";
            var row = @"<data>5000.0, ""A comma, in the value"",30.0</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            returnLog.LogData[0].Data.ForEach(x =>
            {
                DevKit.AssertDataRowWithQuotes(x, ",", 3);
            });
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \f (form feed).")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Character_FormFeed()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \f </description>";
            var row = @"<data>5000.0, Data \f , 5.0</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \f", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \f", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \" (backslash double-quote).")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Character_Backslash_Double_Quote()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \""  </description>";
            var row = @"<data>5000.0, Data \"" , 5.0</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);              
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \""", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \""", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \b (backspace).")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Character_Backspace()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \b  </description>";
            var row = @"<data>5000.0, Data \b , 5.0</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);    
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
            Assert.IsNotNull(returnLog.Description);
            Assert.AreEqual(@"Header \b", returnLog.Description.Trim());
            Assert.AreEqual(1, returnLog.LogData.Count);
            Assert.AreEqual(1, returnLog.LogData[0].Data.Count);

            var channelData = returnLog.LogData[0].Data[0].Split(',');
            Assert.AreEqual(3, channelData.Length);
            Assert.AreEqual(@"Data \b", channelData[1].Trim());
        }

        [TestMethod, Description("To test adding log data string channel with JSON special character \\ (double backslash).")]
        public void Log141DataAdapter_AddToStore_AddLog_With_Special_Character_Double_Backslash()
        {
            AddParents();

            // Add log          
            var description = @"<description>Header \\ </description>";
            var row = @"<data>5000.0, Data \\ , 5.0</data>";

            var xmlIn = FormatXmlIn(Log, description, row);

            var result = DevKit.AddToStore(ObjectTypes.Log, xmlIn, null, null);
            Assert.AreEqual((short)ErrorCodes.Success, result.Result);

            // Query log
            var returnLog = GetLog(Log);
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
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = new Log()
            {
                Uid = DevKit.Uid(),
                UidWell = Wellbore.UidWell,
                NameWell = Well.Name,
                UidWellbore = response.SuppMsgOut,
                NameWellbore = Wellbore.Name,
                Name = DevKit.Name("Log 01"),
                LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() })
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
            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.NodesWithSameIndex, response.Result);

            var query = new Log
            {
                Uid = log.Uid,
                UidWell = log.UidWell,
                UidWellbore = log.UidWellbore
            };

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_With_Null_Indicator()
        {
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(null, DevKit.Name("Log 01"), Wellbore.UidWell, Well.Name, response.SuppMsgOut, Wellbore.Name);
            log.StartIndex = new GenericMeasure(13, "ft");
            log.EndIndex = new GenericMeasure(17, "ft");
            log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,-999.25");
            logData.Data.Add("1800.0,18.1,-999.25");
            logData.Data.Add("1900.0,19.1,-999.25");
            logData.Data.Add("2000.0,20.1,-999.25");
            logData.Data.Add("2100.0,21.1,-999.25");
            logData.Data.Add("2200.0,22.1,-999.25");

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
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
            var response = DevKit.Add<WellList, Well>(Well);

            Wellbore.UidWell = response.SuppMsgOut;
            response = DevKit.Add<WellboreList, Wellbore>(Wellbore);

            var log = DevKit.CreateLog(null, DevKit.Name("Log 01"), Wellbore.UidWell, Well.Name, response.SuppMsgOut, Wellbore.Name);
            log.LogData = DevKit.List(new LogData() { Data = DevKit.List<string>() });

            log.NullValue = "-999.25";

            var logData = log.LogData.First();
            logData.Data.Add("1700.0,17.1,");
            logData.Data.Add("1800.0,18.1,");
            logData.Data.Add("1900.0,19.1,");
            logData.Data.Add("2000.0,20.1,");
            logData.Data.Add("2100.0,21.1,");
            logData.Data.Add("2200.0,22.1,");

            DevKit.InitHeader(log, LogIndexType.measureddepth);

            response = DevKit.Add<LogList, Log>(log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var uidLog = response.SuppMsgOut;

            // Query
            var query = DevKit.CreateLog(uidLog, null, log.UidWell, null, log.UidWellbore, null);

            var results = DevKit.Query<LogList, Log>(query, optionsIn: OptionsIn.ReturnElements.All);
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

            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            Log.StartIndex = new GenericMeasure {Uom = "m", Value = 1.0};
            Log.EndIndex = new GenericMeasure { Uom = "m", Value = 10.0 };

            foreach (var curve in Log.LogCurveInfo)
            {
                curve.MinIndex = Log.StartIndex;
                curve.MaxIndex = Log.EndIndex;
            }

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);

            Assert.IsNull(result.StartIndex);
            Assert.IsNull(result.EndIndex);

            Assert.AreEqual(Log.LogCurveInfo.Count, result.LogCurveInfo.Count);
            foreach (var curve in result.LogCurveInfo)
            {
                Assert.IsNull(curve.MinIndex);
                Assert.IsNull(curve.MaxIndex);
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_NoLogCurveInfos()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            Log.LogCurveInfo.Clear();
            Log.LogData.Clear();

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_Custom_Data_Delimiter()
        {
            var delimiter = "|";

            AddParents();

            // Set data delimiter to other charactrer than ","
            Log.DataDelimiter = delimiter;

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10, hasEmptyChannel:false);

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);

            // Assert data delimiter
            Assert.AreEqual(delimiter, result.DataDelimiter);

            var data = result.LogData.FirstOrDefault()?.Data;
            Assert.IsNotNull(data);

            var channelCount = Log.LogCurveInfo.Count;

            // Assert data delimiter in log data
            foreach (var row in data)
            {
                var points = ChannelDataReader.Split(row, delimiter);
                Assert.AreEqual(channelCount, points.Length);
            }
        }
       
        [TestMethod]
        public void Log141DataAdapter_AddToStore_Add_With_Blank_Unit_InLogCurveInfo_And_UnitList()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            // Set the 3rd LogCurveInfo/unit to null and 3rd UnitList entry to an empty string
            Log.LogCurveInfo[2].Unit = null;
            var logData = Log.LogData.First();
            logData.UnitList = "m,m/h,";

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_With_Non_Double_Data_Types()
        {
            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Add Log Curves with double, datetime, long and string data types
            Log.LogCurveInfo.Clear();
            Log.LogCurveInfo.AddRange(new List<LogCurveInfo>()
            {
                DevKit.LogGenerator.CreateLogCurveInfo("MD", "m", LogDataType.@double),
                DevKit.LogGenerator.CreateLogCurveInfo("ROP", "m/h", LogDataType.@double),
                DevKit.LogGenerator.CreateLogCurveInfo("TS", "s", LogDataType.datetime),
                DevKit.LogGenerator.CreateLogCurveInfo("CNT", "m", LogDataType.@long),
                DevKit.LogGenerator.CreateLogCurveInfo("MSG", "", LogDataType.@string)
            });

            // Generated the data
            var numRows = 5;
            DevKit.LogGenerator.GenerateLogData(Log, numRows);
            var mnemonics = string.Join(",", Log.LogCurveInfo.Select(l => l.Mnemonic.Value));
            var units = string.Join(",", Log.LogCurveInfo.Select(l => l.Unit));

            Log.LogData[0].MnemonicList = mnemonics;
            Log.LogData[0].UnitList = units;

            var response = DevKit.Add<LogList, Log>(Log);
            Assert.AreEqual((short)ErrorCodes.Success, response.Result);

            var result = GetLog(Log);

            // Assert that Data Types match before and after
            for (var i = 0; i < Log.LogCurveInfo.Count; i++)
            {
                Assert.IsNotNull(Log.LogCurveInfo[i].TypeLogData);
                Assert.IsNotNull(result.LogCurveInfo[i].TypeLogData);
                Assert.AreEqual(Log.LogCurveInfo[i].TypeLogData.Value, result.LogCurveInfo[i].TypeLogData.Value);
            }

            // Assert data matches before and after
            var logData = Log.LogData[0].Data;
            var logDataAdded = result.LogData[0].Data;
            Assert.AreEqual(logData.Count, logDataAdded.Count);

            // For each row of data
            for (int i = 0; i < logData.Count; i++)
            {
                var data = logData[i].Split(',');
                var dataAdded = logDataAdded[i].Split(',');
                Assert.AreEqual(data.Length, dataAdded.Length);

                // Check that the string data matches
                for (int j = 0; j < data.Length; j++)
                {
                    if (Log.LogCurveInfo[j].TypeLogData.Value == LogDataType.@string)
                    {
                        Assert.AreEqual(data[j], dataAdded[j]);
                    }
                }
            }
        }

        [TestMethod]
        public void Log141DataAdapter_AddToStore_Invalid_Data_Rows()
        {
            CompatibilitySettings.InvalidDataRowSetting = InvalidDataRowSetting.Error;

            AddParents();

            // Initialize Log Header
            DevKit.InitHeader(Log, LogIndexType.measureddepth);

            // Initialize data with only 1 data point in the
            DevKit.InitData(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 5);

            DevKit.AddAndAssert(Log, ErrorCodes.ErrorRowDataCount);
        }

        [TestMethod, Description("As Unit is not required (can be blank) on unit list when unit is not specfied on logCurveInfo")]
        public void Log141DataAdapter_AddToStore_With_Empty_Unit_On_Curve()
        {
            AddParents();

            DevKit.InitHeader(Log, LogIndexType.measureddepth);
            DevKit.InitDataMany(Log, DevKit.Mnemonics(Log), DevKit.Units(Log), 10);

            //Remove the unit of the last logCurveInfo
            var lci = Log.LogCurveInfo.LastOrDefault();
            Assert.IsNotNull(lci);

            lci.Unit = string.Empty;

            // Set the 3rd UnitList entry to an empty string
            var logData = Log.LogData.First();
            logData.UnitList = "m,m/h,";

            DevKit.AddAndAssert(Log);

            var result = DevKit.GetAndAssert(Log, optionsIn: OptionsIn.ReturnElements.HeaderOnly);
            Assert.IsNotNull(result);

            // Verify the logCurveInfo unit
            var resultLci = result.LogCurveInfo.LastOrDefault();
            Assert.IsNotNull(resultLci);
            Assert.IsNull(resultLci.Unit);

            // Verify the log data unitList matches
            result = DevKit.GetAndAssert(Log, optionsIn: OptionsIn.ReturnElements.DataOnly);
            Assert.IsNotNull(result.LogData[0]);
            Assert.AreEqual(1, result.LogData.Count);

            var resultLogData = result.LogData[0];
            Assert.AreEqual("m,", resultLogData.UnitList, "Unit list of the result doesn't match");
        }

        #region Helper Methods

        private Log GetLog(Log log)
        {
            return DevKit.GetAndAssert(log);
        }

        private string FormatXmlIn(Log log, string description, string row)
        {
            var xmlIn = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:dc=\"http://purl.org/dc/terms/\" " +
                "xmlns:gml=\"http://www.opengis.net/gml/3.2\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log uid=\"" + log.Uid + "\" uidWell=\"" + log.UidWell + "\" uidWellbore=\"" + log.UidWellbore + "\">" + Environment.NewLine +
                        "<nameWell>" + log.NameWell + "</nameWell>" + Environment.NewLine +
                        "<nameWellbore>" + log.NameWellbore + "</nameWellbore>" + Environment.NewLine +
                        "<name>" + DevKit.Name("Test special characters") + "</name>" + Environment.NewLine +
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
