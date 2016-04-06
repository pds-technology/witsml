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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDS.Framework;
using PDS.Witsml.Server.Data;

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
            const string expected = null;

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

        [TestMethod]
        public void PostProcess_remove_empty_elements_and_nil_attributes()
        {
            var input = "<?xml version=\"1.0\"?>" + Environment.NewLine +
                "<logs xmlns:gml=\"http://www.opengis.net/gml/3.2\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                " xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:dc=\"http://purl.org/dc/terms/\" version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\">" + Environment.NewLine +
                    "<log>" + Environment.NewLine +
                        "<nameWell>Well 01 - 160406-074115-107</nameWell>" + Environment.NewLine +
                        "<name>Log 01 - 160406-074117-259</name>" + Environment.NewLine +
                        "<indexType xsi:nil=\"true\" />" + Environment.NewLine +
                        "<startIndex uom=\"m\">0</startIndex>" + Environment.NewLine +
                    "</log>" + Environment.NewLine +
                "</logs>";
            var output = input.PostProcess();
            var root = WitsmlParser.Parse(output).Root;
            var logElement = root.Elements().FirstOrDefault();
            var exist = logElement.Elements().Any(e => e.Name.LocalName == "indexType");
            Assert.IsFalse(exist);
        }
    }
}
