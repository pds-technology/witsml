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
                string channelDataType;

                switch (channelId)
                {
                    case 0:
                        channelDataType = "double";
                        break;
                    case 1:
                        channelDataType = "string";
                        break;
                    default:
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
                    dataBlock.AddIndex(
                        index.Mnemonic,
                        index.Uom,
                        index.Direction == IndexDirections.Increasing,
                        index.IndexType == ChannelIndexTypes.Time);
                }

                dataBlock.AddChannel(channel.ChannelId, channel.ChannelName, channel.Uom);
            }

            foreach (var channel in channelMetadataRecords)
            {
                DataValue dataValue;
                switch (channel.ChannelId)
                {
                    case 0:
                        dataValue = new DataValue() { Item = 0.0 };
                        break;
                    case 1:
                        dataValue = new DataValue()
                        {
                            Item = new DateTimeOffset(2016, 1, 1, 0, 0, 0, new TimeSpan()).ToString("O")
                        };
                        break;
                    default:
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