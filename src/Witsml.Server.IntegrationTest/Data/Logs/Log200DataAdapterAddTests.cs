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

using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Data.Logs
{
    public partial class Log200DataAdapterAddTests
    {
        private IChannelDataProvider _channelDataProvider;

        partial void BeforeEachTest()
        {
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

            // Read the first value for mnemonic "MSG"
            var msgValue = dataBlock.GetReader()["MSG"];

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
    }
}