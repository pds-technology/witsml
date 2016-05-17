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
    public class FrameworkExtensionsTests
    {
        [TestMethod]
        public void FrameworkExtensions_Type_Is_Numeric()
        {
            Assert.IsTrue(typeof(sbyte).IsNumeric());
            Assert.IsTrue(typeof(byte).IsNumeric());
            Assert.IsTrue(typeof(short).IsNumeric());
            Assert.IsTrue(typeof(ushort).IsNumeric());
            Assert.IsTrue(typeof(int).IsNumeric());
            Assert.IsTrue(typeof(uint).IsNumeric());
            Assert.IsTrue(typeof(long).IsNumeric());
            Assert.IsTrue(typeof(ulong).IsNumeric());
            Assert.IsTrue(typeof(float).IsNumeric());          
            Assert.IsTrue(typeof(double).IsNumeric());
            Assert.IsTrue(typeof(decimal).IsNumeric());

            Assert.IsFalse((typeof(string).IsNumeric()));
        }
    }
}
