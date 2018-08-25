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

using System;
using System.Collections.Generic;
using Energistics.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// EtpUris tests.
    /// </summary>
    [TestClass]
    public class EtpUrisTests
    {
        private DataGenerator _data;

        [TestInitialize]
        public void TestSetUp()
        {
            _data = new DataGenerator();
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_141_Uri_Family_From_Type()
        {
            var well = new Witsml141.Well { Uid = _data.Uid() };
            var uriFamily = EtpUris.GetUriFamily(well.GetType());

            Assert.IsTrue("eml://witsml14".EqualsIgnoreCase(uriFamily.ToString()));
            Assert.AreEqual("witsml", uriFamily.Family);
            Assert.AreEqual("1.4.1.1", uriFamily.Version);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_141_As_Default_Uri_Family_From_Invalid_Type()
        {
            var uriFamily = EtpUris.GetUriFamily(Type.GetType(""));

            Assert.IsTrue("eml://witsml14".EqualsIgnoreCase(uriFamily.ToString()));
            Assert.AreEqual("witsml", uriFamily.Family);
            Assert.AreEqual("1.4.1.1", uriFamily.Version);

            uriFamily = EtpUris.GetUriFamily(typeof(string));

            Assert.IsTrue("eml://witsml14".EqualsIgnoreCase(uriFamily.ToString()));
            Assert.AreEqual("witsml", uriFamily.Family);
            Assert.AreEqual("1.4.1.1", uriFamily.Version);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_131_Uri_Family_From_Type()
        {
            var well = new Witsml131.Well { Uid = _data.Uid() };
            var uriFamily = EtpUris.GetUriFamily(well.GetType());

            Assert.IsTrue("eml://witsml13".EqualsIgnoreCase(uriFamily.ToString()));
            Assert.AreEqual("witsml", uriFamily.Family);
            Assert.AreEqual("1.3.1.1", uriFamily.Version);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_200_Uri_Family_From_Type()
        {
            var well = new Witsml200.Well { Uuid = _data.Uid() };
            var uriFamily = EtpUris.GetUriFamily(well.GetType());

            Assert.IsTrue("eml://witsml20".EqualsIgnoreCase(uriFamily.ToString()));
            Assert.AreEqual("witsml", uriFamily.Family);
            Assert.AreEqual("2.0", uriFamily.Version);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Well_141_Uri()
        {
            var well = new Witsml141.Well { Uid = _data.Uid() };
            var uri = well.GetUri();

            Assert.IsTrue($"eml://witsml14/well({ well.Uid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("well", uri.ObjectType);
            Assert.AreEqual(well.Uid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Well_200_Uri()
        {
            var well = new Witsml200.Well { Uuid = _data.Uid() };
            var uri = well.GetUri();

            Assert.IsTrue($"eml://witsml20/Well({ well.Uuid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("Well", uri.ObjectType);
            Assert.AreEqual(well.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Wellbore_141_Uri()
        {
            var wellbore = new Witsml141.Wellbore { Uid = _data.Uid(), UidWell = _data.Uid() };
            var uri = wellbore.GetUri();

            Assert.IsTrue($"eml://witsml14/well({ wellbore.UidWell })/wellbore({ wellbore.Uid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("wellbore", uri.ObjectType);
            Assert.AreEqual(wellbore.Uid, uri.ObjectId);
            Assert.AreEqual(uri, ((IDataObject)wellbore).GetUri());
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Wellbore_200_Uri()
        {
            var wellbore = new Witsml200.Wellbore { Uuid = _data.Uid() };
            var uri = wellbore.GetUri();

            Assert.IsTrue($"eml://witsml20/Wellbore({ wellbore.Uuid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("Wellbore", uri.ObjectType);
            Assert.AreEqual(wellbore.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_141_Uri()
        {
            var log = new Witsml141.Log { Uid = _data.Uid(), UidWell = _data.Uid(), UidWellbore = _data.Uid() };
            var uri = log.GetUri();

            Assert.IsTrue($"eml://witsml14/well({ log.UidWell })/wellbore({ log.UidWellbore })/log({ log.Uid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("log", uri.ObjectType);
            Assert.AreEqual(log.Uid, uri.ObjectId);
            Assert.AreEqual(uri, ((IDataObject)log).GetUri());
            Assert.AreEqual(uri, ((IWellObject)log).GetUri());
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_200_Uri()
        {
            var log = new Witsml200.Log { Uuid = _data.Uid() };
            var uri = log.GetUri();

            Assert.IsTrue($"eml://witsml20/Log({ log.Uuid })".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual("Log", uri.ObjectType);
            Assert.AreEqual(log.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_LogCurveInfo_131_Uri()
        {
            var logCurve = new Witsml131.ComponentSchemas.LogCurveInfo { Mnemonic = "ROP" };

            var log = new Witsml131.Log
            {
                Uid = _data.Uid(),
                UidWell = _data.Uid(),
                UidWellbore = _data.Uid(),
                LogCurveInfo = new List<Witsml131.ComponentSchemas.LogCurveInfo> {logCurve}
            };

            var uri = logCurve.GetUri(log);

            Assert.IsTrue($"eml://witsml13/well({log.UidWell})/wellbore({log.UidWellbore})/log({log.Uid})/logCurveInfo({logCurve.Mnemonic})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.LogCurveInfo, uri.ObjectType);
            Assert.AreEqual(logCurve.Mnemonic, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_LogCurveInfo_141_Uri()
        {
            var logCurve = new Witsml141.ComponentSchemas.LogCurveInfo { Mnemonic = new Witsml141.ComponentSchemas.ShortNameStruct("ROP") };

            var log = new Witsml141.Log
            {
                Uid = _data.Uid(),
                UidWell = _data.Uid(),
                UidWellbore = _data.Uid(),
                LogCurveInfo = new List<Witsml141.ComponentSchemas.LogCurveInfo> { logCurve }
            };

            var uri = logCurve.GetUri(log);

            Assert.IsTrue($"eml://witsml14/well({log.UidWell})/wellbore({log.UidWellbore})/log({log.Uid})/logCurveInfo({logCurve.Mnemonic})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.LogCurveInfo, uri.ObjectType);
            Assert.AreEqual(logCurve.Mnemonic.Value, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_200_ChannelSet_Uri()
        {
            var channelSet = new Witsml200.ChannelSet {Uuid = _data.Uid()};
            var log = new Witsml200.Log {Uuid = _data.Uid(), ChannelSet = new List<Witsml200.ChannelSet> {channelSet}};

            var uri = channelSet.GetUri(log);

            Assert.IsTrue($"eml://witsml20/Log({ log.Uuid })/ChannelSet({channelSet.Uuid})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.ChannelSet.ToPascalCase(), uri.ObjectType);
            Assert.AreEqual(channelSet.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_ChannelSet_200_Channel_Uri()
        {
            var channel = new Witsml200.Channel { Uuid = _data.Uid(), Mnemonic = "ROP" };
            var channelSet = new Witsml200.ChannelSet {Uuid = _data.Uid(), Channel = new List<Witsml200.Channel> {channel}};

            var uri = channel.GetUri(channelSet);

            Assert.IsTrue($"eml://witsml20/ChannelSet({channelSet.Uuid})/Channel({channel.Uuid})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.Channel.ToPascalCase(), uri.ObjectType);
            Assert.AreEqual(channel.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_ChannelSet_200_Channel_Uri()
        {
            var channel = new Witsml200.Channel { Uuid = _data.Uid(), Mnemonic = "ROP" };
            var channelSet = new Witsml200.ChannelSet {Uuid = _data.Uid(), Channel = new List<Witsml200.Channel> {channel}};
            var log = new Witsml200.Log {Uuid = _data.Uid(), ChannelSet = new List<Witsml200.ChannelSet> {channelSet}};
            var uri = channel.GetUri(log, channelSet);

            Assert.IsTrue($"eml://witsml20/Log({log.Uuid})/ChannelSet({channelSet.Uuid})/Channel({channel.Uuid})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.Channel.ToPascalCase(), uri.ObjectType);
            Assert.AreEqual(channel.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Channel_200_ChannelIndex_Uri()
        {
            var channelIndex = new Witsml200.ComponentSchemas.ChannelIndex {Mnemonic = "MD"};
            var channel = new Witsml200.Channel { Uuid = _data.Uid(), Mnemonic = "ROP", Index = new List<Witsml200.ComponentSchemas.ChannelIndex> {channelIndex}};

            var uri = channelIndex.GetUri(channel);

            Assert.IsTrue($"eml://witsml20/Channel({channel.Uuid})/ChannelIndex({channelIndex.Mnemonic})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.ChannelIndex.ToPascalCase(), uri.ObjectType);
            Assert.AreEqual(channelIndex.Mnemonic, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_ChannelSet_200_ChannelIndex_Uri()
        {
            var channelIndex = new Witsml200.ComponentSchemas.ChannelIndex { Mnemonic = "MD" };
            var channel = new Witsml200.Channel { Uuid = _data.Uid(), Mnemonic = "ROP", Index = new List<Witsml200.ComponentSchemas.ChannelIndex> { channelIndex } };
            var channelSet = new Witsml200.ChannelSet { Uuid = _data.Uid(), Channel = new List<Witsml200.Channel> { channel } };

            var uri = channelIndex.GetUri(channelSet);

            Assert.IsTrue($"eml://witsml20/ChannelSet({channelSet.Uuid})/ChannelIndex({channelIndex.Mnemonic})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.ChannelIndex.ToPascalCase(), uri.ObjectType);
            Assert.AreEqual(channelIndex.Mnemonic, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Log_ChannelSet_200_ChannelIndex_Uri()
        {
            var channelIndex = new Witsml200.ComponentSchemas.ChannelIndex { Mnemonic = "MD" };
            var channel = new Witsml200.Channel { Uuid = _data.Uid(), Mnemonic = "ROP", Index = new List<Witsml200.ComponentSchemas.ChannelIndex> { channelIndex } };
            var channelSet = new Witsml200.ChannelSet { Uuid = _data.Uid(), Channel = new List<Witsml200.Channel> { channel } };
            var log = new Witsml200.Log { Uuid = _data.Uid(), ChannelSet = new List<Witsml200.ChannelSet> { channelSet } };

            var uri = channelIndex.GetUri(log, channelSet);

            Assert.IsTrue($"eml://witsml20/Log({log.Uuid})/ChannelSet({channelSet.Uuid})/ChannelIndex({channelIndex.Mnemonic})".EqualsIgnoreCase(uri.ToString()));
            Assert.AreEqual(ObjectTypes.ChannelIndex.ToPascalCase(), uri.ObjectType);
            Assert.AreEqual(channelIndex.Mnemonic, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_Activity_200_Uri()
        {
            var activity = new Witsml200.Activity { Uuid = _data.Uid() };
            var uri = activity.GetUri();

            Assert.AreEqual($"eml://eml21/Activity({ activity.Uuid })", uri.ToString());
            Assert.AreEqual($"{EtpContentTypes.Eml210};type=Activity", uri.ContentType.ToString());
            Assert.AreEqual("Activity", uri.ObjectType);
            Assert.AreEqual(activity.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_ActivityTemplate_200_Uri()
        {
            var activityTemplate = new Witsml200.ActivityTemplate { Uuid = _data.Uid() };
            var uri = activityTemplate.GetUri();

            Assert.AreEqual($"eml://eml21/ActivityTemplate({ activityTemplate.Uuid })", uri.ToString());
            Assert.AreEqual($"{EtpContentTypes.Eml210};type=ActivityTemplate", uri.ContentType.ToString());
            Assert.AreEqual("ActivityTemplate", uri.ObjectType);
            Assert.AreEqual(activityTemplate.Uuid, uri.ObjectId);
        }

        [TestMethod]
        public void EtpUris_GetUri_Can_Get_DataAssuranceRecord_200_Uri()
        {
            var dataAssuranceRecord = new Witsml200.DataAssuranceRecord { Uuid = _data.Uid() };
            var uri = dataAssuranceRecord.GetUri();

            Assert.AreEqual($"eml://eml21/DataAssuranceRecord({ dataAssuranceRecord.Uuid })", uri.ToString());
            Assert.AreEqual($"{EtpContentTypes.Eml210};type=DataAssuranceRecord", uri.ContentType.ToString());
            Assert.AreEqual("DataAssuranceRecord", uri.ObjectType);
            Assert.AreEqual(dataAssuranceRecord.Uuid, uri.ObjectId);
        }
    }
}
