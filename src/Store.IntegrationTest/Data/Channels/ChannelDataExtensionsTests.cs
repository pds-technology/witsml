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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Data.Logs;
using PDS.WITSMLstudio.Store.Configuration;
using Shouldly;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio.Store.Data.Channels
{
    [TestClass]
    public class ChannelDataExtensionsTests
    {
        private Log200Generator _log20Generator;
        public TestContext TestContext { get; set; }
        public DevKit200Aspect DevKit { get; set; }

        [TestInitialize]
        public void TestSetUp()
        {
            _log20Generator = new Log200Generator();
            DevKit = new DevKit200Aspect(TestContext);
        }

        [TestMethod]
        public void ChannelDataExtensions_GetDataDelimiterOrDefault_Returns_Delimter_Correctly()
        {
            var result = ChannelDataExtensions.GetDataDelimiterOrDefault(string.Empty);
            Assert.IsNotNull(result);
            Assert.AreEqual(",", ",");

            result = ChannelDataExtensions.GetDataDelimiterOrDefault("|");
            Assert.IsNotNull(result);
            Assert.AreEqual("|", "|");
        }

        [TestMethod]
        public void ChannelDataExtensions_GetReader_Returns_ChannelDataReader()
        {
            string[] mnemonics, mnemonicFilter;
            SortedDictionary<int, string> mnemonicFilterDictionary;
            InitMnemonics(out mnemonics, out mnemonicFilter, out mnemonicFilterDictionary);

            string[] units;
            SortedDictionary<int, string> unitDictionary;
            InitUnits(out units, out unitDictionary);

            string[] dataTypes;
            SortedDictionary<int, string> dataTypeDictionary;
            InitDataTypes(out dataTypes, out dataTypeDictionary);

            string[] nullValues;
            SortedDictionary<int, string> nullValueDictionary;
            InitNullValues(out nullValues, out nullValueDictionary);

            var ranges = new Dictionary<string, Range<double?>>();

            var listofCii = new List<ChannelIndexInfo>
            {
                new ChannelIndexInfo()
                {
                    DataType = "double",
                    Mnemonic = "MD",
                    Unit = "m",
                    Increasing = true,
                    IsTimeIndex = false
                }
            };
            var records = new ChannelDataReader("[[[0],[0,0,0]],[[1],[1,1,1]]]", mnemonics, units, dataTypes, nullValues, "eml://witsml14/well(1)/wellbore(1)/log(1)", "1")
                .WithIndices(listofCii).AsEnumerable();

            Assert.IsNotNull(records);
            using (var reader = records.GetReader())
            {
                var logData = reader.GetData(new ResponseContext() { HasAllRequestedValues = false, RequestLatestValues = null }, mnemonicFilterDictionary, unitDictionary, dataTypeDictionary, nullValueDictionary, out ranges);
                Assert.AreEqual(1, logData.Count);
                Assert.AreEqual(2, logData[0].Count);
                Assert.AreEqual(1, logData[0][0].Count);
            }
        }

        [TestMethod]
        public void ChannelDataExtensions_IsIncreasing_Returns_If_ChannelSet_Is_Increasing_Correctly()
        {
            var log = new Witsml200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };
            var channelSet = _log20Generator.CreateChannelSet(log);

            channelSet = null;

            var isIncreasing = channelSet.IsIncreasing();
            Assert.IsTrue(isIncreasing);

            // Create channel set
            channelSet = _log20Generator.CreateChannelSet(log);

            channelSet.Index.Add(_log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.decreasing));
            isIncreasing = channelSet.IsIncreasing();
            Assert.IsFalse(isIncreasing);

            channelSet.Index.Clear();
            channelSet.Index.Add(_log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing));
            isIncreasing = channelSet.IsIncreasing();
            Assert.IsTrue(isIncreasing);
        }

        [TestMethod()]
        public void ChannelDataExtensions_IsIncreasing_Returns_If_ChannelIndex_Is_Increasing_Correctly()
        {
            var index = _log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.decreasing);
            var isIncreasing = index.IsIncreasing();
            Assert.IsFalse(isIncreasing);

            index = _log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing);
            isIncreasing = index.IsIncreasing();
            Assert.IsTrue(isIncreasing);
        }

        [TestMethod]
        public void ChannelDataExtensions_IsTimeIndex_Returns_If_ChannelSet_Is_A_Time_Index()
        {
            var log = new Witsml200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };
            var channelSet = _log20Generator.CreateChannelSet(log);

            channelSet = null;

            var isTimeIndex = channelSet.IsTimeIndex();
            Assert.IsFalse(isTimeIndex);

            channelSet = _log20Generator.CreateChannelSet(log);

            channelSet.Index.Add(_log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.decreasing));
            isTimeIndex = channelSet.IsTimeIndex();
            Assert.IsFalse(isTimeIndex);

            channelSet.Index.Clear();
            channelSet.Index.Add(_log20Generator.CreateElapsedTimeIndex(Witsml200.ReferenceData.IndexDirection.increasing));
            isTimeIndex = channelSet.IsTimeIndex();
            Assert.IsFalse(isTimeIndex);

            channelSet.Index.Clear();
            channelSet.Index.Add(_log20Generator.CreateElapsedTimeIndex(Witsml200.ReferenceData.IndexDirection.increasing));
            isTimeIndex = channelSet.IsTimeIndex(true);
            Assert.IsTrue(isTimeIndex);

            channelSet.Index.Clear();
            channelSet.Index.Add(_log20Generator.CreateDateTimeIndex());
            isTimeIndex = channelSet.IsIncreasing();
            Assert.IsTrue(isTimeIndex);
        }

        [TestMethod]
        public void ChannelDataExtensions_IsTimeIndex_Returns_If_ChannelIndex_Is_A_Time_Index()
        {
            var index = _log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.decreasing);
            var isTimeIndex = index.IsTimeIndex();
            Assert.IsFalse(isTimeIndex);

            index = _log20Generator.CreateElapsedTimeIndex(Witsml200.ReferenceData.IndexDirection.decreasing);
            isTimeIndex = index.IsTimeIndex();
            Assert.IsFalse(isTimeIndex);

            index = _log20Generator.CreateElapsedTimeIndex(Witsml200.ReferenceData.IndexDirection.decreasing);
            isTimeIndex = index.IsTimeIndex(true);
            Assert.IsTrue(isTimeIndex);

            index = _log20Generator.CreateDateTimeIndex();
            isTimeIndex = index.IsTimeIndex();
            Assert.IsTrue(isTimeIndex);
        }

        [TestMethod]
        public void ChannelDataExtensions_GetReaders_Returns_ChannelDataReader_For_20_Log()
        {
            var log = new Witsml200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };

            log = null;

            var readers = log.GetReaders();

            Assert.AreEqual(0, readers.Count());

            log = new Witsml200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };

            readers = log.GetReaders();

            Assert.AreEqual(0, readers.Count());

            Witsml200.ComponentSchemas.ChannelIndex mdChannelIndex = _log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing);
            DevKit.InitHeader(log, Witsml200.ReferenceData.LoggingMethod.MWD, mdChannelIndex);

            Assert.AreEqual(1, log.ChannelSet.Count);

            // Correct mnemonic names
            log.ChannelSet[0].Channel[0].Mnemonic = "A";
            log.ChannelSet[0].Channel[1].Mnemonic = "B";
            log.ChannelSet[0].Channel[2].Mnemonic = "C";

            // Null out the datetype of channel B
            log.ChannelSet[0].Channel[1].DataType = null;

            log.ChannelSet.Add(_log20Generator.CreateChannelSet(log));

            Assert.AreEqual(2, log.ChannelSet.Count);

            readers = log.GetReaders();

            var listOfReaders = readers.ToList();

            Assert.AreEqual(1, listOfReaders.Count);

            AssertReaderAndData(listOfReaders);

            log.ChannelSet[0].Data = null;

            readers = log.GetReaders();

            Assert.AreEqual(0, readers.Count());
        }


        [TestMethod]
        public void ChannelDataExtensions_AddChannel_Adds_Channel_To_Datablock()
        {
            var log = new Witsml200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };

            Witsml200.ComponentSchemas.ChannelIndex mdChannelIndex = _log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing);
            DevKit.InitHeader(log, Witsml200.ReferenceData.LoggingMethod.MWD, mdChannelIndex);

            Assert.AreEqual(1, log.ChannelSet.Count);

            log.ChannelSet[0].Channel[1].DataType = null;

            var dataBlock = new ChannelDataBlock("eml://witsml20/well(1)/wellbore(1)/log(1)");

            var channelId = 1;
            foreach (var channel in log.ChannelSet[0].Channel)
            {
                dataBlock.AddChannel(channelId++, channel);
            }

            Assert.AreEqual(3, dataBlock.ChannelIds.Count);
            Assert.AreEqual(3, dataBlock.Mnemonics.Count);
            Assert.AreEqual(3, dataBlock.Units.Count);
            Assert.AreEqual(3, dataBlock.DataTypes.Count);
            Assert.AreEqual(3, dataBlock.NullValues.Count);

            log.ChannelSet[0].Channel.ForEach((x, i) => Assert.AreEqual(x.Mnemonic, dataBlock.Mnemonics[i]));
            log.ChannelSet[0].Channel.ForEach((x, i) => Assert.AreEqual(x.Uom.ToString(), dataBlock.Units[i]));
            log.ChannelSet[0].Channel.ForEach((x, i) => Assert.AreEqual((x.DataType == null ? null : x.DataType.ToString()), dataBlock.DataTypes[i]));
        }

        [TestMethod]
        public void ChannelDataExtensions_AddIndex_Adds_Index_To_Datablock()
        {
            var log = new Witsml200.Log
            {
                Uuid = "uid",
                Citation = new Witsml200.ComponentSchemas.Citation(),
                Wellbore = new Witsml200.ComponentSchemas.DataObjectReference(),
                SchemaVersion = "2.0"
            };

            Witsml200.ComponentSchemas.ChannelIndex mdChannelIndex = _log20Generator.CreateMeasuredDepthIndex(Witsml200.ReferenceData.IndexDirection.increasing);
            DevKit.InitHeader(log, Witsml200.ReferenceData.LoggingMethod.MWD, mdChannelIndex);

            Assert.AreEqual(1, log.ChannelSet.Count);

            log.ChannelSet[0].Channel[1].DataType = null;

            var dataBlock = new ChannelDataBlock("eml://witsml20/well(1)/wellbore(1)/log(1)");

            foreach (var channelIndex in log.ChannelSet[0].Index)
            {
                dataBlock.AddIndex(channelIndex);
            }

            Assert.AreEqual(1, dataBlock.Indices.Count);
            Assert.AreEqual(mdChannelIndex.Mnemonic, dataBlock.Indices[0].Mnemonic);
            Assert.AreEqual(mdChannelIndex.Uom.ToString(), dataBlock.Indices[0].Unit);
        }

        #region Helper Methods

        private static void AssertReaderAndData(List<ChannelDataReader> listOfReaders)
        {
            string[] mnemonics, mnemonicFilter;
            SortedDictionary<int, string> mnemonicFilterDictionary;
            InitMnemonics(out mnemonics, out mnemonicFilter, out mnemonicFilterDictionary);

            string[] units;
            SortedDictionary<int, string> unitDictionary;
            InitUnits(out units, out unitDictionary);

            string[] dataTypes;
            SortedDictionary<int, string> dataTypeDictionary;
            InitDataTypes(out dataTypes, out dataTypeDictionary);

            string[] nullValues;
            SortedDictionary<int, string> nullValueDictionary;
            InitNullValues(out nullValues, out nullValueDictionary);

            var ranges = new Dictionary<string, Range<double?>>();

            foreach (var reader in listOfReaders)
            {
                using (reader)
                {
                    var logData =
                        reader.GetData(
                            new ResponseContext() { HasAllRequestedValues = false, RequestLatestValues = null },
                            mnemonicFilterDictionary, unitDictionary, dataTypeDictionary, nullValueDictionary, out ranges);
                    Assert.AreEqual(1, logData.Count);
                    Assert.AreEqual(2, logData[0].Count);
                    Assert.AreEqual(1, logData[0][0].Count);
                }
            }
        }

        private static void InitMnemonics(out string[] mnemonics, out string[] mnemonicFilter, out SortedDictionary<int, string> mnemonicFilterDictionary)
        {
            mnemonics = ChannelDataReader.Split("DEPTH,A,B,C");
            var tempFilter = new[] { "MD", "A", "B" };
            var tempDictionary = new SortedDictionary<int, string>();
            mnemonics.ForEach((x, i) =>
            {
                if (tempFilter.ContainsIgnoreCase(x))
                    tempDictionary.Add(i, x);
            });
            mnemonicFilter = tempFilter;
            mnemonicFilterDictionary = tempDictionary;
        }

        private static void InitUnits(out string[] units, out SortedDictionary<int, string> unitDictionary)
        {
            units = ChannelDataReader.Split("unitless,m,m,m");
            var tempDictionary = new SortedDictionary<int, string>();
            units.ForEach((x, i) => tempDictionary.Add(i, x));
            unitDictionary = tempDictionary;
        }

        private static void InitDataTypes(out string[] dataTypes, out SortedDictionary<int, string> dataTypeDictionary)
        {
            dataTypes = new string[0];
            var tempDictionary = new SortedDictionary<int, string>();
            dataTypes.ForEach((x, i) => tempDictionary.Add(i, x));
            dataTypeDictionary = tempDictionary;
        }

        private static void InitNullValues(out string[] nullValues, out SortedDictionary<int, string> nullValueDictionary)
        {
            nullValues = ChannelDataReader.Split("-999.25,,-1000.1");
            var tempDictionary = new SortedDictionary<int, string>();
            nullValues.ForEach((x, i) => tempDictionary.Add(i, x));
            nullValueDictionary = tempDictionary;
        }

        #endregion
    }
}
