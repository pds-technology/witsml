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
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Server.Data.Channels;
using Energistics.Datatypes;
using Energistics.Datatypes.ChannelData;
using PDS.Witsml.Data.Channels;

namespace PDS.Witsml.Server.Data.Logs
{
    public partial class Log200DataAdapterAddTests
    {
        private const string SpecialCharacters = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' , . / [ ] \b \f \n \r \t \ """;
        private static readonly Random _random = new Random(123);
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
            channelSet.Channel.Add(LogGenerator.CreateChannel(Log, channelSet.Index, "Message", "MSG", "none", "none", EtpDataType.@string, null));
            channelSet.Channel.Add(LogGenerator.CreateChannel(Log, channelSet.Index, "Count", "CNT", "none", "none", EtpDataType.@long, null));

            // Initialize data block
            var uri = channelSet.GetUri();
            var dataBlock = new ChannelDataBlock(uri);
            var channelId = 1;
            var numRows = 10;

            foreach (var channelIndex in channelSet.Index)
            {
                dataBlock.AddIndex(channelIndex);
            }

            foreach (var channel in channelSet.Channel)
            {
                dataBlock.AddChannel(channelId++, channel);
            }

            // TODO: Refactor into new method - LogGenerator.GenerateChannelData(ChannelDataBlock dataBlock, int numRows);
            // rows
            for (var i = 0; i < numRows; i++)
            {
                var index = (i * 0.1).IndexToScale(3);
                var indexes = new List<object> { index };

                // columns
                for (var j = 1; j < channelId; j++)
                {
                    dataBlock.Append(j, indexes, GenerateDataValue(channelSet.Channel[j - 1]));
                }
            }

            // Submit channel data
            _channelDataProvider.UpdateChannelData(uri, dataBlock.GetReader());

            var mnemonics = channelSet.Index.Select(i => i.Mnemonic)
                .Concat(channelSet.Channel.Select(c => c.Mnemonic))
                .ToList();

            // Query channel data
            var dataOut = _channelDataProvider.GetChannelData(uri, new Range<double?>(0, null), mnemonics, null);

            // Assert
            Assert.AreEqual(numRows, dataOut.Count);
            Assert.AreEqual(2, dataOut[0].Count);
            Assert.AreEqual(5, dataOut[0][1].Count);
            Assert.AreEqual(SpecialCharacters, dataOut[0][1][3]);
        }

        // TODO: Refactor into new method - LogGenerator.GenerateDataValue(Channel)
        private static object GenerateDataValue(Channel channel)
        {
            var dataType = channel.DataType.GetValueOrDefault(EtpDataType.@double);

            switch (dataType)
            {
                case EtpDataType.@long:
                    return _random.Next();

                case EtpDataType.@string:
                    return SpecialCharacters;

                case EtpDataType.@null:
                    return null;
            }

            return _random.NextDouble();
        }

