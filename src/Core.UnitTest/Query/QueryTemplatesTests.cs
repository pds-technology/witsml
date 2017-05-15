//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.1
//
// Copyright 2017 Petrotechnical Data Systems
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
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.WITSMLstudio.Query
{
    [TestClass]
    public class QueryTemplatesTests
    {
        [TestMethod]
        public void QueryTemplatesTests_Create_Header_Only_Template_For_Log_131()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.Log, OptionsIn.DataVersion.Version131.Value, OptionsIn.ReturnElements.HeaderOnly);

            var xml = template.ToString();
            Console.WriteLine(xml);

            Assert.IsFalse(xml.Contains("logData"));
        }

        [TestMethod]
        public void QueryTemplatesTests_Create_Header_Only_Template_For_Log_141()
        {
            var template = QueryTemplates.GetTemplate(ObjectTypes.Log, OptionsIn.DataVersion.Version141.Value, OptionsIn.ReturnElements.HeaderOnly);

            var xml = template.ToString();
            Console.WriteLine(xml);

            Assert.IsFalse(xml.Contains("logData"));
        }
    }
}
