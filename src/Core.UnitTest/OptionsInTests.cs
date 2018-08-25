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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// OptionsIn tests.
    /// </summary>
    [TestClass]
    public class OptionsInTests
    {
        [TestMethod]
        public void OptionsIn_Equals_Checks_If_Values_Are_Equal()
        {
            OptionsIn allOption = OptionsIn.ReturnElements.All;
            Assert.IsFalse(OptionsIn.CompressionMethod.None.Equals(allOption.Value));
            Assert.IsTrue(OptionsIn.ReturnElements.All.Equals(allOption.Value));            
        }

        [TestMethod]
        public void OptionsIn_Parse_Returns_Empty_Dictionary()
        {
            var parseNull = OptionsIn.Parse(null);
            Assert.IsNotNull(parseNull);
            Assert.IsFalse(parseNull.Any());

            var parseEmpty = OptionsIn.Parse(string.Empty);
            Assert.IsNotNull(parseEmpty);
            Assert.IsFalse(parseEmpty.Any());
        }

        [TestMethod]
        public void OptionsIn_Parse_Returns_Single_Item()
        {
            var actual = OptionsIn.Parse("returnElements=all");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("returnElements", actual.Keys.Single());
            Assert.AreEqual("all", actual.Values.Single());
        }

        [TestMethod]
        public void OptionsIn_Parse_Duplicate_Keys_Returns_Single_Item()
        {
            var actual = OptionsIn.Parse("returnElements=all;returnElements=all");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("returnElements", actual.Keys.Single());
            Assert.AreEqual("all", actual.Values.Single());
        }

        [TestMethod]
        public void OptionsIn_GetValue_Returns_Default_Value()
        {
            var returnElementsAll = OptionsIn.Parse("returnElements=all");
            var nullValue = OptionsIn.GetValue(returnElementsAll, null);
            Assert.IsNull(nullValue);

            var foundValue = OptionsIn.GetValue(returnElementsAll, OptionsIn.ReturnElements.GetValues().Last());
            Assert.IsNotNull(foundValue);
            Assert.AreEqual("all", foundValue);

            var firstValue = OptionsIn.CascadedDelete.GetValues().Last();
            var defaultValue = OptionsIn.GetValue(returnElementsAll, firstValue);
            Assert.IsNotNull(defaultValue);
            Assert.AreEqual(firstValue.Value, defaultValue);
        }

        [TestMethod]
        public void OptionsIn_Join_Concatenates_Multiple_Options_Into_Single_String()
        {
            Assert.AreEqual(
                "maxReturnNodes=1;requestLatestValues=2",
                OptionsIn.Join(OptionsIn.MaxReturnNodes.Eq(1), OptionsIn.RequestLatestValues.Eq(2)));

            Assert.AreEqual(
                "compressionMethod=none;compressionMethod=gzip",
                OptionsIn.Join(OptionsIn.CompressionMethod.GetValues().ToArray<OptionsIn>()));

            Assert.AreEqual(
                "requestObjectSelectionCapability=none;requestObjectSelectionCapability=true",
                OptionsIn.Join(OptionsIn.RequestObjectSelectionCapability.GetValues().ToArray<OptionsIn>()));

            Assert.AreEqual(
                "requestPrivateGroupOnly=false;requestPrivateGroupOnly=true",
                OptionsIn.Join(OptionsIn.RequestPrivateGroupOnly.GetValues().ToArray<OptionsIn>()));

            Assert.AreEqual(
                "dataVersion=1.3.1.1;dataVersion=1.4.1.1;dataVersion=2.0",
                OptionsIn.Join(OptionsIn.DataVersion.Version131, OptionsIn.DataVersion.Version141, OptionsIn.DataVersion.Version200));
        }

        [TestMethod]
        public void OptionsIn_String_Operator_Performs_Implicit_Conversion()
        {
            OptionsIn nullOption = null;
            string nullString = nullOption;
            Assert.IsNull(nullString);

            OptionsIn allOption = OptionsIn.ReturnElements.All;
            string allValue = allOption;
            Assert.AreEqual("returnElements=all", allValue);
        }

        [TestMethod]
        public void OptionsIn_Dictionary_Operator_Performs_Implicit_Conversion()
        {
            OptionsIn allOption = OptionsIn.ReturnElements.All;
            Dictionary<string, string> actual = allOption;

            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(allOption.Key, actual.Keys.Single());
            Assert.AreEqual(allOption.Value, actual.Values.Single());
        }
    }
}