        /// <summary>
        /// To test adding log with special characters using ChannelStreamingConsumer
        /// ~ ! @ # $ % ^ & * ( ) _ + { } | &lt; > ? ; : ' " , . / \ [ ] 
        /// \b Backspace, \f Form feed, \n New line, \r Carriage return, \t Tab, \"  Double quote, \\  Backslash character
        /// </summary>
        [TestMethod]
        public void Channel200DataAdapter_Add_With_Special_Characters_Escape_ChannelDataBlock()
        {
            AddParents();

            var logGenerator = new Log200Generator();
            ChannelIndex mdChannelIndex = logGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(Log, LoggingMethod.MWD, mdChannelIndex);

            var channelSet = Log.ChannelSet.First();

            var channelMetadataRecords = new List<ChannelMetadataRecord>();
            var dataBlocks = new Dictionary<EtpUri, ChannelDataBlock>();
            var channelParentUris = new Dictionary<long, EtpUri>();
            var dataItems = new List<DataItem>();
            var specialCharacters = @"~ ! @ # $ % ^ & * ( ) _ + { } | < > ? ; : ' , . / [ ] \b \f \n \r \t \ """;

            // Create metadata records
            CreateMetadataRecords(channelSet, mdChannelIndex, channelMetadataRecords);

            // InitializeDataBlocks with special characters
            InitializeDataBlocks(channelMetadataRecords, dataBlocks, channelParentUris, specialCharacters, dataItems);

            // Append
            AppendChannelData(dataItems, channelParentUris, dataBlocks, channelMetadataRecords);

            // Process
            ProcessDataBlock(dataBlocks, channelSet);

            var dataAdapter = DevKit.Container.Resolve<IWitsmlDataAdapter<ChannelSet>>() as IChannelDataProvider;
            Assert.IsNotNull(dataAdapter);
            var mnemonics = channelSet.Index.Select(i => i.Mnemonic).Concat(channelSet.Channel.Select(c => c.Mnemonic)).ToList();
            var dataOut = dataAdapter.GetChannelData(EtpUris.Witsml200.Append(ObjectTypes.ChannelSet, channelSet.Uuid),
               new Range<double?>(0, 1), mnemonics, null);

            Assert.AreEqual(1, dataOut.Count);
            Assert.AreEqual(2, dataOut[0].Count);
            Assert.AreEqual(3, dataOut[0][1].Count);
            Assert.AreEqual(0.0, dataOut[0][1][0]);
            Assert.AreEqual(new DateTimeOffset(2016, 1, 1, 0, 0, 0, new TimeSpan()), dataOut[0][1][1]);
            Assert.AreEqual(specialCharacters, dataOut[0][1][2]);

            var channelMetadata = dataAdapter.GetChannelMetadata(EtpUris.Witsml200.Append(ObjectTypes.ChannelSet, channelSet.Uuid));
            Assert.AreEqual(3, channelMetadata.Count);

            var mnemonicList = channelMetadata.Select(m => m.ChannelName).ToList();
            Assert.IsTrue(mnemonicList.Contains("ROP"));
            Assert.IsTrue(mnemonicList.Contains("HKLD"));
            Assert.IsTrue(mnemonicList.Contains("GR"));

            foreach (var channel in channelMetadata)
            {
                var channelDataType = channel.DataType;
                switch (channel.ChannelName)
                {
                    case "ROP":
                        Assert.AreEqual(EtpDataType.@double.ToString(), channelDataType);
                        break;
                    case "HKLD":
                        Assert.AreEqual(EtpDataType.@long.ToString(), channelDataType);
                        break;
                    case "GR":
                        Assert.AreEqual(EtpDataType.@string.ToString(), channelDataType);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the ETP URI.
        /// </summary>
        /// <param name="channelSet">The channel set.</param>
        /// <param name="withChannel">if set to <c>true</c> append channel.</param>
        /// <returns></returns>
        private EtpUri GetEtpUri(ChannelSet channelSet, bool withChannel)
        {
            if (withChannel)
                return EtpUris.Witsml200
                    .Append(ObjectTypes.Well, Well.Uuid)
                    .Append(ObjectTypes.Wellbore, Wellbore.Uuid)
                    .Append(ObjectTypes.Log, Log.Uuid)
                    .Append(ObjectTypes.ChannelSet, channelSet.Uuid)
                    .Append(ObjectTypes.Channel, channelSet.Index[0].Mnemonic);

            return EtpUris.Witsml200
                .Append(ObjectTypes.Well, Well.Uuid)
                .Append(ObjectTypes.Wellbore, Wellbore.Uuid)
                .Append(ObjectTypes.Log, Log.Uuid)
                .Append(ObjectTypes.ChannelSet, channelSet.Uuid);
        }

        /// <summary>
        /// Creates the metadata records.
        /// </summary>
        /// <param name="channelSet">The channel set.</param>
        /// <param name="mdChannelIndex">Index of the md channel.</param>
        /// <param name="channelMetadataRecords">The channel metadata records.</param>
        private void CreateMetadataRecords(ChannelSet channelSet, ChannelIndex mdChannelIndex, List<ChannelMetadataRecord> channelMetadataRecords)
        {
            var uri = GetEtpUri(channelSet, true);

            var indexMetadataRecord = new IndexMetadataRecord()
            {
                Uri = uri,
                Mnemonic = channelSet.Index[0].Mnemonic,
                Description = "Depth Index",
                Uom = channelSet.Index[0].Uom,
                Scale = 3,
                IndexType =
                    mdChannelIndex.IndexType.HasValue && mdChannelIndex.IndexType.Value == ChannelIndexType.datetime
                        ? ChannelIndexTypes.Time
                        : ChannelIndexTypes.Depth,
                Direction = IndexDirections.Increasing,
                CustomData = new Dictionary<string, DataValue>(0),
            };

            var channelId = 0;
            foreach (var channel in channelSet.Channel)
            {
                string channelDataType = "double";

                switch (channel.Mnemonic)
                {
                    case "ROP":
                        channelDataType = "double";
                        break;
                    case "HKLD":
                        channelDataType = "long";
                        break;
                    case "GR":
                        channelDataType = "string";
                        break;
                }

                channelMetadataRecords.Add(new ChannelMetadataRecord
                {
                    ChannelUri = uri,
                    ContentType = channel.GetUri().ContentType,
                    ChannelId = channelId++,
                    ChannelName = channel.Mnemonic,
                    Uom = channel.Uom,
                    MeasureClass = channel.CurveClass,
                    DataType = channelDataType,
                    Description = channel.Citation.Description,
                    Uuid = channel.Uuid,
                    Status = 0,
                    Source = channel.Source,
                    Indexes = new[]
                    {
                        indexMetadataRecord
                    },
                    CustomData = new Dictionary<string, DataValue>()
                });

            }
        }

        /// <summary>
        /// Initializes the data blocks.
        /// </summary>
        /// <param name="channelMetadataRecords">The channel metadata records.</param>
        /// <param name="dataBlocks">The data blocks.</param>
        /// <param name="channelParentUris">The channel parent uris.</param>
        /// <param name="specialCharacters">The special characters.</param>
        /// <param name="dataItems">The data items.</param>
        private static void InitializeDataBlocks(List<ChannelMetadataRecord> channelMetadataRecords, Dictionary<EtpUri, ChannelDataBlock> dataBlocks,
            Dictionary<long, EtpUri> channelParentUris, string specialCharacters, List<DataItem> dataItems)
        {
            foreach (var channel in channelMetadataRecords)
            {
                var uri = new EtpUri(channel.ChannelUri);

                var parentUri = uri.Parent; // Log or ChannelSet

                if (!dataBlocks.ContainsKey(parentUri))
                    dataBlocks[parentUri] = new ChannelDataBlock(parentUri);

                var dataBlock = dataBlocks[parentUri];
                channelParentUris[channel.ChannelId] = parentUri;

                foreach (var index in channel.Indexes)
                {
                    var dataType = index.IndexType == ChannelIndexTypes.Time
                        ? EtpDataType.@null.ToString()
                        : EtpDataType.@double.ToString();

                    dataBlock.AddIndex(
                        index.Mnemonic,
                        index.Uom,
                        dataType,
                        index.Direction == IndexDirections.Increasing,
                        index.IndexType == ChannelIndexTypes.Time);
                }

                dataBlock.AddChannel(channel.ChannelId, channel.ChannelName, channel.Uom, channel.DataType);
            }

            foreach (var channel in channelMetadataRecords)
            {
                DataValue dataValue = new DataValue() { Item = 0.0 };
                switch (channel.ChannelName)
                {
                    case "ROP":
                        dataValue = new DataValue() { Item = 0.0 };
                        break;
                    case "HKLD":
                        dataValue = new DataValue()
                        {
                            Item = new DateTimeOffset(2016, 1, 1, 0, 0, 0, new TimeSpan()).ToString("O")
                        };
                        break;
                    case "GR":
                        dataValue = new DataValue()
                        {
                            Item = specialCharacters
                        };
                        break;
                }

                dataItems.Add(new DataItem()
                {
                    ChannelId = channel.ChannelId,
                    Indexes = new List<long>() { 0 },
                    ValueAttributes = new DataAttribute[0],
                    Value = dataValue
                });
            }
        }

        /// <summary>
        /// Processes the data block.
        /// </summary>
        /// <param name="dataBlocks">The data blocks.</param>
        /// <param name="channelSet">The channel set.</param>
        private void ProcessDataBlock(Dictionary<EtpUri, ChannelDataBlock> dataBlocks, ChannelSet channelSet)
        {
            foreach (var item in dataBlocks)
            {
                var dataBlockItem = item.Value;
                var uri = item.Key;
                var reader = dataBlockItem.GetReader();
                dataBlockItem.Clear();

                var dataProvider =
                    DevKit.Container.Resolve<IChannelDataProvider>(new ObjectName(uri.ObjectType, uri.Version));
                dataProvider.UpdateChannelData(uri, reader);
            }

        }

        /// <summary>
        /// Appends the channel data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="channelParentUris">The channel parent uris.</param>
        /// <param name="dataBlocks">The data blocks.</param>
        /// <param name="channelMetadataRecords">The channel metadata records.</param>
        private void AppendChannelData(IList<DataItem> data, Dictionary<long, EtpUri> channelParentUris,
            Dictionary<EtpUri, ChannelDataBlock> dataBlocks, IList<ChannelMetadataRecord> channelMetadataRecords)
        {
            foreach (var dataItem in data)
            {
                // Check to see if we are accepting data for this channel
                if (!channelParentUris.ContainsKey(dataItem.ChannelId))
                    continue;

                var parentUri = channelParentUris[dataItem.ChannelId];

                var dataBlock = dataBlocks[parentUri];

                var channel = channelMetadataRecords.FirstOrDefault(x => x.ChannelId == dataItem.ChannelId);
                if (channel == null) continue;

                var indexes = DownscaleIndexValues(channel.Indexes, dataItem.Indexes);
                dataBlock.Append(dataItem.ChannelId, indexes, dataItem.Value.Item);
            }
        }

        /// <summary>
        /// Downscales the index values.
        /// </summary>
        /// <param name="indexMetadata">The index metadata.</param>
        /// <param name="indexValues">The index values.</param>
        /// <returns></returns>
        private IList<object> DownscaleIndexValues(IList<IndexMetadataRecord> indexMetadata, IList<long> indexValues)
        {
            return indexValues
                .Select((x, i) =>
                {
                    var index = indexMetadata[i];
                    return index.IndexType == ChannelIndexTypes.Depth
                        ? (object)(indexValues[i] / Math.Pow(10, index.Scale))
                        : DateTimeExtensions.FromUnixTimeMicroseconds(indexValues[i]);
                })
                .ToList();
        }
    }
}