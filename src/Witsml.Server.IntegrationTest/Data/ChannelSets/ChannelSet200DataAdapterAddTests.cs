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

/*
using System.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Data.Logs;

namespace PDS.Witsml.Server.Data.ChannelSets
{
    [TestClass]
    public class ChannelSet200DataAdapterAddTests
    {
        private DevKit200Aspect DevKit;
        private Log200Generator LogGenerator;
        private IDatabaseProvider Provider;
        private IWitsmlDataAdapter<ChannelSet> ChannelSetAdapter;
        private ChannelDataChunkAdapter ChunkAdapter;

        [TestInitialize]
        public void TestSetUp()
        {
            var container = ContainerFactory.Create();
            DevKit = new DevKit200Aspect();
            LogGenerator = new Log200Generator();
            Provider = new DatabaseProvider(container, new MongoDbClassMapper());

            ChunkAdapter = new ChannelDataChunkAdapter(Provider);
            ChannelSetAdapter = new ChannelSet200DataAdapter(Provider, ChunkAdapter);

            var log = new Log();
            var mdChannelIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DevKit.InitHeader(log, LoggingMethod.MWD, mdChannelIndex);

            ChannelSet = log.ChannelSet.First();
        }

        [TestMethod]
        [Ignore, Description("Move tests to the MongoDb.IntegrationTest library")]
        public void ChannelSet_can_be_added_with_depth_data()
        {
            ChannelSetAdapter.Add(DevKit.Parser(ChannelSet), ChannelSet);
        }

        [TestMethod]
        [Ignore, Description("Move tests to the MongoDb.IntegrationTest library")]
        public void ChannelSet_can_be_updated_with_middle_depth_data()
        {
            // Create
            ChannelSetAdapter.Add(DevKit.Parser(ChannelSet), ChannelSet);

            ChannelSet.Data = new ChannelData();

            // Add data that will update in the middle
            ChannelSet.Data.Data = @"[
                            [ [0.11 ], [ [ 1.11, false ], null, 3.11 ] ],
                            [ [100.11 ], [ [ 1.11, false ], null, 3.11 ] ],
                            [ [150.11 ], [ [ 1.11, false ], null, 3.11 ] ],
                            [ [200.11 ], [ [ 1.11, false ], null, 3.11 ] ],
                            [ [250.11 ], [ [ 1.11, false ], null, 3.11 ] ],
                        ]";

            // Update
            ChannelSetAdapter.Update(DevKit.Parser(ChannelSet), ChannelSet);
        }
    }
}
*/