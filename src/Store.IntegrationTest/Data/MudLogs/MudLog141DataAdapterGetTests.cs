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

using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;
using Energistics.DataAccess.WITSML141.ComponentSchemas;
using Energistics.DataAccess.WITSML141.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data.MudLogs
{
    [TestClass]
    public partial class MudLog141DataAdapterGetTests : MudLog141TestBase
    {
        private const string IntervalRangeQuery =
            "<mudLogs xmlns=\"http://www.witsml.org/schemas/1series\" version=\"1.4.1.1\">" +
            "<mudLog uidWell=\"490251091700\" uidWellbore=\"25-LX-11\" uid=\"h45a\">" +
            "<startMd uom=\"ft\">5011</startMd>" +
            "<endMd uom=\"ft\">5019</endMd>" +
            "<geologyInterval uid=\"\">" +
            "<mdTop uom=\"\"/>" +
            "<mdBottom uom=\"\"/>" +
            "</geologyInterval>" +
            "</mudLog>" +
            "</mudLogs>";

        [ClassInitialize]
        public static void ClassSetUp(TestContext context)
        {
            new SampleDataTests()
                .AddSampleData(context);
        }

        [TestMethod]
        public void MudLog141DataAdapter_GetFromStore_IntervalRangeInclusion_Minimal_Point()
        {
            var expectedIntervals = new[] { "gi123", "gi124", "gi125", "gi126" };
            var intervalRangeInclusion = OptionsIn.IntervalRangeInclusion.MinimumPoint;

            AssertIntervalRangeInclusion(intervalRangeInclusion, expectedIntervals);
        }

        [TestMethod]
        public void MudLog141DataAdapter_GetFromStore_IntervalRangeInclusion_Any_Part()
        {
            var expectedIntervals = new[] { "gi122", "gi123", "gi124", "gi125", "gi126" };
            var intervalRangeInclusion = OptionsIn.IntervalRangeInclusion.AnyPart;

            AssertIntervalRangeInclusion(intervalRangeInclusion, expectedIntervals);
        }

        [TestMethod]
        public void MudLog141DataAdapter_GetFromStore_IntervalRangeInclusion_Whole_Interval()
        {
            var expectedIntervals = new[] { "gi123", "gi124", "gi125" };
            var intervalRangeInclusion = OptionsIn.IntervalRangeInclusion.WholeInterval;

            AssertIntervalRangeInclusion(intervalRangeInclusion, expectedIntervals);
        }

        [TestMethod]
        public void MudLog141DataAdapter_GetFromStore_Get_Range_With_StartMD()
        {
            AddParents();

            MudLog.GeologyInterval = DevKit.MudLogGenerator.GenerateGeologyIntervals(5, 10.0);
            DevKit.AddAndAssert(MudLog);

            var query = new MudLog()
            {
                Uid = MudLog.Uid,
                UidWell = MudLog.UidWell,
                UidWellbore = MudLog.UidWellbore,
                StartMD = new MeasuredDepthCoord(160, MeasuredDepthUom.ft)
            };

            var result = DevKit.GetAndAssert(query, queryByExample: true);
            Assert.IsNotNull(result);
            Assert.AreEqual(MudLog.Uid, result.Uid);
            Assert.AreEqual(160, result.StartMD.Value);
            Assert.AreEqual(260, result.EndMD.Value);
            Assert.AreEqual(2, result.GeologyInterval.Count);
        }

        [TestMethod]
        public void MudLog141DataAdapter_GetFromStore_Get_Range_With_EndMD()
        {
            AddParents();

            MudLog.GeologyInterval = DevKit.MudLogGenerator.GenerateGeologyIntervals(5, 10.0);
            DevKit.AddAndAssert(MudLog);

            var query = new MudLog()
            {
                Uid = MudLog.Uid,
                UidWell = MudLog.UidWell,
                UidWellbore = MudLog.UidWellbore,
                EndMD = new MeasuredDepthCoord(140, MeasuredDepthUom.ft)
            };

            var result = DevKit.GetAndAssert(query, queryByExample: true);
            Assert.IsNotNull(result);
            Assert.AreEqual(MudLog.Uid, result.Uid);
            Assert.AreEqual(10, result.StartMD.Value);
            Assert.AreEqual(160, result.EndMD.Value);
            Assert.AreEqual(3, result.GeologyInterval.Count);
        }

        #region Helper Methods

        private void AssertIntervalRangeInclusion(OptionsIn.IntervalRangeInclusion intervalRangeInclusion, string[] expectedIntervals)
        {
            var result = DevKit.GetFromStore(ObjectTypes.MudLog, IntervalRangeQuery, null, intervalRangeInclusion);
            Assert.IsNotNull(result);

            var mudLogList = EnergisticsConverter.XmlToObject<MudLogList>(result.XMLout);
            Assert.IsNotNull(mudLogList);
            Assert.AreEqual(1, mudLogList.MudLog.Count);

            var mudLog = mudLogList.MudLog[0];
            Assert.IsNotNull(mudLogList);
            Assert.AreEqual(expectedIntervals.Length, mudLog.GeologyInterval.Count);

            mudLog.GeologyInterval.ForEach((x, i) => Assert.AreEqual(expectedIntervals[i], x.Uid));
        }

        #endregion
    }
}
