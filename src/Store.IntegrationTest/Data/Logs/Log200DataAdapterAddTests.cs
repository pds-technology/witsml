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

using System.IO;
using System.Linq;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Data.Channels;
using PDS.WITSMLstudio.Data.Channels;

namespace PDS.WITSMLstudio.Store.Data.Logs
{
    [TestClass]
    public partial class Log200DataAdapterAddTests : Log200TestBase
    {
        private IChannelDataProvider _channelDataProvider;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            _channelDataProvider = DevKit.Container.Resolve<IWitsmlDataAdapter<ChannelSet>>() as IChannelDataProvider;
        }

        [TestMethod]
        public void Channel200DataAdapter_UpdateChannelData_With_Special_Characters()
        {
            AddParents();

            // Initialize ChannelSet
            var mdChannelIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(Log, LoggingMethod.MWD, mdChannelIndex);

            // Add special channels
            var channelSet = Log.ChannelSet.First();
            channelSet.Channel.Add(LogGenerator.CreateChannel(Log, channelSet.Index, "Message", "MSG", null, "none", EtpDataType.@string, null));
            channelSet.Channel.Add(LogGenerator.CreateChannel(Log, channelSet.Index, "Count", "CNT", null, "none", EtpDataType.@long, null));

            // Initialize data block
            var uri = channelSet.GetUri();
            var dataBlock = new ChannelDataBlock(uri);
            var channelId = 1;
            var numRows = ChannelDataBlock.BatchSize;
            var flushRate = ChannelDataBlock.BlockFlushRateInMilliseconds;

            foreach (var channelIndex in channelSet.Index)
            {
                dataBlock.AddIndex(channelIndex);
            }

            foreach (var channel in channelSet.Channel)
            {
                dataBlock.AddChannel(channelId++, channel);
            }

            LogGenerator.GenerateChannelData(dataBlock, numRows);

            var reader = dataBlock.GetReader();

            Assert.IsTrue(reader.Read());

            // Read the first value for mnemonic "MSG"
            var msgValue = reader["MSG"];

            // Submit channel data
            _channelDataProvider.UpdateChannelData(uri, dataBlock.GetReader());

            var mnemonics = channelSet.Index.Select(i => i.Mnemonic)
                .Concat(channelSet.Channel.Select(c => c.Mnemonic))
                .ToList();

            // Query channel data
            var dataOut = _channelDataProvider.GetChannelData(uri, new Range<double?>(0, null), mnemonics, null);

            // Assert
            Assert.AreEqual(numRows, dataOut.Count);
            Assert.AreEqual(numRows, dataBlock.Count());
            Assert.AreEqual(2, dataOut[0].Count);
            Assert.AreEqual(5, dataOut[0][1].Count);
            Assert.AreEqual(msgValue, dataOut[0][1][3]);
            Assert.IsTrue(flushRate > 1000);
        }

        [TestMethod]
        public void Log200DataAdapter_Can_Add_And_Get_Log()
        {
            AddParents();

            var mdChannelIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(Log, LoggingMethod.MWD, mdChannelIndex);

            DevKit.AddAndAssert(Log);
            var log = DevKit.GetAndAssert(Log);

            Assert.AreEqual(Log.Citation.Title, log.Citation.Title);
            Assert.AreEqual(Log.Uuid, log.Uuid);
        }

        [TestMethod]
        public void Log200DataAdapter_Log_Can_Be_Added_With_Increasing_Log_Data()
        {
            AddParents();

            var numDataValue = 150;
            var mdChannelIndex = DevKit.LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(Log, LoggingMethod.MWD, mdChannelIndex);

            var secondaryIndex = DevKit.LogGenerator.CreateDateTimeIndex();
            var channelSet = Log.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Generate rows of data
            DevKit.LogGenerator.GenerateChannelData(Log.ChannelSet, numDataValue);

            File.WriteAllText("TestData/DepthLog-2.0-Well.xml", EnergisticsConverter.ObjectToXml(Well));
            File.WriteAllText("TestData/DepthLog-2.0-Wellbore.xml", EnergisticsConverter.ObjectToXml(Wellbore));
            File.WriteAllText("TestData/DepthLog-2.0.xml", EnergisticsConverter.ObjectToXml(Log));

            DevKit.AddAndAssert(Log);
            var log = DevKit.GetAndAssert(Log);

            Assert.AreEqual(Log.Citation.Title, log.Citation.Title);
            Assert.AreEqual(Log.Uuid, log.Uuid);

            var mnemonics = channelSet.Index.Select(i => i.Mnemonic).Concat(channelSet.Channel.Select(c => c.Mnemonic)).ToList();
            var logData = _channelDataProvider.GetChannelData(channelSet.GetUri(), new Range<double?>(0, null), mnemonics, null);

            // Test that the rows of data before and after are the same.
            Assert.AreEqual(numDataValue, logData.Count);

            var start = logData[0][0][0];
            var end = logData[numDataValue - 1][0][0];

            Assert.IsTrue(double.Parse(end.ToString()) > double.Parse(start.ToString()));

            // Check PointMetadata values
            foreach (var row in logData)
            {
                var ropValues = ChannelDataReader.ReadValue(row[1][0]) as object[];
                var hkldValues = ChannelDataReader.ReadValue(row[1][1]) as object[];

                Assert.IsNull(hkldValues);
                if (ropValues == null) continue;

                Assert.AreEqual(2, ropValues.Length);
                Assert.IsTrue(ropValues[0] == null || ropValues[0] is double);
                Assert.IsTrue(ropValues[1] == null || ropValues[1] is bool);
            }
        }

