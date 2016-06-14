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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;

namespace PDS.Witsml
{
    [TestClass]
    public class ExtensionsTests
    {
        [TestMethod]
        public void GetDescription_returns_correct_description_for_error_code()
        {
            const string expected = "Function completed successfully";

            var actual = ErrorCodes.Success.GetDescription();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetDescription_returns_null_for_unset_error_code()
        {
            const string expected = "Unset";

            var actual = ErrorCodes.Unset.GetDescription();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Functions_GetDescription_returns_DescriptionAttribute_value()
        {
            var expected = "Get From Store";
            var actual = Functions.GetFromStore.GetDescription();

            Assert.AreEqual(expected, actual);
        }
    }
}
