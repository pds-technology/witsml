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
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//using MongoDB.Driver;
//using MongoDB.Driver.Linq;
using PDS.Framework;
using PDS.Witsml.Data.Logs;
using PDS.Witsml.Server.Data.Channels;
using PDS.Witsml.Server.Data.Wellbores;
using PDS.Witsml.Server.Data.Wells;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log200DataAdapterAddTests
    {
        /*
        private DevKit200Aspect DevKit;
        private Log200Generator LogGenerator;
        private IContainer Container;
        private IDatabaseProvider Provider;
        private IWitsmlDataAdapter<Well> WellAdapter;
        private IWitsmlDataAdapter<Wellbore> WellboreAdapter;
        private IWitsmlDataAdapter<Log> LogAdapter;
        private IWitsmlDataAdapter<ChannelSet> ChannelSetAdapter;

        private Well Well1;
        private Wellbore Wellbore1;
        private Log Log1;
        private Log Log2;
        private Log LogDecreasing;
        private DataObjectReference WellReference;
        private DataObjectReference WellboreReference;

        [TestInitialize]
        public void TestSetUp()
        {
            var container = ContainerFactory.Create();
            DevKit = new DevKit200Aspect();
            LogGenerator = new Log200Generator();
            Container = ContainerFactory.Create();
            Provider = new DatabaseProvider(container, new MongoDbClassMapper());

            WellAdapter = new Well200DataAdapter(Provider);
            WellboreAdapter = new Wellbore200DataAdapter(Provider);
            ChannelSetAdapter = new ChannelSet200DataAdapter(Provider, new ChannelDataChunkAdapter(Provider));
            LogAdapter = new Log200DataAdapter(Provider, ChannelSetAdapter);

            Well1 = new Well() { Citation = DevKit.Citation("Well 01"), TimeZone = DevKit.TimeZone, Uuid = DevKit.Uid() };
            Well1.GeographicLocationWGS84 = DevKit.Location();

            WellReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Well),
                Title = Well1.Citation.Title,
                Uuid = Well1.Uuid
            };

            Wellbore1 = new Wellbore() { Citation = DevKit.Citation("Wellbore 01"), ReferenceWell = WellReference, Uuid = DevKit.Uid() };

            WellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = Wellbore1.Citation.Title,
                Uuid = Wellbore1.Uuid
            };

            Log1 = new Log() { Citation = DevKit.Citation("Log 01"), Wellbore = WellboreReference, Uuid = DevKit.Uid() };
            Log2 = new Log() { Citation = DevKit.Citation("Log 02"), Wellbore = WellboreReference };
            LogDecreasing = new Log() { Citation = DevKit.Citation("Log Decreasing"), Wellbore = WellboreReference, Uuid = DevKit.Uid() };

            ChannelIndex mdChannelIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            ChannelIndex mdChannelIndexDecreasing = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.decreasing);
            ChannelIndex dtChannelIndex = LogGenerator.CreateDateTimeIndex();

            DevKit.InitHeader(Log1, LoggingMethod.MWD, mdChannelIndex);
            DevKit.InitHeader(Log2, LoggingMethod.surface, dtChannelIndex);
            DevKit.InitHeader(LogDecreasing, LoggingMethod.surface, mdChannelIndexDecreasing);
        }

        [TestMethod]
        public void Log_can_be_added_with_uuid()
        {
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);
            LogAdapter.Add(DevKit.Parser(Log1), Log1);

            var log1 = LogAdapter.Get(Log1.GetUri());

            Assert.AreEqual(Log1.Citation.Title, log1.Citation.Title);
        }

        [TestMethod]
        public void Log_can_be_added_without_uuid()
        {
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);
            LogAdapter.Add(DevKit.Parser(Log2), Log2);

            var log2 = Provider.GetDatabase().GetCollection<Log>(ObjectNames.Log200).AsQueryable()
                .First(x => x.Citation.Title == Log2.Citation.Title);

            Assert.AreEqual(Log2.Citation.Title, log2.Citation.Title);
        }

        [TestMethod]
        public void Log_can_be_added_with_secondary_index()
        {
            var secondaryIndex = LogGenerator.CreateDateTimeIndex();
            var channelSet = Log1.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);
            
            DevKit.CreateMockChannelSetData(channelSet, channelSet.Index);

            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);
            LogAdapter.Add(DevKit.Parser(Log1), Log1);

            var log1 = LogAdapter.Get(Log1.GetUri());

            Assert.AreEqual(Log1.Citation.Title, log1.Citation.Title);
        }

        [TestMethod]
        public void Log_can_be_added_with_increasing_log_data()
        {
            var numDataValue = 150;
            var secondaryIndex = LogGenerator.CreateDateTimeIndex();
            var channelSet = Log1.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Save the Well and Wellbore
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);

            // Generate 150 rows of data
            LogGenerator.GenerateChannelData(Log1.ChannelSet, numDataValue);
            LogAdapter.Add(DevKit.Parser(Log1), Log1);

            var cda = new ChannelDataChunkAdapter(Provider);

            // Retrieve the data
            var indexCurve = channelSet.Channel.Select(c => c.Mnemonic).FirstOrDefault();
            var range = new Range<double?>(null, null);
            var logData = cda.GetData(channelSet.GetUri(), indexCurve, range, true);

            var rowCount = logData.Sum(ld => LogGenerator.DeserializeChannelSetData(ld.Data).Count);

            var start = logData.First().Indices.First().Start;
            var end = logData.Last().Indices.First().End;

            // Test that the rows of data before and after are the same.
            Assert.AreEqual(numDataValue, rowCount);

            // Test the log is still increasing
            Assert.IsTrue(end > start);
        }

        [TestMethod]
        public void Log_can_be_added_with_decreasing_log_data()
        {
            var numDataValue = 150;
            var secondaryIndex = LogGenerator.CreateDateTimeIndex();
            var channelSet = LogDecreasing.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Save the Well and Wellbore
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);

            // Generate 150 rows of data
            LogGenerator.GenerateChannelData(LogDecreasing.ChannelSet, numDataValue);
            LogAdapter.Add(DevKit.Parser(LogDecreasing), LogDecreasing);

            var cda = new ChannelDataChunkAdapter(Provider);

            // Retrieve the data
            var indexCurve = channelSet.Channel.Select(c => c.Mnemonic).FirstOrDefault();
            var range = new Range<double?>(null, null);
            var logData = cda.GetData(channelSet.GetUri(), indexCurve, range, false);

            var rowCount = logData.Sum(ld => LogGenerator.DeserializeChannelSetData(ld.Data).Count);

            var start = logData.First().Indices.First().Start;
            var end = logData.Last().Indices.First().End;

            // Test that the rows of data before and after are the same.
            Assert.AreEqual(numDataValue, rowCount);

            // Test the log is still decreasing
            Assert.IsTrue(end < start);
        }

        [TestMethod]
        public void Log_can_be_added_with_increasing_time_data()
        {
            var numDataValue = 150;
            var secondaryIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            var channelSet = Log2.ChannelSet.First();
            channelSet.Index.Add(secondaryIndex);

            // Save the Well and Wellbore
            WellAdapter.Add(DevKit.Parser(Well1), Well1);
            WellboreAdapter.Add(DevKit.Parser(Wellbore1), Wellbore1);

            // Generate 150 rows of data
            LogGenerator.GenerateChannelData(Log2.ChannelSet, numDataValue);
            LogAdapter.Add(DevKit.Parser(Log2), Log2);
        }

        */
    }
}
