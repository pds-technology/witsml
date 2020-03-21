//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio.Data.Channels
{
    [TestClass]
    public class ChannelDataReaderTests
    {
        private const string TimeLogData = @"[
            [[""2016-03-01T00:00:00.0-06:00""], [0.0, 0.0, 0.0]],
            [[""2016-03-01T00:00:01.0-06:00""], [1.0, 1.1, 1.2]],
            [[""2016-03-01T00:00:02.0-06:00""], [2.0, 2.1, 2.2]],
            [[""2016-03-01T00:00:03.0-06:00""], [3.0, 3.1, 3.2]],
            [[""2016-03-01T00:00:04.0-06:00""], [4.0, 4.1, 4.2]],
        ]";

        private const string DepthLogData1 = @"[
            [[0.0], [0.0, 0.1, 0.2]],
            [[0.1], [1.0, 1.1, 1.2]],
            [[0.2], [2.0, 2.1, 2.2]],
            [[0.3], [3.0, 3.1, 3.2]],
            [[0.4], [4.0, 4.1, 4.2]],
        ]";

        private const string DepthLogData2 = @"[
            [[0.5], [5.0, 5.1, 5.2]],
            [[0.6], [6.0, 6.1, 6.2]],
            [[0.7], [7.0, 7.1, 7.2]],
            [[0.8], [8.0, 8.1, 8.2]],
            [[0.9], [9.0, 9.1, 9.2]],
        ]";

        private const string ChannelSetData = @"[
            [[0.0, ""2016-03-01T00:00:00.0-06:00""], [[0.0, true], 0.1, 0.2]],
            [[0.1, ""2016-03-01T00:00:01.0-06:00""], [[1.0, true], 1.1, 1.2]],
            [[0.2, ""2016-03-01T00:00:02.0-06:00""], [[2.0, false], 2.1, 2.2]],
            [[0.3, ""2016-03-01T00:00:03.0-06:00""], [null,        null, 3.2]],
            [[0.4, ""2016-03-01T00:00:04.0-06:00""], [[4.0, true], 4.1, 4.2]],
        ]";

        private const string UpdateLogData1 = @"[
            [[0.0], [0.0, 0.1, 0.2]],
            [[1.0], [1.0, 1.1, 1.2]],
            [[2.0], [2.0, 2.1, 2.2]],
            [[3.0], [3.0, 3.1, 3.2]],
            [[4.0], [4.0, 4.1, 4.2]],
            [[5.0], [5.0, 5.1, 5.2]],
            [[5.5], [5.05, 5.15, 5.25]],
            [[6.0], [6.0, 6.1, 6.2]],
            [[7.0], [7.0, 7.1, 7.2]]
        ]";

        private const string UpdateLogData2 = @"[
            [[3.0], [null, null, 3.22]],
            [[3.5], [3.005, 3.115, 3.225]],
            [[5.0], [null, null, 5.22]],
            [[6.0], [null, null, 6.22]],
        ]";

        private const string UpdateLogData3 = @"[
            [[3.0], [null, null, 3.22]],
            [[3.5], [3.005, 3.115, 3.225]],
            [[5.0], [null, null, 5.22]],
            [[6.0], [null, 6.11, 6.22]],
            [[7.0], [null, 7.11, 7.22]],
            [[7.5], [null, null, 7.225]],
        ]";

        private const string HasEmptyChannels = @"[
               [[10],[null,11,12,null,null,13,null,null,null]],
               [[20],[null,21,22,null,null,23,null,null,null]],
               [[30],[null,31,32,null,null,33,null,null,null]],
               [[40],[null,41,42,null,null,43,null,null,null]],
               [[50],[null,51,52,null,null,53,null,null,null]],
               [[60],[null,61,62,null,null,63,null,null,null]],
               [[70],[null,71,72,null,null,73,null,null,null]],
        ]";

        [TestMethod]
        public void ChannelDataReader_Can_Parse_Null_Data()
        {
            var reader = new ChannelDataReader(string.Empty);

            Assert.AreEqual(0, reader.Depth);
            Assert.AreEqual(0, reader.FieldCount);
            Assert.AreEqual(0, reader.RecordsAffected);
        }

        [TestMethod]
        public void ChannelDataReader_Can_Set_Data_Value()
        {
            var reader = new ChannelDataReader(UpdateLogData1, new[] { "MD", "ROP", "GR", "HKLD" })
                .WithIndex("MD", "m", true, false);

            Assert.AreEqual(1, reader.Depth);
            Assert.AreEqual(4, reader.FieldCount);
            Assert.AreEqual(9, reader.RecordsAffected);

            if (reader.Read())
            {
                reader.SetValue(1, 1000.0);
                Assert.AreEqual(1000.0, reader.GetDouble(1));
            }
        }

        [TestMethod]
        public void ChannelDataReader_Can_Read_Time_Log_Data()
        {
            var reader = new ChannelDataReader(TimeLogData);
            int count = 0;

            Assert.AreEqual(1, reader.Depth);
            Assert.AreEqual(4, reader.FieldCount);
            Assert.AreEqual(5, reader.RecordsAffected);

            while (reader.Read())
            {
                Console.WriteLine("Row {0}: {1}, {2}, {3}, {4}", count++,
                    reader.GetDateTimeOffset(0),
                    reader.GetDouble(1),
                    reader.GetDouble(2),
                    reader.GetDouble(3));
            }
        }

        [TestMethod]
        public void ChannelDataReader_Can_Read_Depth_Log_Data()
        {
            var reader = new ChannelDataReader(DepthLogData1, new[] { "MD", "ROP", "GR", "HKLD" });
            int count = 0;

            Assert.AreEqual(1, reader.Depth);
            Assert.AreEqual(4, reader.FieldCount);
            Assert.AreEqual(5, reader.RecordsAffected);

            while (reader.Read())
            {
                Console.WriteLine("Row {0}: {1}, {2}, {3}, {4}", count++,
                    reader.GetDouble(0),
                    reader.GetDouble(1),
                    reader.GetDouble(2),
                    reader.GetDouble(reader.GetOrdinal("HKLD")));
            }
        }

        [TestMethod]
        public void ChannelDataReader_Can_calculate_Channel_Min_Max_Indices_With_Single_Value()
        {
            var reader = new ChannelDataReader(UpdateLogData2, new[] { "ROP", "GR", "HKLD" })
                .WithIndex("MD", "m", true, false);

            Assert.AreEqual(1, reader.Depth);
            Assert.AreEqual(4, reader.FieldCount);
            Assert.AreEqual(4, reader.RecordsAffected);

            var range = reader.GetChannelIndexRange(reader.GetOrdinal("GR"));

            Assert.AreEqual(3.5, range.Start);
            Assert.AreEqual(3.5, range.End);
        }

        [TestMethod]
        public void ChannelDataReader_Can_Calculate_Channel_Min_Max_Indices_With_Multiple_Values()
        {
            var reader = new ChannelDataReader(UpdateLogData3, new[] { "ROP", "GR", "HKLD" })
                .WithIndex("MD", "m", true, false);

            Assert.AreEqual(1, reader.Depth);
            Assert.AreEqual(4, reader.FieldCount);
            Assert.AreEqual(6, reader.RecordsAffected);

            var range = reader.GetChannelIndexRange(reader.GetOrdinal("GR"));

            Assert.AreEqual(3.5, range.Start);
            Assert.AreEqual(7.0, range.End);
        }

        [TestMethod]
        public void ChannelDataReader_can_Read_ChannelSet_Data()
        {
            var reader = new ChannelDataReader(ChannelSetData);
            var json = new StringBuilder("[");
            int count = 0;

            Assert.AreEqual(2, reader.Depth);
            Assert.AreEqual(5, reader.FieldCount);
            Assert.AreEqual(5, reader.RecordsAffected);
            json.AppendLine();

            while (reader.Read())
            {
                Console.WriteLine("Row {0}: {1}, {2}, {3}, {4}, {5}", count++,
                    reader.GetDouble(0),
                    reader.GetDateTimeOffset(1),
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetDouble(4));

                json.AppendLine(reader.GetJson());
            }

            Assert.IsNull(reader.GetJson());

            // original
            Console.WriteLine();
            Console.WriteLine(ChannelSetData);

            // serialized
            Console.WriteLine();
            Console.WriteLine(json.Append("]"));
        }

        [TestMethod]
        public void ChannelDataReader_Can_Slice()
        {
            var data = 
                "[" +
                "[[1],[1,1,1,1,1]]," +
                "[[2],[2,2,2,2,2]]," +
                "[[3],[3,3,3,3,3]]," +
                "[[4],[4,4,4,4,4]]," +
                "[[5],[5,5,5,5,5]]," +
                "[[6],[6,6,6,6,6]]," +
                "[[7],[7,7,7,7,7]]" +
                "]";

            // Create a Reader
            var reader = new ChannelDataReader(data, "CH1,CH2,CH3,CH4,CH5".Split(','), "ft1,ft2,ft3,ft4,ft5".Split(','), "double,double,double,double,double".Split(','), ",,,,".Split(','), "eml://witsml14/well(Energistics-well-0001)/wellbore(Energistics-w1-wellbore-0001)/log(Energistics-w1-wb1-log-0002)", "06e4dff8-3de4-4057-a21b-92026e89a6d4")
                .WithIndex("MD", "ft", true, false);

            Assert.IsTrue(reader.Read());

            // Slice the Reader
            //var slices = new string[] { "MD", "CH2", "CH5" };

            Dictionary<int, string> requestedMnemonics = new Dictionary<int, string>() { { 0, "MD" }, { 2, "CH2" }, { 5, "CH5" } };
            Dictionary<int, string> requestedUnits = new Dictionary<int, string>() { { 0, "ft" }, { 2, "ft2" }, { 5, "ft5" } };
            Dictionary<int, string> requestedDataTypes = new Dictionary<int, string>() { { 0, "double" }, { 2, "double" }, { 5, "double" } };
            Dictionary<int, string> requestedNullValues = new Dictionary<int, string>() { { 0, string.Empty }, { 2, string.Empty }, { 5, string.Empty } };

            reader.Slice(requestedMnemonics, requestedUnits, requestedDataTypes, requestedNullValues);

            // Test Mnemonic Slices
            var mnemonics = reader.AllMnemonics;
            var requestedMnemonicValues = requestedMnemonics.Values.ToArray();
            for (var i = 0; i < mnemonics.Count; i++)
            {
                Assert.AreEqual(requestedMnemonicValues[i], mnemonics[i]);
            }

            // Test Unit Slices
            var units = reader.AllUnits;
            Assert.AreEqual(requestedMnemonics.Keys.Count, units.Count);
            Assert.AreEqual(units[0], "ft");
            Assert.AreEqual(units[1], "ft2");
            Assert.AreEqual(units[2], "ft5");

            var values = new object[6];
            var valueCount = reader.GetValues(values);

            Assert.AreEqual(requestedMnemonics.Keys.Count, valueCount);
        }

        [TestMethod]
        public void ChannelDataReader_Can_Slice_With_Empty_Channels()
        {           
            // Create a Reader
            var reader = new ChannelDataReader(HasEmptyChannels, "CH1,CH2,CH3,CH4,CH5,CH6,CH7,CH8,CH9".Split(','), "ft1,ft2,ft3,ft4,ft5,ft6,ft7,ft8,ft9".Split(','), "double,double,double,double,double,double,double,double, double".Split(','), ",,,,,,,,".Split(','), "eml://witsml14/well(Energistics-well-0001)/wellbore(Energistics-w1-wellbore-0001)/log(Energistics-w1-wb1-log-0002)", "06e4dff8-3de4-4057-a21b-92026e89a6d4")
                .WithIndex("MD", "ft", true, false);

            Assert.IsTrue(reader.Read());

            Dictionary<int, string> requestedMnemonics = new Dictionary<int, string>() { { 0, "MD" }, { 2, "CH2" }, { 6, "CH6" } };
            Dictionary<int, string> requestedUnits = new Dictionary<int, string>() { { 0, "ft" }, { 2, "ft2" }, { 6, "ft6" } };
            Dictionary<int, string> requestedDataTypes = new Dictionary<int, string>() { { 0, "double" }, { 2, "double" }, { 6, "double" } };
            Dictionary<int, string> requestedNullValues = new Dictionary<int, string>() { { 0, string.Empty }, { 2, string.Empty }, { 6, string.Empty } };

            reader.Slice(requestedMnemonics, requestedUnits, requestedDataTypes, requestedNullValues);

            // Test Mnemonic Slices
            var mnemonics = reader.AllMnemonics;
            var requestedMnemonicValues = requestedMnemonics.Values.ToArray();
            Assert.AreEqual(3, mnemonics.Count());
            Assert.AreEqual(mnemonics.Count(), requestedMnemonicValues.Count());
            for (var i = 0; i < mnemonics.Count; i++)
            {
                Assert.AreEqual(requestedMnemonicValues[i], mnemonics[i]);
            }

            // Test Unit Slices
            var units = reader.AllUnits;
            Assert.AreEqual(3, units.Count());
            Assert.AreEqual(requestedMnemonics.Keys.Count, units.Count);
            Assert.AreEqual("ft", units[0]);
            Assert.AreEqual("ft2", units[1]);
            Assert.AreEqual("ft6", units[2]);

            var values = new object[9];
            var valueCount = reader.GetValues(values);
            Assert.AreEqual(3, valueCount);
            Assert.AreEqual((long)10, values[0]);
            Assert.AreEqual((long)11, values[1]);
            Assert.AreEqual((long)13, values[2]);          
        }

        [TestMethod]
        public void ChannelDataReader_Can_Slice_With_Request_Has_Empty_Channels()
        {
            // Create a Reader
            var reader = new ChannelDataReader(HasEmptyChannels, "CH1,CH2,CH3,CH4,CH5,CH6,CH7,CH8,CH9".Split(','), "ft1,ft2,ft3,ft4,ft5,ft6,ft7,ft8,ft9".Split(','), "double,double,double,double,double,double,double,double, double".Split(','),",,,,,,,,".Split(','), "eml://witsml14/well(Energistics-well-0001)/wellbore(Energistics-w1-wellbore-0001)/log(Energistics-w1-wb1-log-0002)", "06e4dff8-3de4-4057-a21b-92026e89a6d4")
                .WithIndex("MD", "ft", true, false);

            Assert.IsTrue(reader.Read());

            Dictionary<int, string> requestedMnemonics = new Dictionary<int, string>() { { 0, "MD" }, { 1, "CH1" }, { 2, "CH2" }, { 5, "CH5" }, { 6, "CH6" }, { 7, "CH7" }, { 9, "CH9" } };
            Dictionary<int, string> requestedUnits = new Dictionary<int, string>() { { 0, "ft" }, { 1, "ft1" }, { 2, "ft2" }, { 5, "ft5" }, { 6, "ft6" }, { 7, "ft7" }, { 9, "ft9" } };
            Dictionary<int, string> requestedDataTypes = new Dictionary<int, string>() { { 0, "double" }, { 1, "double" }, { 2, "double" }, { 5, "double" }, { 6, "double" }, { 7, "double" }, { 9, "double" } };
            Dictionary<int, string> requestedNullValues = new Dictionary<int, string>() { { 0, string.Empty }, { 1, string.Empty }, { 2, string.Empty }, { 5, string.Empty }, { 6, string.Empty }, { 7, string.Empty }, { 9, string.Empty } };

            reader.Slice(requestedMnemonics, requestedUnits, requestedDataTypes, requestedNullValues);

            // Test Mnemonic Slices
            var mnemonics = reader.AllMnemonics;
            var requestedMnemonicValues = requestedMnemonics.Values.ToArray();
            Assert.AreEqual(3, mnemonics.Count());
            Assert.AreEqual(mnemonics.Count(), requestedMnemonicValues.Count());
            for (var i = 0; i < mnemonics.Count; i++)
            {
                Assert.AreEqual(requestedMnemonicValues[i], mnemonics[i]);
            }

            // Test Unit Slices
            var units = reader.AllUnits;
            Assert.AreEqual(3, units.Count());
            Assert.AreEqual(requestedMnemonics.Keys.Count, units.Count);
            Assert.AreEqual("ft", units[0]);
            Assert.AreEqual("ft2", units[1]);
            Assert.AreEqual("ft6", units[2]);

            var values = new object[9];
            var valueCount = reader.GetValues(values);
            Assert.AreEqual(3, valueCount);
            Assert.AreEqual((long)10, values[0]);
            Assert.AreEqual((long)11, values[1]);
            Assert.AreEqual((long)13, values[2]);
        }

        //[TestMethod]
        //public void ChannelDataReader_can_read_Log_131()
        //{
        //    var devKit = new DevKit131Aspect();
        //    var log = new Witsml131.Log();
        //    var rows = 10;
        //    var cols = 3;

        //    devKit.InitHeader(log, Witsml131.ReferenceData.LogIndexType.measureddepth);
        //    devKit.InitDataMany(log, devKit.Mnemonics(log), devKit.Units(log), rows);

        //    var reader = log.GetReader();
        //    int count = 0;

        //    Assert.AreEqual(1, reader.Depth);
        //    Assert.AreEqual(cols, reader.FieldCount);
        //    Assert.AreEqual(rows, reader.RecordsAffected);

        //    while (reader.Read())
        //    {
        //        Console.WriteLine("Row {0}: {1}, {2}, {3}", count++,
        //            reader.GetDouble(0),
        //            reader.GetDouble(1),
        //            reader.GetDouble(2));
        //    }
        //}

        //[TestMethod]
        //public void ChannelDataReader_can_read_Log_141()
        //{
        //    var devKit = new DevKit141Aspect();
        //    var log = new Witsml141.Log();
        //    var rows = 10;
        //    var cols = 3;

        //    devKit.InitHeader(log, Witsml141.ReferenceData.LogIndexType.measureddepth);
        //    devKit.InitDataMany(log, devKit.Mnemonics(log), devKit.Units(log), rows, 0.5);

        //    var reader = log.GetReaders().Single();
        //    int count = 0;

        //    Assert.AreEqual(1, reader.Depth);
        //    Assert.AreEqual(cols, reader.FieldCount);
        //    Assert.AreEqual(rows, reader.RecordsAffected);

        //    while (reader.Read())
        //    {
        //        Console.WriteLine("Row {0}: {1}, {2}, {3}", count++,
        //            reader.GetDouble(0),
        //            reader.GetDouble(1),
        //            reader.GetDouble(2));
        //    }
        //}

        //[TestMethod]
        //public void ChannelDataReader_can_read_Log_200()
        //{
        //    var devKit = new DevKit200Aspect();
        //    var log = new Witsml200.Log();
        //    var rows = 4;
        //    var cols = 4;

        //    var channelIndex = new Witsml200.ComponentSchemas.ChannelIndex
        //    {
        //        IndexType = Witsml200.ReferenceData.ChannelIndexType.datetime,
        //        Direction = Witsml200.ReferenceData.IndexDirection.increasing,
        //        Mnemonic = "MD",
        //        Uom = "m"
        //    };

        //    devKit.InitHeader(log, Witsml200.ReferenceData.LoggingMethod.Mixed, channelIndex);

        //    var reader = log.GetReaders().Single();
        //    int count = 0;

        //    Assert.AreEqual(1, reader.Depth);
        //    Assert.AreEqual(cols, reader.FieldCount);
        //    Assert.AreEqual(rows, reader.RecordsAffected);
        //    Console.WriteLine(log.ChannelSet[0].Data.Data);

        //    while (reader.Read())
        //    {
        //        Console.WriteLine("Row {0}: {1}, {2}, {3}, {4}", count++,
        //            reader.GetDateTimeOffset(0),
        //            reader.GetString(1),
        //            reader.GetDouble(2),
        //            reader.GetDouble(3));
        //    }
        //}
    }
}
