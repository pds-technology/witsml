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
using System.IO;
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Store.Data.Channels;

namespace PDS.WITSMLstudio.Store.Data.ChannelSets
{
    /// <summary>
    /// ChannelSet200DataAdapter Update tests.
    /// </summary>
    [TestClass]
    public partial class ChannelSet200DataAdapterUpdateTests : ChannelSet200TestBase
    {
        private IChannelDataProvider _channelDataProvider;

        protected override void OnTestSetUp()
        {
            base.OnTestSetUp();

            _channelDataProvider = DevKit.Container.Resolve<IWitsmlDataAdapter<ChannelSet>>() as IChannelDataProvider;
        }

        [TestMethod]
        [Ignore, Description("Not sure if this should be done using UpdateChannelData on ChannelDataBlock/simple streamer")]
        public void ChannelSet200DataAdapter_Can_Update_ChannelSet_With_Middle_Depth_Data()
        {
            var dataGenerator = new DataGenerator();

            var channelIndex = new ChannelIndex { Direction = IndexDirection.increasing, IndexType = ChannelIndexType.measureddepth, Mnemonic = "MD", Uom = UnitOfMeasure.m };
            ChannelSet.Index = dataGenerator.List(channelIndex);
            ChannelSet.Channel = new List<Channel>
            {
                new Channel()
                {
                    Uuid = dataGenerator.Uid(),
                    Citation = new Citation {Title = dataGenerator.Name("ChannelSetTest")},
                    Mnemonic = "MSG",
                    Uom = null,
                    ChannelClass = dataGenerator.ToPropertyKindReference("velocity"),
                    DataType = EtpDataType.@long,
                    GrowingStatus = ChannelStatus.active,
                    Index = ChannelSet.Index,
                    StartIndex = new DepthIndexValue(),
                    EndIndex = new DepthIndexValue(),
                    SchemaVersion = OptionsIn.DataVersion.Version200.Value
                }
            };

            ChannelSet.Data = new ChannelData();
            ChannelSet.SetData(@"[
                [ [0 ], [ 3.11 ] ],
                [ [100 ], [ 3.12 ] ],
                [ [150 ], [ 3.14 ] ],
                [ [200 ], [ 3.15 ] ],
            ]");

            DevKit.AddAndAssert(ChannelSet);

            ChannelSet.Data = new ChannelData();
            ChannelSet.SetData(@"[
                [ [0 ], [ 3.11 ] ],
                [ [100 ], [ 3.12 ] ],
                [ [120 ], [ 3.13 ] ],
                [ [150 ], [ 3.14 ] ],
                [ [200 ], [ 3.15 ] ],
            ]");

            DevKit.UpdateAndAssert(ChannelSet);

            var mnemonics = ChannelSet.Index.Select(i => i.Mnemonic).Concat(ChannelSet.Channel.Select(c => c.Mnemonic)).ToList();
            var dataOut = _channelDataProvider.GetChannelData(ChannelSet.GetUri(), new Range<double?>(0, null), mnemonics, null);

            Assert.AreEqual(5, dataOut.Count);
            Assert.AreEqual(2, dataOut[1].Count);
            Assert.AreEqual(3.13, dataOut[2][1][0]);
        }
    }
}
