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
using Energistics.DataAccess.WITSML141;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace PDS.Witsml
{
    [TestClass]
    public class ObjectTypesTests
    {
        private static readonly string _wellsXml =
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?>" + Environment.NewLine +
                "<wells version=\"1.4.1.1\" xmlns=\"http://www.witsml.org/schemas/1series\" />";

        [TestMethod]
        public void ObjectTypes_GetObjectType_returns_valid_witsml_type_for_Well()
        {
            const string expected = ObjectTypes.Well;

            var actual = ObjectTypes.GetObjectType<WellList>();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_returns_valid_witsml_type_for_Wellbore()
        {
            const string expected = ObjectTypes.Wellbore;

            var actual = ObjectTypes.GetObjectType(typeof(WellboreList));

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectType_returns_null_for_invalid_witsml_type()
        {
            Should.Throw<ArgumentException>(() =>
            {
                ObjectTypes.GetObjectType(typeof(DateTime));
            });
        }

        [TestMethod]
        public void ObjectTypes_GetObjectTypeFromGroup_returns_type_for_valid_xml()
        {
            var document = WitsmlParser.Parse(_wellsXml);
            var typeFound = ObjectTypes.GetObjectTypeFromGroup(document.Root);

            Assert.AreEqual(ObjectTypes.Well, typeFound);
        }

        [TestMethod]
        public void ObjectTypes_GetObjectTypeFromGroup_returns_unknown_for_invalid_xml()
        {
            var typeFound = ObjectTypes.GetObjectTypeFromGroup(null);

            Assert.AreEqual(ObjectTypes.Unknown, typeFound);
        }
    }
}
