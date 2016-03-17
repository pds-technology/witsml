using System.Collections.Generic;
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace PDS.Witsml.Server.Data.Logs
{
    [TestClass]
    public class DevKitAspectLogGeneratorTests
    {
        private DevKit200Aspect DevKit200;
        private DataObjectReference WellboreReference;

        [TestInitialize]
        public void TestSetUp()
        {
            DevKit200 = new DevKit200Aspect();

            WellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = "Generated Citation Title",
                Uuid = "Generated Citition Uid"
            };

        }

        [TestMethod]
        public void Can_Generate_Log_200()
        {
            Log log200 = DevKit200.GenerateLog(numDataValue: 5);

            Assert.IsNotNull(log200);

            Assert.IsNotNull(log200.ChannelSet);
            Assert.AreEqual(1, log200.ChannelSet.Count);
            Assert.AreEqual(4, log200.ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DeserializeChannelSetData(log200.ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(4, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200_From_ChannelSet()
        {          
            Log log200 = new Log() { TimeDepth = "Depth", Citation = DevKit200.Citation("Log 01"), Wellbore = WellboreReference, Uuid = "Test Generate Log Wellbore Uuid" };

            ChannelSet channelSet = DevKit200.CreateDepthChannelSet_Increasing_Index(log200);
            List<ChannelSet> ChannelSet = new List<ChannelSet>();
            ChannelSet.Add(channelSet);

            DevKit200.GenerateChannelData(ChannelSet, numDataValue: 5);
            Assert.AreEqual(1, ChannelSet.Count);
            Assert.AreEqual(4, ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DeserializeChannelSetData(ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(4, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200_From_ChannelSet_Decreasing()
        {
            Log log200 = new Log() { TimeDepth = "Depth", Citation = DevKit200.Citation("Log 01"), Wellbore = WellboreReference, Uuid = "Test Generate Log Wellbore Uuid" };

            ChannelSet channelSet = DevKit200.CreateDepthChannelSet_Decreasing_Index(log200);
            List<ChannelSet> ChannelSet = new List<ChannelSet>();
            ChannelSet.Add(channelSet);

            DevKit200.GenerateChannelData(ChannelSet, numDataValue: 5);
            Assert.AreEqual(1, ChannelSet.Count);
            Assert.AreEqual(4, ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DeserializeChannelSetData(ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(4, dataValues[0].Count);
        }

        [TestMethod]
        public void Can_Generate_Depth_Log_200_From_ChannelSet_MultiChannelSet()
        {
            Log log200 = new Log() { Citation = DevKit200.Citation("Log 01"), Wellbore = WellboreReference, Uuid = "Test Generate Log Wellbore Uuid" };

            ChannelSet channelSet = DevKit200.CreateDepthChannelSet_Increasing_Index(log200);
            List<ChannelSet> ChannelSet = new List<ChannelSet>();
            ChannelSet.Add(channelSet);
            ChannelSet.Add(channelSet);

            DevKit200.GenerateChannelData(ChannelSet, numDataValue: 5);
            Assert.AreEqual(2, ChannelSet.Count);
            Assert.AreEqual(4, ChannelSet[0].Channel.Count);
            Assert.AreEqual(4, ChannelSet[1].Channel.Count);

            List<List<List<object>>> dataValues = DeserializeChannelSetData(ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(4, dataValues[0].Count);

            dataValues = DeserializeChannelSetData(ChannelSet[1].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(4, dataValues[0].Count);
        }


        [TestMethod]
        public void Can_Generate_Time_Log_200_From_ChannelSet()
        {
            Log log200 = new Log() { TimeDepth = "Time", Citation = DevKit200.Citation("Log 01"), Wellbore = WellboreReference, Uuid = "Test Generate Log Wellbore Uuid" };

            ChannelSet channelSet = DevKit200.CreateTimeChannelSet(log200);
            List<ChannelSet> ChannelSet = new List<ChannelSet>();
            ChannelSet.Add(channelSet);;

            DevKit200.GenerateChannelData(ChannelSet, numDataValue: 5);
            Assert.AreEqual(1, ChannelSet.Count);
            Assert.AreEqual(3, ChannelSet[0].Channel.Count);

            List<List<List<object>>> dataValues = DeserializeChannelSetData(ChannelSet[0].Data.Data);
            Assert.AreEqual(5, dataValues.Count);
            Assert.AreEqual(3, dataValues[0].Count);
        }

        private List<List<List<object>>> DeserializeChannelSetData(string data)
        {
            return JsonConvert.DeserializeObject<List<List<List<object>>>>(data);
        }

        private string SerializeChannelSetData(List<List<List<object>>> data)
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}
