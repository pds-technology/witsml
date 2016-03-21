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
        private DataObjectReference WellboreReference;
        private Log DepthLog;
        private Log TimeLog;
        private ChannelIndex MeasuredDepthIndex;
        private ChannelIndex DateTimeIndex;
        private ChannelIndex ElapseTimeIndex;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit = new DevKit200Aspect();

            WellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = DevKit.Name("Log200Generator"),
                Uuid = DevKit.Uid()
            };

            TimeLog = new Log() { TimeDepth = "Time", Citation = DevKit.Citation(DevKit.Name("Log200Generator")), Wellbore = WellboreReference, Uuid = DevKit.Uid() };
            DepthLog = new Log() { TimeDepth = "Depth", Citation = DevKit.Citation(DevKit.Name("Log200Generator")), Wellbore = WellboreReference, Uuid = DevKit.Uid() };

            MeasuredDepthIndex = DevKit.CreateChannelIndex(ChannelIndexType.measureddepth);
            DateTimeIndex = DevKit.CreateChannelIndex(ChannelIndexType.datetime);
            ElapseTimeIndex = DevKit.CreateChannelIndex(ChannelIndexType.elapsedtime);
        }

        [TestMethod]
        public void Can_Generate_Log_200()
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
        }

        [TestMethod]
        public void Can_Create_Log_200_Depth()
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
        public void Can_Create_Log_200_Time()
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
        public void Can_Generate_Depth_Log_200()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            ChannelSet channelSet = DevKit.CreateChannelSet(DepthLog, indexList);
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);

            DevKit.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200_Decreasing()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            MeasuredDepthIndex.Direction = IndexDirection.decreasing;
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            ChannelSet channelSet = DevKit.CreateChannelSet(DepthLog, indexList);
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);

            DevKit.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200_MultiChannelSet()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(MeasuredDepthIndex);
            indexList.Add(DateTimeIndex);

            ChannelSet channelSet1 = DevKit.CreateChannelSet(DepthLog, indexList);            
            ChannelSet channelSet2 = DevKit.CreateChannelSet(DepthLog, indexList);

            var PointMetadataList = new List<PointMetadata>()
                                    {
                                        DevKit.PointMetadata( "confidence", "confidence", EtpDataType.boolean )
                                    };
            channelSet2.Channel.Clear();
            channelSet2.Channel.Add(DevKit.Channel(DepthLog, indexList, "GR", "GR", "api", "gammer_ray", EtpDataType.@double, pointMetadataList: PointMetadataList));

            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet1);
            channelSetList.Add(channelSet2);

            DevKit.GenerateChannelData(channelSetList, numDataValue: 5);
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
        public void Can_Generate_Time_Log_200_From_ChannelSet()
        {
            List<ChannelIndex> indexList = new List<ChannelIndex>();
            indexList.Add(ElapseTimeIndex);

            ChannelSet channelSet = DevKit.CreateChannelSet(TimeLog, indexList);
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);;

            DevKit.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(1, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(1, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);
        }
    }
}
