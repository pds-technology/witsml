using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml
{
    [TestClass]
    public class ErrorCodesTests
    {
        [TestMethod]
        public void ErrorCodesTests_Validate_errorCode_with_Resources()
        {
            var errorCodes = Enum.GetNames(typeof(ErrorCodes));
            Properties.Resources.Culture = null;

            foreach (var errorCode in errorCodes)
            {
                var property = typeof(Properties.Resources).GetProperty(errorCode, BindingFlags.Static | BindingFlags.NonPublic);
                var errorCodeDescription = property?.GetValue(null);
                ErrorCodes errorCodeEnum;
                Enum.TryParse(errorCode, out errorCodeEnum);
                Assert.IsNotNull(errorCodeEnum);
                Assert.AreEqual(errorCodeEnum.GetDescription(), errorCodeDescription);
            }
        }
    }
}