        [TestMethod]
        public void Log200DataAdapter_Log_Can_Be_Added_With_Decreasing_Log_Data()
        {
            AddParents();

            var numDataValue = 150;
            var secondaryIndex = DevKit.LogGenerator.CreateDateTimeIndex();
            var mdChannelIndexDecreasing = DevKit.LogGenerator.CreateMeasuredDepthIndex(IndexDirection.decreasing);
            DevKit.InitHeader(Log, LoggingMethod.surface, mdChannelIndexDecreasing);

            var channelSet = Log.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Generate rows of data
            DevKit.LogGenerator.GenerateChannelData(Log.ChannelSet, numDataValue);

            File.WriteAllText("TestData/DecreasingDepthLog-2.0-Well.xml", EnergisticsConverter.ObjectToXml(Well));
            File.WriteAllText("TestData/DecreasingDepthLog-2.0-Wellbore.xml", EnergisticsConverter.ObjectToXml(Wellbore));
            File.WriteAllText("TestData/DecreasingDepthLog-2.0.xml", EnergisticsConverter.ObjectToXml(Log));

            DevKit.AddAndAssert(Log);
           var log = DevKit.GetAndAssert(Log);

            Assert.AreEqual(Log.Citation.Title, log.Citation.Title);
            Assert.AreEqual(Log.Uuid, log.Uuid);

            var mnemonics = channelSet.Index.Select(i => i.Mnemonic).Concat(channelSet.Channel.Select(c => c.Mnemonic)).ToList();
            var logData = _channelDataProvider.GetChannelData(channelSet.GetUri(), new Range<double?>(null, null), mnemonics, null);

            // Test that the rows of data before and after are the same.
            Assert.AreEqual(numDataValue, logData.Count);

            var start = logData[0][0][0];
            var end = logData[numDataValue - 1][0][0];

            // Test the log is still decreasing
            Assert.IsTrue(double.Parse(end.ToString()) < double.Parse(start.ToString()));

            // Check PointMetadata values
            foreach (var row in logData)
            {
                var ropValues = ChannelDataReader.ReadValue(row[1][0]) as object[];
                var hkldValues = ChannelDataReader.ReadValue(row[1][1]) as object[];

                Assert.IsNull(hkldValues);
                if (ropValues == null) continue;

                Assert.AreEqual(2, ropValues.Length);
                Assert.IsTrue(ropValues[0] == null || ropValues[0] is double);
                Assert.IsTrue(ropValues[1] == null || ropValues[1] is bool);
            }
        }

        [TestMethod]
        public void Log200DataAdapter_Log_Can_Be_Added_With_Increasing_Time_Data()
        {
            AddParents();

            var numDataValue = 150;
            var dtChannelIndex = DevKit.LogGenerator.CreateDateTimeIndex();
            DevKit.InitHeader(Log, LoggingMethod.surface, dtChannelIndex);

            var secondaryIndex = DevKit.LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            var channelSet = Log.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Generate 150 rows of data
            DevKit.LogGenerator.GenerateChannelData(Log.ChannelSet, numDataValue);

            File.WriteAllText("TestData/TimeLog-2.0-Well.xml", EnergisticsConverter.ObjectToXml(Well));
            File.WriteAllText("TestData/TimeLog-2.0-Wellbore.xml", EnergisticsConverter.ObjectToXml(Wellbore));
            File.WriteAllText("TestData/TimeLog-2.0.xml", EnergisticsConverter.ObjectToXml(Log));

            DevKit.AddAndAssert(Log);
            var log = DevKit.GetAndAssert(Log);

            Assert.AreEqual(Log.Citation.Title, log.Citation.Title);
            Assert.AreEqual(Log.Uuid, log.Uuid);
        }

        [TestMethod]
        public void Log200DataAdapter_Log_Can_Be_Added_With_Secondary_Index()
        {
            AddParents();

            var mdChannelIndex = DevKit.LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(Log, LoggingMethod.MWD, mdChannelIndex);

            var secondaryIndex = DevKit.LogGenerator.CreateDateTimeIndex();
            var channelSet = Log.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Generate mock data
            DevKit.CreateMockChannelSetData(channelSet, channelSet.Index);

            File.WriteAllText("TestData/DepthLogWithSecondaryIndex-2.0-Well.xml", WitsmlParser.ToXml(Well));
            File.WriteAllText("TestData/DepthLogWithSecondaryIndex-2.0-Wellbore.xml", WitsmlParser.ToXml(Wellbore));
            File.WriteAllText("TestData/DepthLogWithSecondaryIndex-2.0.xml", WitsmlParser.ToXml(Log));

            DevKit.AddAndAssert(Log);
            var log = DevKit.GetAndAssert(Log);

            Assert.AreEqual(Log.Citation.Title, log.Citation.Title);
            Assert.AreEqual(Log.Uuid, log.Uuid);
        }
    }
}
