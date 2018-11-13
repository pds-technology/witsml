//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
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
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Protocol.Core;
using Energistics.Etp.v11;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using Shouldly;

namespace PDS.WITSMLstudio.Store.Providers.ChannelStreaming
{
    [TestClass]
    [ProtocolRole((int)Protocols.ChannelStreaming, "consumer", "producer")]
    public class ChannelStreamingProtocolTests : IntegrationTestBase
    {
        public DevKit141Aspect DevKit { get; set; }

        [ClassInitialize]
        public static void ClassSetUp(TestContext context)
        {
            new SampleDataTests()
                .AddSampleData(context);
        }

        [TestInitialize]
        public void TestSetUp()
        {
            Logger.Debug($"Executing {TestContext.TestName}");
            DevKit = new DevKit141Aspect(TestContext);

            EtpSetUp(DevKit.Container);
            _server.Start();
        }

        [TestCleanup]
        public void TestTearDown()
        {
            _server?.Stop();
            EtpCleanUp();
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "11a")]
        [Description("Verifying that issuing a ChannelRangeRequest returns the correct ChannelData objects ( i.e. range and index order )")]
        public async Task IChannelStreamingProducer_Test_11a_ChannelData_Returned_For_Valid_ChannelRangeRequest()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelRangeInfos = new List<ChannelRangeInfo>();

            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "HKLD"),
                TestUris.LogMd.Append("logCurveInfo", "GRR")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, uris.Count);

            // Send Start message
            handler.Start();

            // Send ChannelDescribe message for 2 channels
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata response
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelHkld = channels.FirstOrDefault(c => c.ChannelName.Equals("HKLD"));
            var channelGrr = channels.FirstOrDefault(c => c.ChannelName.Equals("GRR"));

            channelRangeInfos.Add(ToChannelRangeInfo(channelHkld, ToScale(channelHkld, 4000.0), ToScale(channelHkld, 4001.0)));
            channelRangeInfos.Add(ToChannelRangeInfo(channelGrr, ToScale(channelGrr, 4002.0), ToScale(channelGrr, 4003.0)));

            // Send ChannelRangeRequest messages for 2 channels
            messageId = handler.ChannelRangeRequest(channelRangeInfos);

            // Wait for all ChannelData responses
            var data = await onChannelData;

            // Check count of data for each channel
            Assert.IsNotNull(data);
            Assert.AreEqual(uris.Count, data.Count);
            Assert.AreEqual(12, data.SelectMany(x => x.Message.Data).Count());

            // Check Correlation IDs
            foreach (var arg in data)
            {
                VerifyCorrelationId(arg, messageId);
            }

            // Check channel data is in index order, and 
            //... check channel data is within requested range
            VerifyChannelData(channelRangeInfos, data);
        }

        [TestMethod]
        [Description("Verifying that issuing a ChannelRangeRequest on a time log returns the correct ChannelData objects ( i.e. range and index order )")]
        public async Task IChannelStreamingProducer_ChannelData_Returned_For_Valid_ChannelRangeRequest_On_TimeLog()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelRangeInfos = new List<ChannelRangeInfo>();
            var logUri = new EtpUri("eml://witsml14/well(804415d0-b5e7-4389-a3c6-cdb790f5485f)/wellbore(d3e7d4bf-0f29-4c2b-974d-4871cf8001fd)/log(e2401b72-550f-4695-ab27-d5b0589bde18)");
            var uris = new List<string>
            {
                logUri.Append("logCurveInfo", "ROP"),
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, uris.Count);

            // Send Start message
            handler.Start();

            // Send ChannelDescribe message for 1 channel
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata response
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelRop = channels.FirstOrDefault(c => c.ChannelName.Equals("ROP"));

            // Request data from Mon, 04 Apr 2016 23:40:04.023 GMT to Mon, 04 Apr 2016 23:40:14.001 GMT
            channelRangeInfos.Add(ToChannelRangeInfo(channelRop, 1459813204023000, 1459813214001000));

            // Send ChannelRangeRequest messages for Message channel
            messageId = handler.ChannelRangeRequest(channelRangeInfos);

            // Wait for all ChannelData responses
            var data = await onChannelData;

            // Check count of data for each channel
            Assert.IsNotNull(data);
            Assert.AreEqual(uris.Count, data.Count);
            Assert.AreEqual(5, data.SelectMany(x => x.Message.Data).Count());

            // Check Correlation IDs
            foreach (var arg in data)
            {
                VerifyCorrelationId(arg, messageId);
            }

            // Check channel data is in index order, and 
            //... check channel data is within requested range
            VerifyChannelData(channelRangeInfos, data);

            // Check that the index values have the correct milliseconds
            var logData = data.SelectMany(x => x.Message.Data).ToList();
            var expectedIndexValues = new[]
            {
                new DateTimeOffset(2016, 4, 4, 18, 40, 05, 999, new TimeSpan(-5,0,0)),
                new DateTimeOffset(2016, 4, 4, 18, 40, 07, 43, new TimeSpan(-5,0,0)),
                new DateTimeOffset(2016, 4, 4, 18, 40, 11, 172, new TimeSpan(-5,0,0)),
                new DateTimeOffset(2016, 4, 4, 18, 40, 13, 890, new TimeSpan(-5,0,0)),
                new DateTimeOffset(2016, 4, 4, 18, 40, 14, 1, new TimeSpan(-5,0,0))
            };

            logData.ForEach((x, i) =>
            {
                Assert.AreEqual(expectedIndexValues[i].ToUnixTimeMicroseconds(), x.Indexes[0]);
            });
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "11b")]
        [Description("Verifying that issuing a ChannelRangeRequest with invalid channelId returns an error from the server")]
        public async Task IChannelStreamingProducer_Test_11b_ProtocolException_Returned_For_Invalid_ChannelRangeRequest()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelRangeInfos = new List<ChannelRangeInfo>();

            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "HKLD"),
                TestUris.LogMd.Append("logCurveInfo", "GRR")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);            
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send Start message
            handler.Start();

            // Send ChannelDescribe message for 2 channels
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata response
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelHkld = channels.FirstOrDefault(c => c.ChannelName.Equals("HKLD"));
            var channelGrr = channels.FirstOrDefault(c => c.ChannelName.Equals("GRR"));

            channelRangeInfos.Add(ToChannelRangeInfo(channelHkld, ToScale(channelHkld, 4000.0), ToScale(channelHkld, 4001.0)));
            channelRangeInfos.Add(ToChannelRangeInfo(channelGrr, ToScale(channelGrr, 4003.0), ToScale(channelGrr, 4002.0)));

            // Send ChannelRangeRequest messages for 2 channels, 1 valid and 1 invalid
            messageId = handler.ChannelRangeRequest(channelRangeInfos);

            // Wait for Protocol Exception
            var args = await onProtocolException;

            // Check response error code
            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message);
            Assert.AreEqual((int)EtpErrorCodes.InvalidArgument, args.Message.ErrorCode);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming")]
        [Description("Verifying that issuing a ChannelRangeRequest with invalid range returns a single error from the server")]
        public async Task IChannelStreamingProducer_ProtocolException_Returned_Once_For_Invalid_Range_On_ChannelRangeRequest()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelRangeInfos = new List<ChannelRangeInfo>();

            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "HKLD"),
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send Start message
            handler.Start();

            // Send ChannelDescribe message for 2 channels
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata response
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelHkld = channels.FirstOrDefault(c => c.ChannelName.Equals("HKLD"));

            // Since the log is increasing we will send an invalid decreasing range request
            channelRangeInfos.Add(ToChannelRangeInfo(channelHkld,  ToScale(channelHkld, 4002), ToScale(channelHkld, 4000.0)));

            // Send ChannelRangeRequest messages with invalid range
            handler.ChannelRangeRequest(channelRangeInfos);

            // Wait for Protocol Exception
            var args = await onProtocolException;

            // Check response error code
            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message);
            Assert.AreEqual((int)EtpErrorCodes.InvalidArgument, args.Message.ErrorCode);
            Assert.AreEqual("Invalid Argument: startIndex > endIndex", args.Message.ErrorMessage);

            // Wait 30s and verify that no further error messages are sent.
            var moreExceptions = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x, 30000);

            // Assert a Timeout exception because no more errors should be returned within 30s
            await Should.ThrowAsync<TimeoutException>(moreExceptions);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "61")]
        [Description("Verify a producer sends correct ChannelMetadata for increasing and decreasing time and depth channels")]
        public async Task IChannelStreamingProducer_Test_61_ChannelMetadata_Returned_For_Channels_From_Different_Logs()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();

            // Compose Uris for one channel from each pre-loaded log
            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "ROPI"),
                TestUris.LogMdDec.Append("logCurveInfo", "PESD"),
                TestUris.LogTime.Append("logCurveInfo", "GS_TV08"),
                TestUris.LogTimeDec.Append("logCurveInfo", "GS_TV06")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);

            // Send Start message
            handler.Start();

            // Request ChannelMetadata
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata message
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelRopi = channels.FirstOrDefault(c => c.ChannelName.Equals("ROPI"));
            var channelPesd = channels.FirstOrDefault(c => c.ChannelName.Equals("PESD"));
            var channelGstv8 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV08"));
            var channelGstv6 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV06"));

            // Verify that the index metadata is correct
            VerifyChannelIndex(channelRopi, ChannelIndexTypes.Depth, IndexDirections.Increasing);
            VerifyChannelIndex(channelPesd, ChannelIndexTypes.Depth, IndexDirections.Decreasing);
            VerifyChannelIndex(channelGstv8, ChannelIndexTypes.Time, IndexDirections.Increasing);
            VerifyChannelIndex(channelGstv6, ChannelIndexTypes.Time, IndexDirections.Decreasing);

            // Verify that the other properties correspond with properties of the log channel
            VerifyChannelProperties(channelRopi, "Inverse ROP", "Drilling", "double", "s/m", ToScale(channelRopi, 3198.266), ToScale(channelRopi, 4343.705));
            VerifyChannelProperties(channelPesd, "PWD Static Pressure", "General", "double", "kPa", ToScale(channelPesd, 4211.269), ToScale(channelPesd, 3080.614));
            VerifyChannelProperties(channelGstv8, "MudPit Volume Average 8", "MudLog", "double", "m3",
                ToMicroseconds("2015-11-22T08:03:51.0000000+00:00"),
                ToMicroseconds("2015-11-22T10:50:03.0000000+00:00"));
            VerifyChannelProperties(channelGstv6, "MudPit Volume Average 6", "MudLog", "double", "m3",
                ToMicroseconds("2015-11-29T17:44:05.0000000+00:00"),
                ToMicroseconds("2015-11-29T15:28:07.0000000+00:00"));

            // TODO: Properly handle parsing LogCurveInfo as root objects without prefix
            // Verify that, if provided, domainObject is the LogCurveInfo for the channel.
            //foreach (var curve in channels
            //    .Where(channel => channel.DomainObject != null)
            //    .Select(channel => channel.DomainObject.GetString())
            //    .Select(EnergisticsConverter.XmlToObject<LogCurveInfo>))
            //{
            //    Assert.IsNotNull(curve, "DomainObject is not LogCurveInfo");
            //}

            foreach (var domainObject in channels
                .Where(channel => channel.DomainObject != null)
                .Select(channel => channel.DomainObject.GetString()))
            {
                Assert.IsTrue(ValidateLogCurveInfo(domainObject), "DomainObject is not LogCurveInfo");
            }
        }

        bool ValidateLogCurveInfo(string xml)
        {
            var document = XDocument.Parse(xml);

            return typeof(LogCurveInfo).Name.EqualsIgnoreCase(document.Root?.Name.LocalName);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "62")]
        [Description("Verify a producer sends one ChannelData in response to ChannelStreamingStart for non-growing logs with a StreamingStartIndex of null (latestValue)")]
        public async Task IChannelStreamingProducer_Test_62_ChannelData_Returned_For_StreamingStartIndex_LatestValue()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelStreamingInfos = new List<ChannelStreamingInfo>();

            // Compose Uris for one channel from each pre-loaded log
            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "ROPI"),
                TestUris.LogMdDec.Append("logCurveInfo", "PESD"),
                TestUris.LogTime.Append("logCurveInfo", "GS_TV08"),
                TestUris.LogTimeDec.Append("logCurveInfo", "GS_TV06")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, uris.Count);

            // Send Start message 
            handler.Start();

            // Request ChannelMetadata
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata message
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelRopi = channels.FirstOrDefault(c => c.ChannelName.Equals("ROPI"));
            var channelPesd = channels.FirstOrDefault(c => c.ChannelName.Equals("PESD"));
            var channelGstv8 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV08"));
            var channelGstv6 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV06"));

            // Add a ChannelStreamingInfo record for each URI where StreamingStartIndex set to null to indicate latestValue
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelRopi));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelPesd));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelGstv8));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelGstv6));

            // Send ChannelStreamingStart message
            handler.ChannelStreamingStart(channelStreamingInfos);

            // Wait for ChannelData messages
            var argsData = await onChannelData;
            Assert.IsNotNull(argsData);

            // Verify that 4 ChannelData messages are received, one for each log channel.
            Assert.IsTrue(argsData.Count == 4);

            foreach (var arg in argsData)
            {
                // Verify for each ChannelData message that the correlationId is 0.
                VerifyCorrelationId(arg, 0);

                var messageData = arg.Message.Data;
                var channelId = messageData.Select(d => d.ChannelId).FirstOrDefault();

                // Verify that only one data point is received for each log channel.... 
                Assert.AreEqual(1, messageData.Count, $"The Message does not have the expected number of data points for channelId {channelId}.");
            }

            // Isolate data by channel
            var channelRopiData = GetChannelData(argsData, channelRopi.ChannelId);
            var channelPesdData = GetChannelData(argsData, channelPesd.ChannelId);
            var channelGstv8Data = GetChannelData(argsData, channelGstv8.ChannelId);
            var channelGstv6Data = GetChannelData(argsData, channelGstv6.ChannelId);
            
            // Verify that the data point is the "last" point in each log channel
            //... by checking the last index value            
            VerifyDataIndexValue(channelRopiData, channelRopiData.Length - 1, ToScale(channelRopi, 4343.705));
            VerifyDataIndexValue(channelPesdData, channelPesdData.Length - 1, ToScale(channelPesd, 3080.614));
            VerifyDataIndexValue(channelGstv8Data, channelGstv8Data.Length - 1, ToMicroseconds("2015-11-22T10:50:03.0000000+00:00"));
            VerifyDataIndexValue(channelGstv6Data, channelGstv6Data.Length - 1, ToMicroseconds("2015-11-29T15:28:07.0000000+00:00"));

            // Wait 30s and verify that no further ChannelData messages are sent.
            var onMoreChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, 1, 30000);

            // Assert a Timeout exception because no data should be returned within 30s
            await Should.ThrowAsync<TimeoutException>(onMoreChannelData);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "63")]
        [Description("Verify a producer sends 10 data points in ChannelData message(s) in response to ChannelStreamingStart for non-growing logs with a StreamingStartIndex indexValue of the 10th to the last value in the channel")]
        public async Task IChannelStreamingProducer_Test_63_ChannelData_Returned_For_StreamingStartIndex_With_IndexCount()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelStreamingInfos = new List<ChannelStreamingInfo>();

            // Compose Uris for one channel from each pre-loaded log
            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "ROPI"),
                TestUris.LogMdDec.Append("logCurveInfo", "PESD"),
                TestUris.LogTime.Append("logCurveInfo", "GS_TV08"),
                TestUris.LogTimeDec.Append("logCurveInfo", "GS_TV06")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, uris.Count);

            // Send Start message 
            handler.Start();

            // Request ChannelMetadata
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata message
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelRopi = channels.FirstOrDefault(c => c.ChannelName.Equals("ROPI"));
            var channelPesd = channels.FirstOrDefault(c => c.ChannelName.Equals("PESD"));
            var channelGstv8 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV08"));
            var channelGstv6 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV06"));

            // Add a ChannelStreamingInfo record for each URI where StreamingStartIndex set to the int value of 10 (indexCount = 10)
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelRopi, 10));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelPesd, 10));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelGstv8, 10));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelGstv6, 10));

            // Send ChannelStreamingStart message
            handler.ChannelStreamingStart(channelStreamingInfos);

            // Wait for ChannelData messages
            var argsData = await onChannelData;
            Assert.IsNotNull(argsData);

            // Verify that at least 4 ChannelData messages are received, at least one for each log channel.
            Assert.IsTrue(argsData.Count >= 4);

            foreach (var arg in argsData)
            {
                // Verify for each ChannelData message that the correlationId is 0.
                VerifyCorrelationId(arg, 0);

                var messageData = arg.Message.Data;
                var channelId = messageData.Select(d => d.ChannelId).FirstOrDefault();

                // Verify that 10 data points are received for each log channel.... 
                Assert.AreEqual(10, messageData.Count, $"The Message does not have the expected number of data points for channelId {channelId}.");

                //... with no duplication...
                var dupCount = messageData.GroupBy(c => c.Indexes.FirstOrDefault()).Count(grp => grp.Count() > 1);
                Assert.AreEqual(0, dupCount);

                // Get data to verify index order.
                var channelMetadata = channels.FirstOrDefault(c => c.ChannelId.Equals(channelId));
                Assert.IsNotNull(channelMetadata);

                var indexMetadata = channelMetadata.Indexes.FirstOrDefault();
                Assert.IsNotNull(indexMetadata);

                var indexes = messageData.Select(d => d.Indexes.FirstOrDefault()).ToArray();

                //... and in index order.
                VerifyIndexOrder(indexes, indexMetadata.Direction == IndexDirections.Increasing);
            }

            // Isolate data by channel
            var channelRopiData = GetChannelData(argsData, channelRopi.ChannelId);
            var channelPesdData = GetChannelData(argsData, channelPesd.ChannelId);
            var channelGstv8Data = GetChannelData(argsData, channelGstv8.ChannelId);
            var channelGstv6Data = GetChannelData(argsData, channelGstv6.ChannelId);
            
            // Verify that the 10 data points are the "last" 10 points in each log channel
            //... by checking the last index value
            VerifyDataIndexValue(channelRopiData, channelRopiData.Length - 1, ToScale(channelRopi, 4343.705));
            VerifyDataIndexValue(channelPesdData, channelPesdData.Length - 1, ToScale(channelPesd, 3080.614));
            VerifyDataIndexValue(channelGstv8Data, channelGstv8Data.Length - 1, ToMicroseconds("2015-11-22T10:50:03.0000000+00:00"));
            VerifyDataIndexValue(channelGstv6Data, channelGstv6Data.Length - 1, ToMicroseconds("2015-11-29T15:28:07.0000000+00:00"));

            // Wait 30s and verify that no further ChannelData messages are sent.
            var onMoreChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, 1, 30000);

            // Assert a Timeout exception because no data should be returned within 30s
            await Should.ThrowAsync<TimeoutException>(onMoreChannelData);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "64")]
        [Description("Verify a producer sends 10 data points in ChannelData message(s) in response to ChannelStreamingStart for non-growing logs with a StreamingStartIndex indexValue of the 10th to the last value in the channel")]
        public async Task IChannelStreamingProducer_Test_64_ChannelData_Returned_For_StreamingStartIndex_With_IndexValue()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelStreamingInfos = new List<ChannelStreamingInfo>();

            // Compose Uris for one channel from each pre-loaded log
            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "ROPI"),
                TestUris.LogMdDec.Append("logCurveInfo", "PESD"),
                TestUris.LogTime.Append("logCurveInfo", "GS_TV08"),
                TestUris.LogTimeDec.Append("logCurveInfo", "GS_TV06")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, uris.Count);

            // Send Start message 
            handler.Start();

            // Request ChannelMetadata
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata message
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelRopi = channels.FirstOrDefault(c => c.ChannelName.Equals("ROPI"));
            var channelPesd = channels.FirstOrDefault(c => c.ChannelName.Equals("PESD"));
            var channelGstv8 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV08"));
            var channelGstv6 = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV06"));

            channelStreamingInfos.Add(ToChannelStreamingInfo(channelRopi, ToScale(channelRopi, 4342.181)));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelPesd, ToScale(channelPesd, 3437.839)));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelGstv8, ToMicroseconds("2015-11-22T10:49:45.0000000+00:00")));
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelGstv6, ToMicroseconds("2015-11-29T15:28:25.0000000+00:00")));

            // Send ChannelStreamingStart message
            handler.ChannelStreamingStart(channelStreamingInfos);

            // Wait for ChannelData messages
            var argsData = await onChannelData;
            Assert.IsNotNull(argsData);

            // Verify that at least 4 ChannelData messages are received, at least one for each log channel.
            Assert.IsTrue(argsData.Count >= 4);

            foreach (var arg in argsData)
            {
                // Verify for each ChannelData message that the correlationId is 0.
                VerifyCorrelationId(arg, 0);

                var messageData = arg.Message.Data;
                var channelId = messageData.Select(d => d.ChannelId).FirstOrDefault();

                // Verify that 10 data points are received for each log channel.... 
                Assert.AreEqual(10, messageData.Count, $"The Message does not have the expected number of data points for channelId {channelId}.");

                //... with no duplication...
                var dupCount = messageData.GroupBy(c => c.Indexes.FirstOrDefault()).Count(grp => grp.Count() > 1);
                Assert.AreEqual(0, dupCount);

                // Get data to verify index order.
                var channelMetadata = channels.FirstOrDefault(c => c.ChannelId.Equals(channelId));
                Assert.IsNotNull(channelMetadata);

                var indexMetadata = channelMetadata.Indexes.FirstOrDefault();
                Assert.IsNotNull(indexMetadata);

                var indexes = messageData.Select(d => d.Indexes.FirstOrDefault()).ToArray();

                //... and in index order.
                VerifyIndexOrder(indexes, indexMetadata.Direction == IndexDirections.Increasing);
            }

            // Isolate data by channel
            var channelRopiData = GetChannelData(argsData, channelRopi.ChannelId);
            var channelPesdData = GetChannelData(argsData, channelPesd.ChannelId);
            var channelGstv8Data = GetChannelData(argsData, channelGstv8.ChannelId);
            var channelGstv6Data = GetChannelData(argsData, channelGstv6.ChannelId);

            // Verify that the 10 data points are the "last" 10 points in each log channel
            //... by checking the last index value
            VerifyDataIndexValue(channelRopiData, channelRopiData.Length - 1, ToScale(channelRopi, 4343.705));
            VerifyDataIndexValue(channelPesdData, channelPesdData.Length - 1, ToScale(channelPesd, 3080.614));
            VerifyDataIndexValue(channelGstv8Data, channelGstv8Data.Length - 1, ToMicroseconds("2015-11-22T10:50:03.0000000+00:00"));
            VerifyDataIndexValue(channelGstv6Data, channelGstv6Data.Length - 1, ToMicroseconds("2015-11-29T15:28:07.0000000+00:00"));

            // Wait 30s and verify that no further ChannelData messages are sent.
            var onMoreChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, 1, 30000);

            // Assert a Timeout exception because no data should be returned within 30s
            await Should.ThrowAsync<TimeoutException>(onMoreChannelData);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "68")]
        [Description("Verify a Server/Consumer responds with error when receiving a ChannelRangeRequest with to/from indexes not in index order of underlying Log.")]
        public async Task IChannelStreamingProducer_Test_68_ProtocolException_Returned_For_Invalid_ChannelRangeRequest_Range()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelRangeInfos = new List<ChannelRangeInfo>();

            // URI for GS_TV06 curve from an increasing time log.
            var uris = new List<string>
            {
                TestUris.LogTime.Append("logCurveInfo", "GS_TV06")
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onProtocolException = HandleAsync<IProtocolException>(x => handler.OnProtocolException += x);

            // Send Start message
            handler.Start();

            // Send ChannelDescribe message for 2 channels
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata response
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelHkld = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_TV06"));

            // ChannelRangeInfo with a decreasing range
            channelRangeInfos.Add(ToChannelRangeInfo(channelHkld, ToScale(channelHkld, 4001.0), ToScale(channelHkld, 4000.0)));

            // Send ChannelRangeRequest message with invalid range (log is increasing, range is decreasing)
            handler.ChannelRangeRequest(channelRangeInfos);

            // Wait for Protocol Exception
            var args = await onProtocolException;

            // Check response error code
            Assert.IsNotNull(args);
            Assert.IsNotNull(args.Message);
            Assert.AreEqual((int)EtpErrorCodes.InvalidArgument, args.Message.ErrorCode);
        }

        [TestMethod]
        //[TestCategory("ChannelStreaming"), TestProperty("TestID", "Benchmark")]
        [Description("Verify a producer sends one ChannelData in less than 2 seconds")]
        public async Task IChannelStreamingProducer_Benchmark_Producer_Sends_ChannelData_In_Less_Than_2_Seconds()
        {
            await RequestSessionAndAssert();

            var handler = _client.Handler<IChannelStreamingConsumer>();
            var channelStreamingInfos = new List<ChannelStreamingInfo>();

            // Compose Uris for one channel from each pre-loaded log
            var uris = new List<string>
            {
                TestUris.LogMd.Append("logCurveInfo", "GS_DXC"),
            };

            // Register event handlers
            var onChannelMetadata = HandleMultiPartAsync<ChannelMetadata>(x => handler.OnChannelMetadata += x);
            var onChannelData = HandleMultiPartAsync<ChannelData>(x => handler.OnChannelData += x, uris.Count);

            // Send Start message 
            handler.Start();

            // Request ChannelMetadata
            var messageId = handler.ChannelDescribe(uris);

            // Wait for ChannelMetadata message
            var argsMetadata = await onChannelMetadata;

            // Verify ChannelMetadata
            var channels = VerifyChannelMetadata(argsMetadata, uris, messageId);

            // Get channels by mnemonic for inspection
            var channelRopi = channels.FirstOrDefault(c => c.ChannelName.Equals("GS_DXC"));

            // Add a ChannelStreamingInfo record for each URI where StreamingStartIndex set to null to indicate latestValue
            channelStreamingInfos.Add(ToChannelStreamingInfo(channelRopi));

            // Send ChannelStreamingStart message
            handler.ChannelStreamingStart(channelStreamingInfos);

            // Start stopwatch timer
            var sw = new System.Diagnostics.Stopwatch();
            sw.Restart();

            // Wait for ChannelData messages
            await onChannelData;
            sw.Stop();

            // Assert that it took less than 2 seconds for the response
            Assert.IsTrue(sw.ElapsedMilliseconds < 2000);
        }

        private IList<ChannelMetadataRecord> VerifyChannelMetadata(IList<ProtocolEventArgs<ChannelMetadata>> args, IList<string> uris, long messageId)
        {
            var channels = args.SelectMany(x => x.Message.Channels).ToList();

            // Verify count of ChannelMetadataRecords received, one for each channel.
            Assert.IsTrue(args.Any());
            Assert.AreEqual(uris.Count, channels.Count, "Channel count does not match the URI count");

            // Verify that the last ChannelMetadata message has a messageFlag of 0x2.
            Assert.IsTrue(args.Select(x => x.Header).Last().IsFinalPart());

            // Verify that the ChannelMetadata message correlationId matches the corresponding ChannelDescribe messageId
            foreach (var arg in args)
            {
                VerifyCorrelationId(arg, messageId);
            }

            // Verify that all channelIds are unique
            var dupCount = channels.GroupBy(c => c.ChannelId).Count(grp => grp.Count() > 1);
            Assert.AreEqual(0, dupCount);

            // Verify the channelUri of each ChannelMetadata message matches the expected URI
            foreach (var uri in uris)
            {
                Assert.IsTrue(channels.Any(c => c.ChannelUri == uri), "URI not found in channel metatdata");
            }

            return channels;
        }

        private void VerifyChannelIndex(ChannelMetadataRecord channelMetadataRecord, ChannelIndexTypes channelIndexType, IndexDirections indexDirection)
        {
            // Verify that the channel exists
            Assert.IsNotNull(channelMetadataRecord);

            // Verify that the channel has an index
            var index = channelMetadataRecord.Indexes.FirstOrDefault();
            Assert.IsNotNull(index);

            // Verify that the index type and diretion are as expected.
            Assert.AreEqual(channelIndexType, index.IndexType);
            Assert.AreEqual(indexDirection, index.Direction);
        }

        private void VerifyChannelProperties(ChannelMetadataRecord channel, string description, string source, string dataType, string uom, long startIndex, long endIndex)
        {
            Assert.AreEqual(description, channel.Description);
            Assert.AreEqual(source, channel.Source);
            Assert.AreEqual(dataType, channel.DataType);
            Assert.AreEqual(uom, channel.Uom);
            Assert.AreEqual(startIndex, channel.StartIndex);
            Assert.AreEqual(endIndex, channel.EndIndex);
        }

        private void VerifyChannelData(List<ChannelRangeInfo> channelRangeInfos, List<ProtocolEventArgs<ChannelData>> data)
        {
            foreach (var channelRangeInfo in channelRangeInfos)
            {
                var dataItems = data
                    .SelectMany(d => d.Message.Data)
                    .Where(d => d.ChannelId == channelRangeInfo.ChannelId[0])
                    .ToArray();
                var indexes = dataItems
                    .Select(d => d.Indexes.FirstOrDefault())
                    .ToArray();

                // Do we have an index for every dataItem.
                Assert.AreEqual(dataItems.Length, indexes.Length);

                // Assert that there are no index values outside of the requested range
                Assert.IsFalse(dataItems
                    .Select(c => c.Indexes.First())
                    .Any(i => i < channelRangeInfo.StartIndex && i > channelRangeInfo.EndIndex));

                // Verify index order
                VerifyIndexOrder(indexes);
            }
        }

        private void VerifyIndexOrder(long[] indexes, bool isIncreasing = true)
        {
            for (var i = 1; i < indexes.Length; i++)
            {
                if (isIncreasing)
                {
                    Assert.IsTrue(indexes[i - 1] < indexes[i], "Channel indexes are not in increasing order");
                }
                else
                {
                    Assert.IsTrue(indexes[i - 1] > indexes[i], "Channel indexes are not in decreasing order");
                }
            }
        }

        private void VerifyDataIndexValue(DataItem[] dataItems, int index, long scaledValue)
        {
            Assert.IsNotNull(dataItems);
            Assert.AreEqual(scaledValue, dataItems[index].Indexes.FirstOrDefault());
        }

        private ChannelRangeInfo ToChannelRangeInfo(ChannelMetadataRecord channel, long startIndex, long endIndex)
        {
            return ToChannelRangeInfo(new [] { channel }, startIndex, endIndex);
        }

        private ChannelRangeInfo ToChannelRangeInfo(IEnumerable<ChannelMetadataRecord> channels, long startIndex, long endIndex)
        {
            return new ChannelRangeInfo
            {
                ChannelId = channels.Select(x => x.ChannelId).ToList(),
                StartIndex = startIndex,
                EndIndex = endIndex
            };
        }

        private ChannelStreamingInfo ToChannelStreamingInfo(ChannelMetadataRecord channel, object value = null)
        {
            return new ChannelStreamingInfo
            {
                ChannelId = channel.ChannelId,
                StartIndex = new StreamingStartIndex
                {
                    Item = value
                }
            };
        }

        private long ToScale(ChannelMetadataRecord channel, double indexValue)
        {
            var scale = channel.Indexes.Select(x => x.Scale).FirstOrDefault();
            return Convert.ToInt64(indexValue * Math.Pow(10, scale));
        }

        private long ToMicroseconds(string stringDateTime)
        {
            return DateTimeOffset.Parse(stringDateTime).ToUnixTimeMicroseconds();
        }

        private static DataItem[] GetChannelData(List<ProtocolEventArgs<ChannelData>> argsData, long channelId)
        {
            return argsData.SelectMany(d => d.Message.Data).Where(d => d.ChannelId.Equals(channelId)).ToArray();
        }

        private static class TestUris
        {
            public static readonly EtpUri LogMd = new EtpUri("eml://witsml14/well(b04e88c7-72c1-443e-aecc-8ea3c6)/wellbore(6ed5bb6b-f6e6-465b-9879-d87220f8)/log(L-3610902-MD)");
            public static readonly EtpUri LogTime = new EtpUri("eml://witsml14/well(b04e88c7-72c1-443e-aecc-8ea3c6)/wellbore(6ed5bb6b-f6e6-465b-9879-d87220f8)/log(L-3610903-Time)");
            public static readonly EtpUri LogMdDec = new EtpUri("eml://witsml14/well(b04e88c7-72c1-443e-aecc-8ea3c6)/wellbore(6ed5bb6b-f6e6-465b-9879-d87220f8)/log(L-3610903-MD-DEC)");
            public static readonly EtpUri LogTimeDec = new EtpUri("eml://witsml14/well(b04e88c7-72c1-443e-aecc-8ea3c6)/wellbore(6ed5bb6b-f6e6-465b-9879-d87220f8)/log(L-3610902-Time-Dec)");
        }
    }
}
