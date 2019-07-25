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

using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using UnitOfMeasureExt = Energistics.DataAccess.ExtensibleEnum<Energistics.DataAccess.WITSML200.ReferenceData.UnitOfMeasure>;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Units tests.
    /// </summary>
    [TestClass]
    public class UnitsTests
    {
        [TestMethod]
        public void Extensions_GetUnit_Returns_OriginalUnit()
        {
            var uom = string.Empty;
            var result = Units.GetUnit(uom);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            Assert.AreEqual(Units.None, result);

            uom = "m";
            result = Units.GetUnit(uom);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            Assert.AreEqual(uom, result);
        }

        [TestMethod]
        public void Extensions_GetUnit_Returns_Original_UnitOfMeasure()
        {
            var nullUom = default(UnitOfMeasureExt?);
            var result = Units.GetUnit(nullUom);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            Assert.AreEqual(Units.None, result);

            var uom = UnitOfMeasure.mh;
            result = Units.GetUnit(uom);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
            Assert.AreEqual("m/h", result);
        }

        [TestMethod]
        public void Extensions_GetUnitOfMeasure_Returns_UnitOfMeasure()
        {
            var uom = string.Empty;
            var result = Units.GetUnitOfMeasure(uom);
            Assert.IsNull(result);

            uom = "m";
            result = Units.GetUnitOfMeasure(uom);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.IsEnum);
            Assert.IsTrue(result.Equals(UnitOfMeasure.m));
        }
    }
}
