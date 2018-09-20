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
using System.Linq;
using Energistics.DataAccess.WITSML131;
using Energistics.DataAccess.WITSML131.ComponentSchemas;
using Energistics.DataAccess.WITSML131.ReferenceData;
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
        public void FrameworkExtensions_IsMatch()
        {
            {
                // it is an extension
                Assert.IsTrue("hello".IsMatch(null));
                Assert.IsTrue("hello".IsMatch(""));
                Assert.IsTrue("hello".IsMatch("    "));
                Assert.IsTrue("hello".IsMatch("hello"));
                Assert.IsTrue("Hello".IsMatch("hello"));

                Assert.IsFalse("".IsMatch("*"));
            }
            {
                // it can be used as a static method alright
                Assert.IsTrue(FrameworkExtensions.IsMatch(null, null));
                Assert.IsTrue(FrameworkExtensions.IsMatch("hello", null));
                Assert.IsTrue(FrameworkExtensions.IsMatch("hello", ""));
                Assert.IsTrue(FrameworkExtensions.IsMatch("hello", "    "));
                Assert.IsTrue(FrameworkExtensions.IsMatch("hello", "hello"));

                Assert.IsFalse(FrameworkExtensions.IsMatch("", "*"));
                Assert.IsFalse(FrameworkExtensions.IsMatch(null, "hello"));
            }
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
                Energistics.DataAccess.WITSML200.ReferenceData.UnitOfMeasure.m,
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

        [TestMethod]
        public void FrameworkExtensions_GetPropertyValue_Can_Access_First_List_Element()
        {
            var dataContext = new WellList
            {
                Well = new List<Well>
                {
                    new Well
                    {
                        Uid = Guid.Empty.ToString(),
                        Name = "Well 01",

                        WellLocation = new List<Location>
                        {
                            new Location
                            {
                                Uid = "LatLon",
                                Latitude = new PlaneAngleMeasure(123.456, PlaneAngleUom.dega),
                                Longitude = new PlaneAngleMeasure(987.654, PlaneAngleUom.dega)
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(1, dataContext.GetPropertyValue<int>("Well.Count"));
            Assert.AreEqual(Guid.Empty.ToString(), dataContext.GetPropertyValue<string>("Well.Uid"));
            Assert.AreEqual("Well 01", dataContext.GetPropertyValue<string>("Well.Name"));

            Assert.AreEqual(1, dataContext.GetPropertyValue<int>("Well.WellLocation.Count"));
            Assert.AreEqual(123.456, dataContext.GetPropertyValue<double>("Well.WellLocation.Latitude.Value"));
            Assert.AreEqual(987.654, dataContext.GetPropertyValue<double>("Well.WellLocation.Longitude.Value"));

            Assert.AreEqual(PlaneAngleUom.dega, dataContext.GetPropertyValue<PlaneAngleUom>("Well.WellLocation.Latitude.Uom"));
            Assert.AreEqual(PlaneAngleUom.dega, dataContext.GetPropertyValue<PlaneAngleUom>("Well.WellLocation.Longitude.Uom"));

            // TODO: for future implementation
            //Assert.AreEqual(PlaneAngleUom.dega, dataContext.GetPropertyValue<PlaneAngleUom>("Well.WellLocation[1].Longitude.Uom"));
            //Assert.AreEqual(PlaneAngleUom.dega, dataContext.GetPropertyValue<PlaneAngleUom>("Well.WellLocation[Uid = 'LatLon'].Longitude.Uom"));
        }

        [TestMethod]
        public void FrameworkExtensions_ToDictionaryIgnoreCase_Can_Create_Dictionary_With_Specified_Key_Selector()
        {
            var list = new List<Well> {
                new Well { Name = "well1", Country = "country1" },
                new Well { Name = "well3", Country = "country4" },
                new Well { Name = "WELL1", Country = "COUNTRY4" },
                new Well { Name = "well4", Country = "country4" } 
            };

            var result = list.ToDictionaryIgnoreCase(w => w.Name);

            Assert.AreEqual(3, result.Count);
            var firstRecord = result.First();
            Assert.IsNotNull(firstRecord);
            Assert.AreEqual("well1", firstRecord.Key);
            Assert.AreEqual("country1", firstRecord.Value.Country);

            var lastRecord = result.Last();
            Assert.IsNotNull(lastRecord);
            Assert.AreEqual("well4", result.Last().Key);
            Assert.AreEqual("country4", lastRecord.Value.Country);

            result = list.ToDictionaryIgnoreCase(w => w.Country);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("country1", result.First().Key);

            firstRecord = result.First();
            Assert.IsNotNull(firstRecord);
            Assert.AreEqual("country1", firstRecord.Key);
            Assert.AreEqual("well1", firstRecord.Value.Name);

            lastRecord = result.Last();
            Assert.IsNotNull(lastRecord);
            Assert.AreEqual("country4", lastRecord.Key);
            Assert.AreEqual("well3", lastRecord.Value.Name);
        }

        [TestMethod]
        public void FrameworkExtensions_ToDictionaryIgnoreCase_Can_Create_Dictionary_With_Specified_Key_And_Element_Selector()
        {
            var list = new List<Well> {
                new Well { Name = "well1", Country = "country1" },
                new Well { Name = "well3", Country = "country4" },
                new Well { Name = "WELL1", Country = "COUNTRY4" },
                new Well { Name = "well4", Country = "country4" }
            };

            var result = list.ToDictionaryIgnoreCase(w => w.Name, w2 => new {w2.Country, w2.Name});

            Assert.AreEqual(3, result.Count);
            var firstRecord = result.First();
            Assert.IsNotNull(firstRecord);
            Assert.AreEqual("well1", firstRecord.Key);
            Assert.AreEqual("country1", firstRecord.Value.Country);
            Assert.AreEqual(2, firstRecord.Value.GetType().GetProperties().Length);

            var lastRecord = result.Last();
            Assert.IsNotNull(lastRecord);
            Assert.AreEqual("well4", result.Last().Key);
            Assert.AreEqual("country4", lastRecord.Value.Country);

            var result2 = list.ToDictionaryIgnoreCase(w => w.Country, w2 => new { w2.Country, w2.Name, w2.County});

            Assert.AreEqual(2, result2.Count);
            Assert.AreEqual("country1", result2.First().Key);

            var firstRecord2 = result2.First();
            Assert.IsNotNull(firstRecord);
            Assert.AreEqual("country1", firstRecord2.Key);
            Assert.AreEqual("well1", firstRecord2.Value.Name);
            Assert.IsNull(firstRecord2.Value.County);
            Assert.AreEqual(3, firstRecord2.Value.GetType().GetProperties().Length);

            var lastRecord2 = result2.Last();
            Assert.IsNotNull(lastRecord2);
            Assert.AreEqual("country4", lastRecord2.Key);
            Assert.AreEqual("well3", lastRecord2.Value.Name);
        }

        [TestMethod]
        public void FrameworkExtensions_SplitQuotedString_Returns_String_Array()
        {
            var delimiter = ",";
            var value = "well1,well2,\"well3,a\",\"well4,b\",well5";

            var values = value.SplitQuotedString(delimiter);
            Assert.AreEqual(values[0], "well1");
            Assert.AreEqual(values[1], "well2");
            Assert.AreEqual(values[2], "well3,a");
            Assert.AreEqual(values[3], "well4,b");
            Assert.AreEqual(values[4], "well5");
        }

        [TestMethod]
        public void FrameworkExtensions_JoinQuotedString_Returns_Value()
        {
            var delimiter = ",";
            var testValue = "well1,well2,\"well3,a\",\"well4,b\",well5";
            var values = testValue.SplitQuotedString(",");

            var value = values.JoinQuotedStrings(delimiter);
            Assert.AreEqual(testValue, value);
        }
    }
}
