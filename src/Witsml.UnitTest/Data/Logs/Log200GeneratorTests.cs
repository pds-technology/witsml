using System.Collections.Generic;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Data.Logs
{
    [TestClass]
    public class Log200GeneratorTests
    {
        private Log200Generator LogGenerator;
        private Log DepthLog;
        private Log TimeLog;
        private DataObjectReference WellboreReference;
        private ChannelIndex MeasuredDepthIndex;
        private ChannelIndex DateTimeIndex;
        private ChannelIndex ElapseTimeIndex;
        private PointMetadata BooleanPointMetadata;
        private PointMetadata FloatPointMetadata;
        private ChannelSet DepthLogChannelSet;
        private ChannelSet TimeLogChannelSet;

        [TestInitialize]
        public void TestSetUp()
        {
            LogGenerator = new Log200Generator();
            WellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = LogGenerator.Name("Wellbore"),
                Uuid = LogGenerator.Uid()
            };

            TimeLog = new Log() { TimeDepth = "Time", Citation = LogGenerator.CreateCitation(LogGenerator.Name("Citation")), Wellbore = WellboreReference, Uuid = LogGenerator.Uid() };
            DepthLog = new Log() { TimeDepth = "Depth", Citation = LogGenerator.CreateCitation(LogGenerator.Name("Citation")), Wellbore = WellboreReference, Uuid = LogGenerator.Uid() };

            MeasuredDepthIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DateTimeIndex = LogGenerator.CreateDateTimeIndex();
            ElapseTimeIndex = LogGenerator.CreateElapsedTimeIndex(IndexDirection.increasing);

            BooleanPointMetadata = LogGenerator.CreatePointMetadata("confidence", "confidence", EtpDataType.boolean);
            FloatPointMetadata = LogGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float);

            DepthLogChannelSet = LogGenerator.CreateChannelSet(DepthLog);
            DepthLogChannelSet.Index.Add(MeasuredDepthIndex);
            DepthLogChannelSet.Index.Add(DateTimeIndex);
            DepthLogChannelSet.Channel.Add(LogGenerator.CreateChannel(DepthLog, DepthLogChannelSet.Index, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: LogGenerator.List(BooleanPointMetadata)));
            DepthLogChannelSet.Channel.Add(LogGenerator.CreateChannel(DepthLog, DepthLogChannelSet.Index, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double, null));

            TimeLogChannelSet = LogGenerator.CreateChannelSet(TimeLog);
            TimeLogChannelSet.Index.Add(ElapseTimeIndex);
            TimeLogChannelSet.Channel.Add(LogGenerator.CreateChannel(TimeLog, TimeLogChannelSet.Index, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: LogGenerator.List(FloatPointMetadata)));
        }

        [TestMethod]
        public void Can_Generate_Depth_Log()
        {
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(DepthLogChannelSet);

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = LogGenerator.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            for (int i = 0; i < 5; i++)
            {
                var channel = dataValues[i][1][0];
                if (channel != null)
                {
                    var channelValues = LogGenerator.DeserializeChannelValues(channel.ToString());
                    Assert.IsNotNull(channelValues[0]);
                }
            }
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_Decreasing()
        {
            DepthLogChannelSet.Index[0].Direction = IndexDirection.decreasing;

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(DepthLogChannelSet);

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = LogGenerator.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_MultiChannelSet()
        {
            ChannelSet channelSet2 = LogGenerator.CreateChannelSet(DepthLog);
            channelSet2.Index.Add(MeasuredDepthIndex);
            channelSet2.Index.Add(DateTimeIndex);
            channelSet2.Channel.Add(LogGenerator.CreateChannel(DepthLog, channelSet2.Index, "GR", "GR", "api", "gamma_ray", EtpDataType.@double, pointMetadataList: LogGenerator.List(FloatPointMetadata)));

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(DepthLogChannelSet);
            channelSetList.Add(channelSet2);

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(2, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);
            Assert.AreEqual(1, channelSetList[1].Channel.Count);

            List<List<List<object>>> dataValues = LogGenerator.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            dataValues = LogGenerator.DeserializeChannelSetData(channelSetList[1].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);
        }

        [TestMethod]
        public void Log_can_be_generated_with_time_data_for_channel_set()
        {
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(TimeLogChannelSet); ;

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(1, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = LogGenerator.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(1, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);

            for (int i = 0; i < 5; i++)
            {
                var channel = dataValues[i][1][0];
                if (channel != null)
                {
                    var channelValues = LogGenerator.DeserializeChannelValues(channel.ToString());
                    Assert.IsNotNull(channelValues[0]);
                }
            }
        }
    }
}
