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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;

namespace PDS.Witsml
{
    [TestClass]
    public class DateTimeExtensionsTests
    {
        [TestMethod]
        public void DateTimeExtensions_ToUnixTimeMicrosecods_Converts_From_DateTimeOffset_Correctly()
        {
            var value = DateTimeOffset.Parse("2016-10-19T16:36:55.569Z");
            var expected = 1476895015569000L;
            var actual = value.ToUnixTimeMicroseconds();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DateTimeExtensions_FromUnixTimeMicroseconds_Converts_To_DateTimeOffset_Correctly()
        {
            var value = 1476895015569000L;
            var expected = DateTimeOffset.Parse("2016-10-19T16:36:55.569Z");
            var actual = DateTimeExtensions.FromUnixTimeMicroseconds(value);

            Assert.AreEqual(expected, actual);
        }
    }
}
