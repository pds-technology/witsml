//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.2
//
// Copyright 2017 PDS Americas LLC
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
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.WITSMLstudio.Framework;
using Shouldly;

namespace PDS.WITSMLstudio
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

            Type t = null;
            Assert.IsFalse(t.IsNumeric());

            Assert.IsFalse(typeof(DateTime).IsNumeric());
            Assert.IsFalse(typeof(object).IsNumeric());

            Assert.IsTrue(typeof(int?).IsNumeric());
        }

        [TestMethod]
        public void FrameworkExtensions_GetAssemblyVersion_Returns_Version()
        {
            var log131 = new Log();
            var result = log131.GetType().GetAssemblyVersion();

            Assert.AreEqual("1.0.0.0", result);

            result = log131.GetType().GetAssemblyVersion(1);

            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void FrameworkExtensions_NotNull_Returns_Exception_If_Object_Is_Null()
        {
            object obj = null;
            Should.Throw<ArgumentNullException>(() =>
            {
                obj.NotNull("test");
            });

            obj = 0;
            Should.NotThrow(() =>
            {
                obj.NotNull("test");
            });
        }

        [TestMethod]
        public void FrameworkExtensions_ContainsIgnoreCase_Returns_Bool_If_List_Contains_String()
        {
            var list = new List<string>() {"AbC", "deF", "gHI"};
            Assert.IsFalse(list.ContainsIgnoreCase("test"));
            Assert.IsTrue(list.ContainsIgnoreCase("def"));
        }

        [TestMethod]
        public void FrameworkExtensions_ContainsIgnoreCase_Returns_Bool_If_String_Contains_String()
        {
            var source = "before_deF_after";
            Assert.IsFalse(source.ContainsIgnoreCase("test"));
            Assert.IsTrue(source.ContainsIgnoreCase("def"));
        }

        [TestMethod]
        public void FrameworkExtensions_TrimTrailingZeros_Removes_Trailing_Zeros_And_Whitespace()
        {
            var expected = "999.25";
            var source = "    999.250000    ";
            Assert.AreEqual(expected, source.TrimTrailingZeros());
        }

        [TestMethod]
        public void FrameworkExtensions_ToCamelCase_Converts_String_To_CamelCase()
        {
            var word = string.Empty;
            var expected = "mudLog";
            var result = word.ToCamelCase();

            Assert.IsTrue(string.IsNullOrWhiteSpace(result));

            word = "MudLog";
            result = word.ToCamelCase();

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void FrameworkExtensions_ToPascalCase_Converts_String_To_PascalCase()
        {
            var word = string.Empty;
            var expected = "MudLog";
            var result = word.ToPascalCase();

            Assert.IsTrue(string.IsNullOrWhiteSpace(result));

            word = "mudLog";
            result = word.ToPascalCase();

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void FrameworkExtensions_ForEach_Performs_Action_On_Each_Item()
        {
            var units = new List<Enum>()
            {
                UnitOfMeasure.m,
                Functions.GetFromStore
            };

            var results = new List<string>();

            units.ForEach(x => results.Add(x.GetDescription()));

            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public void FrameworkExtensions_ForEach_With_Index_Performs_Action_On_Each_Item()
        {
            var results = new List<string>();
            var units = new List<string>()
            {
                "first",
                "second",
                "third"
            };

            units.ForEach((x, i) => results.Add($"{i} - {units[i]}"));

            Assert.IsNotNull(results);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("0 - first", results[0]);
            Assert.AreEqual("1 - second", results[1]);
            Assert.AreEqual("2 - third", results[2]);
        }

        [TestMethod]
        public void FrameworkExtensions_Encrypt_Returns_Encrypted_String()
        {
            string value = null;

            // Null encrypt and decrypt
            var result = value.Encrypt();

            Assert.IsNull(result);

            result = value.Decrypt();

            Assert.IsNull(result);

            // Use default encryption key
            value = "plaintext";
            result = value.Encrypt();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(value, result);

            var decrypted = result.Decrypt();

            Assert.IsNotNull(decrypted);
            Assert.AreEqual(value, decrypted);

            // Use custom encryption key
            var key = "password";
            result = value.Encrypt(key);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(value, result);

            decrypted = result.Decrypt(key);

            Assert.IsNotNull(decrypted);
            Assert.AreEqual(value, decrypted);
        }

        [TestMethod]
        public void FrameworkExtensions_ToSecureString_Returns_Secure_String()
        {
            var word = "securestring";
            var result = word.ToSecureString();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(word, result);
        }

        [TestMethod]
        public void FrameworkExtensions_GetBaseException_Returns_The_Base_Exception()
        {
            var ex = new NullReferenceException();

            var witsmlEx = ex.GetBaseException<Exception>();

            Assert.IsNotNull(witsmlEx);

            witsmlEx = ex.GetBaseException<WitsmlException>();

            Assert.IsNull(witsmlEx);

            ex = new NullReferenceException("null", new Exception("innerEx"));
            witsmlEx = ex.GetBaseException<WitsmlException>();

            Assert.IsNull(witsmlEx);

            ex = new NullReferenceException("null", new WitsmlException(ErrorCodes.ApiVersionNotMatch));
            witsmlEx = ex.GetBaseException<WitsmlException>();

            Assert.IsNotNull(witsmlEx);
        }
    }
}
