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
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml
{
    [TestClass]
    public class ErrorCodesTests
    {
        [TestMethod]
        public void ErrorCodes_GetDescription_Validate_errorCode_with_Resources()
        {
            var errorCodes = Enum.GetNames(typeof(ErrorCodes));
            Properties.Resources.Culture = null;

            foreach (var errorCode in errorCodes)
            {
                var property = typeof(Properties.Resources).GetProperty(errorCode, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                Assert.IsNotNull(property, errorCode);
                var errorCodeDescription = property.GetValue(null);
                ErrorCodes errorCodeEnum;
                Enum.TryParse(errorCode, out errorCodeEnum);
                Assert.IsNotNull(errorCodeEnum, errorCode);
                Assert.AreEqual(errorCodeEnum.GetDescription(), errorCodeDescription);
            }
        }
    }
}
