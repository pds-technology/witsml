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
using Energistics.DataAccess.WITSML200;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void Extensions_GetDescription_Returns_Correct_Description_For_Error_Code()
        {
            const string expected = "Function completed successfully";

            var actual = ErrorCodes.Success.GetDescription();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Extensions_GetDescription_Returns_Null_For_Unset_Error_Code()
        {
            const string expected = "Unset";

            var actual = ErrorCodes.Unset.GetDescription();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Extensions_GetDescription_Returns_DescriptionAttribute_Value()
        {
            var expected = "Get From Store";
            var actual = Functions.GetFromStore.GetDescription();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Extensions_NewUid_Returns_New_Uid_For_Plural_Object()
        {
            var well = new Energistics.DataAccess.WITSML131.Well();
            Assert.IsNotNull(well);
            well.Uid = well.NewUid();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(well.Uid));
            well.Uid = well.NewUid();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(well.Uid));
        }

        [TestMethod]
        public void Extensions_NewUid_Returns_New_Uid_For_Abstract_Object()
        {
            var rigUtilization = new RigUtilization();
            Assert.IsNotNull(rigUtilization);
            rigUtilization.Uuid = rigUtilization.NewUuid();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(rigUtilization.Uuid));
            rigUtilization.Uuid = rigUtilization.NewUuid();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(rigUtilization.Uuid));
        }

        [TestMethod]
        public void Extensions_GetVersion_Returns_The_Version_Of_The_dataObject()
        {
            var collection131 = new Energistics.DataAccess.WITSML131.DtsMeasurementList();
            Assert.AreEqual("1.3.1.1", collection131.GetVersion());
            var collection141 = new Energistics.DataAccess.WITSML141.LogList();
            Assert.AreEqual("1.4.1.1", collection141.GetVersion());
        }

        [TestMethod]
        public void Extensions_SetVersion_Sets_The_Version_Of_The_dataObject()
        {
            var collection131 = new Energistics.DataAccess.WITSML131.DtsMeasurementList();
            collection131.SetVersion("1.0");
            Assert.AreEqual("1.0", collection131.GetVersion());
            var collection141 = new Energistics.DataAccess.WITSML141.LogList();
            collection141.SetVersion("1.0");
            Assert.AreEqual("1.0", collection141.GetVersion());
        }

        [TestMethod]
        public void Extensions_BuildEmtpyQuery_Returns_An_Empty_Query()
        {
            var connection = new WITSMLWebServiceConnection(string.Empty, WMLSVersion.WITSML131);

            var collection131 = new Energistics.DataAccess.WITSML131.DtsMeasurementList();
            var query = connection.BuildEmptyQuery(collection131.GetType(), "1.3.1.1");
            Assert.IsNotNull(query);

            connection = new WITSMLWebServiceConnection(string.Empty, WMLSVersion.WITSML141);

            var collection141 = new Energistics.DataAccess.WITSML141.LogList();
            query = connection.BuildEmptyQuery(collection141.GetType(), "1.4.1.1");
            Assert.IsNotNull(query);
        }

        [TestMethod]
        public void Extensions_AsList_Returns_Object_As_List()
        {
            var obj = new Energistics.DataAccess.WITSML131.Log();
            var container = obj.AsList();
            Assert.IsNotNull(container);
            Assert.AreEqual(1, container.Count);
        }

        [TestMethod]
        public void Extensions_IndexFromScale_Returns_The_Nullable_Double_Index_Value_For_Long_Index()
        {
            var index = 1000L;
            var value = index.IndexFromScale(3, true) as double?;
            Assert.IsTrue(value.HasValue);
            Assert.AreEqual(1000.0, value.Value);

            value = index.IndexFromScale(3);

            Assert.IsTrue(value.HasValue);
            Assert.AreEqual(1.0, value.Value);
        }

        [TestMethod]
        public void Extensions_IndexFromScale_Returns_Nullable_Double_Value_For_Nullable_Long_Index()
        {
            // Nullable long index
            long? index = null;
            var value = index.IndexFromScale(3, true);
            Assert.IsNull(value);

            // Non null index time log
            index = 1000L;
            value = index.IndexFromScale(3, true);
            Assert.AreEqual(1000.0, value);

            // Non null index depth log
            value = index.IndexFromScale(3);
            Assert.AreEqual(1.0, value);
        }

        [TestMethod]
        public void Extensions_IndexFromScale_Returns_The_Double_Index_Value_For_Long_Index()
        {
            var index = 1000L;
            var value = index.IndexFromScale(3, true);
            Assert.AreEqual(1000.0, value);

            value = index.IndexFromScale(3);
            Assert.AreEqual(1.0, value);
        }

        [TestMethod]
        public void Extensions_IndexFromScale_Returns_The_Nullable_Double_Value_For_Long_Index()
        {
            var index = 1000L;
            var value = index.IndexFromScale(3, true) as double?;
            Assert.IsTrue(value.HasValue);
            Assert.AreEqual(1000.0, value.Value);

            value = index.IndexFromScale(3);

            Assert.IsTrue(value.HasValue);
            Assert.AreEqual(1.0, value.Value);
        }

        [TestMethod]
        public void Extensions_IndexToScale_Returns_The_Nullable_Long_Index_For_Nullable_Double_Value()
        {
            // Nullable double value
            double? value = null;
            long? index = value.IndexToScale(3, true);
            Assert.IsNull(index);

            // Non null index time log
            value = 1L;
            index = value.IndexToScale(3, true);
            Assert.AreEqual(1L, index);

            // Non null index depth log
            index = value.IndexToScale(3);
            Assert.AreEqual(1000L, index);
        }

        [TestMethod]
        public void Extensions_IndexToScale_Returns_The_Long_Index_For_Double_Value()
        {
            // Double value
            var value = 1.0;
            var index = value.IndexToScale(3, true);
            Assert.IsNotNull(index);
            Assert.AreEqual(1L, index);

            // Non null index depth log
            index = value.IndexToScale(3);
            Assert.AreEqual(1000L, index);
        }

        [TestMethod]
        public void Extensions_ToOffsetTime_Returns_DateTimeOffset()
        {
            Timestamp? ts = null;
            var timeSpan = new TimeSpan(6, 0, 0);

            // Null timespan and null timestamp
            var result = ts.ToOffsetTime(new TimeSpan?());
            Assert.IsNull(result);
            Assert.IsTrue(!result.HasValue);

            // Null timestamp and valid timespan
            result = ts.ToOffsetTime(timeSpan);
            Assert.IsNull(result);
            Assert.IsTrue(!result.HasValue);

            // Valid timestamp and null timespan
            var dto = new DateTimeOffset(2016, 1, 2, 3, 4, 5, 6, timeSpan);
            ts = new Timestamp(dto);
            result = ts.ToOffsetTime(new TimeSpan?());
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(2016, result.Value.Year);
            Assert.AreEqual(1, result.Value.Month);
            Assert.AreEqual(2, result.Value.Day);
            Assert.AreEqual(3, result.Value.Hour);
            Assert.AreEqual(4, result.Value.Minute);
            Assert.AreEqual(5, result.Value.Second);
            Assert.AreEqual(6, result.Value.Millisecond);
            Assert.AreEqual(timeSpan, result.Value.Offset);

            // Valid timestamp and valid timespan
            ts = new Timestamp(dto);
            result = ts.ToOffsetTime(timeSpan);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(2016, result.Value.Year);
            Assert.AreEqual(1, result.Value.Month);
            Assert.AreEqual(2, result.Value.Day);
            Assert.AreEqual(3, result.Value.Hour);
            Assert.AreEqual(4, result.Value.Minute);
            Assert.AreEqual(5, result.Value.Second);
            Assert.AreEqual(6, result.Value.Millisecond);
            Assert.AreEqual(timeSpan, result.Value.Offset);
        }

        [TestMethod]
        public void Extensions_ToUnixTimeMicroseconds_Returns_Unix_Time_Value_Of_Timestamp()
        {
            var timeSpan = new TimeSpan(6, 0, 0);
            var dto = new DateTimeOffset(2016, 1, 1, 0, 0, 0, 1, timeSpan);
            var ts = new Timestamp(dto);
            var result = ts.ToUnixTimeMicroseconds();

            Assert.IsNotNull(result);
            Assert.AreEqual(1451584800001000, result);
        }

        [TestMethod]
        public void Extensions_ToUnixTimeMicroseconds_Returns_Unix_Time_Value_Of_Nullable_Timestamp()
        {
            var timeSpan = new TimeSpan(6, 0, 0);
            var dto = new DateTimeOffset(2016, 1, 1, 0, 0, 0, 1, timeSpan);
            var ts = new Timestamp?();
            var result = ts.ToUnixTimeMicroseconds();
            Assert.IsNull(result);
            Assert.IsTrue(!result.HasValue);

            ts = new Timestamp(dto);
            result = ts.ToUnixTimeMicroseconds();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(1451584800001000, result.Value);
        }

        
        [TestMethod]
        public void Extensions_GetNonConformingErrorCode_Returns_Errorcode()
        {
            var result = Functions.UpdateInStore.GetNonConformingErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.UpdateTemplateNonConforming, result);

            result = Functions.AddToStore.GetNonConformingErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.InputTemplateNonConforming, result);

            result = Functions.DeleteFromStore.GetNonConformingErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.InputTemplateNonConforming, result);

            result = Functions.GetFromStore.GetNonConformingErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.InputTemplateNonConforming, result);
        }

        [TestMethod]
        public void Extensions_GetMissingElementUidErrorCode_Returns_Errorcode()
        {
            var result = Functions.UpdateInStore.GetMissingElementUidErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.MissingElementUidForUpdate, result);

            result = Functions.AddToStore.GetMissingElementUidErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.MissingElementUidForAdd, result);

            result = Functions.DeleteFromStore.GetMissingElementUidErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.MissingElementUidForDelete, result);
        }

        [TestMethod]
        public void Extensions_GetMissingUomValueErrorCode_Returns_Errorcode()
        {
            var result = Functions.UpdateInStore.GetMissingUomValueErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.MissingUnitForMeasureData, result);

            result = Functions.AddToStore.GetMissingUomValueErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.MissingUnitForMeasureData, result);

            result = Functions.DeleteFromStore.GetMissingUomValueErrorCode();
            Assert.IsNotNull(result);
            Assert.AreEqual(ErrorCodes.EmptyUomSpecified, result);
        }
    }
}
