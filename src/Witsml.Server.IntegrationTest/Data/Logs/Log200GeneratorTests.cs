using System.Collections.Generic;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class Log200GeneratorTests
    {
        private DevKit200Aspect DevKit;
        private DataObjectReference WellboreReference;
        private Log DepthLog;
        private Log TimeLog;

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
        }

        [TestMethod]
        public void Can_Generate_Log_200()
        {
            DevKit.InitChannelSet(DepthLog, true, numDataValue: 5);

            Assert.IsNotNull(DepthLog);

            Assert.IsNotNull(DepthLog.ChannelSet);
            Assert.AreEqual(1, DepthLog.ChannelSet.Count);
            Assert.AreEqual(2, DepthLog.ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(DepthLog.ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200()
        {          
            ChannelSet channelSet = DevKit.CreateChannelSet(DepthLog, true);
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

            for (int i = 0; i < 5; i++)
            {
                var channel = dataValues[0][1][0];
                if (channel != null)
                {
                    var channelValues = DevKit.DeserializeChannelValues(channel.ToString());
                    Assert.AreEqual(2, channelValues.Count);
                }
            }
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200_Decreasing()
        {
            ChannelSet channelSet = DevKit.CreateChannelSet(DepthLog, false);
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
            ChannelSet channelSet = DevKit.CreateChannelSet(DepthLog, true);
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);
            channelSetList.Add(channelSet);

            DevKit.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(2, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);
            Assert.AreEqual(2, channelSetList[1].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);

            dataValues = DevKit.DeserializeChannelSetData(channelSetList[1].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }


        [TestMethod]
        public void Can_Generate_Time_Log_200_From_ChannelSet()
        {
            ChannelSet channelSet = DevKit.CreateChannelSet(TimeLog, true);
            List<ChannelSet> channelSetList = new List<ChannelSet>();
            channelSetList.Add(channelSet);;

            DevKit.GenerateChannelData(channelSetList, numDataValue: 5);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            List<List<List<object>>> dataValues = DevKit.DeserializeChannelSetData(channelSetList[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(1, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            for (int i = 0; i < 5; i++)
            {
                var channel = dataValues[0][1][0];
                if (channel != null)
                {
                    var channelValues = DevKit.DeserializeChannelValues(channel.ToString());
                    Assert.AreEqual(2, channelValues.Count);
                }
            }
        }
    }
}
