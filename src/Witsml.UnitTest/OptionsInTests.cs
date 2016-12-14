//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
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

namespace PDS.Witsml
{
    /// <summary>
    /// OptionsIn tests.
    /// </summary>
    [TestClass]
    public class OptionsInTests
    {
        [TestMethod]
        public void OptionsIn_Parse_returns_empty_dictionary()
        {
            var parseNull = OptionsIn.Parse(null);
            Assert.IsNotNull(parseNull);
            Assert.IsFalse(parseNull.Any());

            var parseEmpty = OptionsIn.Parse(string.Empty);
            Assert.IsNotNull(parseEmpty);
            Assert.IsFalse(parseEmpty.Any());
        }

        [TestMethod]
        public void OptionsIn_Parse_returns_single_item()
        {
            var actual = OptionsIn.Parse("returnElements=all");
            Assert.IsNotNull(actual);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual("returnElements", actual.Keys.Single());
            Assert.AreEqual("all", actual.Values.Single());
        }

        [TestMethod]
        public void OptionsIn_GetValue_returns_default_value()
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
        public void OptionsIn_Join_concatenates_multiple_options_into_single_string()
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
        }

        [TestMethod]
        public void OptionsIn_string_operator_performs_implicit_conversion()
        {
            OptionsIn nullOption = null;
            string nullString = nullOption;
            Assert.IsNull(nullString);

            OptionsIn allOption = OptionsIn.ReturnElements.All;
            string allValue = allOption;
            Assert.AreEqual("returnElements=all", allValue);
        }

        [TestMethod]
        public void OptionsIn_dictionary_operator_performs_implicit_conversion()
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
