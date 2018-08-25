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
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ComponentSchemas;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Data.Logs
{
    [TestClass]
    public class Log200GeneratorTests
    {
        private Log200Generator _logGenerator;
        private Log _depthLog;
        private Log _timeLog;
        private DataObjectReference _wellboreReference;
        private ChannelIndex _measuredDepthIndex;
        private ChannelIndex _dateTimeIndex;
        private ChannelIndex _elapseTimeIndex;
        private PointMetadata _booleanPointMetadata;
        private PointMetadata _floatPointMetadata;
        private ChannelSet _depthLogChannelSet;
        private ChannelSet _timeLogChannelSet;

        [TestInitialize]
        public void TestSetUp()
        {
            _logGenerator = new Log200Generator();
            _wellboreReference = new DataObjectReference
            {
                ContentType = EtpContentTypes.Witsml200.For(ObjectTypes.Wellbore),
                Title = _logGenerator.Name("Wellbore"),
                Uuid = _logGenerator.Uid()
            };

            _timeLog = new Log() { TimeDepth = "Time", Citation = _logGenerator.CreateCitation(_logGenerator.Name("Citation")), Wellbore = _wellboreReference, Uuid = _logGenerator.Uid() };
            _depthLog = new Log() { TimeDepth = "Depth", Citation = _logGenerator.CreateCitation(_logGenerator.Name("Citation")), Wellbore = _wellboreReference, Uuid = _logGenerator.Uid() };

            _measuredDepthIndex = _logGenerator.CreateMeasuredDepthIndex(IndexDirection.increasing);
            _dateTimeIndex = _logGenerator.CreateDateTimeIndex();
            _elapseTimeIndex = _logGenerator.CreateElapsedTimeIndex(IndexDirection.increasing);

            _booleanPointMetadata = _logGenerator.CreatePointMetadata("confidence", "confidence", EtpDataType.boolean);
            _floatPointMetadata = _logGenerator.CreatePointMetadata("Confidence", "Confidence", EtpDataType.@float);

            _depthLogChannelSet = _logGenerator.CreateChannelSet(_depthLog);
            _depthLogChannelSet.Index.Add(_measuredDepthIndex);
            _depthLogChannelSet.Index.Add(_dateTimeIndex);
            _depthLogChannelSet.Channel.Add(_logGenerator.CreateChannel(_depthLog, _depthLogChannelSet.Index, "Rate of Penetration", "ROP", UnitOfMeasure.mh, "Velocity", EtpDataType.@double, pointMetadataList: _logGenerator.List(_booleanPointMetadata)));
            _depthLogChannelSet.Channel.Add(_logGenerator.CreateChannel(_depthLog, _depthLogChannelSet.Index, "Hookload", "HKLD", UnitOfMeasure.klbf, "Force", EtpDataType.@double, null));
            _timeLogChannelSet = _logGenerator.CreateChannelSet(_timeLog);
            _timeLogChannelSet.Index.Add(_elapseTimeIndex);
            _timeLogChannelSet.Channel.Add(_logGenerator.CreateChannel(_timeLog, _timeLogChannelSet.Index, "Rate of Penetration", "ROP", UnitOfMeasure.mh, "Velocity", EtpDataType.@double, pointMetadataList: _logGenerator.List(_floatPointMetadata)));
        }

        [TestMethod]
        public void Log200Generator_Can_Generate_Depth_Log_Data_Increasing()
        {
            var channelSetList = new List<ChannelSet> {_depthLogChannelSet};
            var numDataValue = 150;

            _logGenerator.GenerateChannelData(channelSetList, numDataValue);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            var dataValues = _logGenerator.DeserializeChannelSetData(channelSetList[0].GetData());
            Assert.AreEqual(numDataValue, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            for (var i = 0; i < numDataValue; i++)
            {
                var channel = dataValues[i][1][0];
                if (channel != null)
                {
                    var channelValues = _logGenerator.DeserializeChannelValues(channel.ToString());
                    Assert.IsNotNull(channelValues[0]);
                }
            }
        }

        [TestMethod]
        public void Log200Generator_Can_Generate_Depth_Log_Data_Decreasing()
        {
            _depthLogChannelSet.Index[0].Direction = IndexDirection.decreasing;

            var channelSetList = new List<ChannelSet>();
            channelSetList.Add(_depthLogChannelSet);

            var numDataValue = 150;
            _logGenerator.GenerateChannelData(channelSetList, numDataValue);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);

            var dataValues = _logGenerator.DeserializeChannelSetData(channelSetList[0].GetData());
            Assert.AreEqual(numDataValue, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
        }

        [TestMethod]
        public void Log200Generator_Can_Generate_Depth_Log_Data_MultiChannelSet()
        {
            var channelSet2 = _logGenerator.CreateChannelSet(_depthLog);
            channelSet2.Index.Add(_measuredDepthIndex);
            channelSet2.Index.Add(_dateTimeIndex);
            channelSet2.Channel.Add(_logGenerator.CreateChannel(_depthLog, channelSet2.Index, "GR", "GR", UnitOfMeasure.gAPI, "gamma_ray", EtpDataType.@double, pointMetadataList: _logGenerator.List(_floatPointMetadata)));

            var channelSetList = new List<ChannelSet> {_depthLogChannelSet, channelSet2};
            var numDataValue = 150;

            _logGenerator.GenerateChannelData(channelSetList, numDataValue);
            Assert.AreEqual(2, channelSetList.Count);
            Assert.AreEqual(2, channelSetList[0].Channel.Count);
            Assert.AreEqual(1, channelSetList[1].Channel.Count);

            var dataValues = _logGenerator.DeserializeChannelSetData(channelSetList[0].GetData());
            Assert.AreEqual(numDataValue, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(2, dataValues[0][1].Count);

            dataValues = _logGenerator.DeserializeChannelSetData(channelSetList[1].GetData());
            Assert.AreEqual(numDataValue, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(2, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);
        }

        [TestMethod]
        public void Log200Generator_Can_Generate_Time_Log_Data()
        {
            var channelSetList = new List<ChannelSet> {_timeLogChannelSet};
            var numDataValue = 150;

            _logGenerator.GenerateChannelData(channelSetList, numDataValue);
            Assert.AreEqual(1, channelSetList.Count);
            Assert.AreEqual(1, channelSetList[0].Channel.Count);

            var dataValues = _logGenerator.DeserializeChannelSetData(channelSetList[0].GetData());
            Assert.AreEqual(numDataValue, dataValues.Count);
            Assert.AreEqual(2, dataValues[0].Count);
            Assert.AreEqual(1, dataValues[0][0].Count);
            Assert.AreEqual(1, dataValues[0][1].Count);

            for (var i = 0; i < numDataValue; i++)
            {
                var channel = dataValues[i][1][0];
                if (channel != null)
                {
                    var channelValues = _logGenerator.DeserializeChannelValues(channel.ToString());
                    Assert.IsNotNull(channelValues[0]);
                }
            }
        }
    }
}
