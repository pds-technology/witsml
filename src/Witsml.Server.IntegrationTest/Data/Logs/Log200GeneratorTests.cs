using System.Collections.Generic;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log200GeneratorTests
    {
        private DevKit200Aspect DevKit;
        private Log200Generator LogGenerator;
        private DataObjectReference WellboreReference;
        private Log DepthLog;
        private Log TimeLog;
        private ChannelIndex MeasuredDepthIndex;
        private ChannelIndex DateTimeIndex;
        private ChannelIndex ElapseTimeIndex;
        private PointMetadata BooleanPointMetadata;
        private PointMetadata FloatPointMetadata;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();
            LogGenerator = new Log200Generator();
            WellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = DevKit.Name("Wellbore"),
                Uuid = DevKit.Uid()
            };

            TimeLog = new Log() { TimeDepth = "Time", Citation = DevKit.Citation(DevKit.Name("Citation")), Wellbore = WellboreReference, Uuid = DevKit.Uid() };
            DepthLog = new Log() { TimeDepth = "Depth", Citation = DevKit.Citation(DevKit.Name("Citation")), Wellbore = WellboreReference, Uuid = DevKit.Uid() };

            MeasuredDepthIndex = LogGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            DateTimeIndex = LogGenerator.CreateDateTimeIndex();
            ElapseTimeIndex = LogGenerator.CreateElapsedTimeIndex(IndexDirection.increasing);

            BooleanPointMetadata = LogGenerator.CreatePointMetadata("confidence", "confidence", EtpDataType.boolean);
            FloatPointMetadata = LogGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float);
        }

        [TestMethod]
        public void Can_Generate_Log_Data_with_Indexes()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            DevKit.InitChannelSet(DepthLog, indexList, numDataValue: 5);

            Assert.IsNotNull(DepthLog);

            Assert.IsNotNull(DepthLog.ChannelSet);
            Assert.AreEqual(1, DepthLog.ChannelSet.Count);
            Assert.AreEqual(2, DepthLog.ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(DepthLog.ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
        }

        [TestMethod]
        public void Log_can_be_created_with_depth_index()
        {
            Log tvdLog = DevKit.CreateLog(ChannelIndexType.trueverticaldepth, true);

            Assert.IsNotNull(tvdLog);
            Assert.IsNotNull(tvdLog.ChannelSet);
            Assert.AreEqual(1, tvdLog.ChannelSet.Count);
            Assert.AreEqual(2, tvdLog.ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(tvdLog.ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);

        }

        [TestMethod]
        public void Log_can_be_created_with_time_index()
        {
            Log dateTimeLog = DevKit.CreateLog(ChannelIndexType.datetime, true);

            Assert.IsNotNull(dateTimeLog);
            Assert.IsNotNull(dateTimeLog.ChannelSet);
            Assert.AreEqual(1, dateTimeLog.ChannelSet.Count);
            Assert.AreEqual(1, dateTimeLog.ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(dateTimeLog.ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            ChannelSet channelSet = LogGenerator.CreateChannelSet(DepthLog, indexList);
            channelSet.Channel.Add(DevKit.Channel(DepthLog, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: DevKit.List(BooleanPointMetadata)));
            channelSet.Channel.Add(DevKit.Channel(DepthLog, indexList, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double));

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            for (int i = 0; i < 5; i++)
            {
                var channel = dataValues[i][1][0];
                if (channel != null)
                {
                    var channelValues = DevKit.DeserializeChannelValues(channel.ToString());
                    Assert.IsNotNull(channelValues[0]);
                }
            }
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_Decreasing()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            MeasuredDepthIndex.Direction = IndexDirection.decreasing;
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            ChannelSet channelSet = LogGenerator.CreateChannelSet(DepthLog, indexList);
            channelSet.Channel.Add(DevKit.Channel(DepthLog, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: DevKit.List(BooleanPointMetadata)));
            channelSet.Channel.Add(DevKit.Channel(DepthLog, indexList, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double));

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_MultiChannelSet()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            ChannelSet channelSet1 = LogGenerator.CreateChannelSet(DepthLog, indexList);
            channelSet1.Channel.Add(DevKit.Channel(DepthLog, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: DevKit.List(BooleanPointMetadata)));
            channelSet1.Channel.Add(DevKit.Channel(DepthLog, indexList, "Hookload", "HKLD", "klbf", "Force", EtpDataType.@double));

            ChannelSet channelSet2 = LogGenerator.CreateChannelSet(DepthLog, indexList);
            channelSet2.Channel.Add(DevKit.Channel(DepthLog, indexList, "GR", "GR", "api", "gamma_ray", EtpDataType.@double, pointMetadataList: DevKit.List(FloatPointMetadata)));

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet1);
            channelSetList.Add(channelSet2);

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(2, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);
            Assert.AreEqual(1, channelSetList[1].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            dataValues = DevKit.DeserializeChannelSetData(channelSetList[1].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);
        }

        [TestMethod]
        public void Log_can_be_generated_with_time_data_for_channel_set()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(ElapseTimeIndex);

            ChannelSet channelSet = LogGenerator.CreateChannelSet(TimeLog, indexList);

            channelSet.Channel.Add(DevKit.Channel(TimeLog, indexList, "Rate of Penetration", "ROP", "m/h", "Velocity", EtpDataType.@double, pointMetadataList: DevKit.List(FloatPointMetadata)));

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);;

            LogGenerator.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(1, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(1, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);

            for (int i=0; i< 5; i++)
            {
                var channel = dataValues[i][1][0];
                if (channel != null)
                {
                    var channelValues = DevKit.DeserializeChannelValues(channel.ToString());
                    Assert.IsNotNull(channelValues[0]);
                }
            }
        }
    }
}
