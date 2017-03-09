//----------------------------------------------------------------------- 
// PDS.Witsml, 2017.1
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

using Energistics.DataAccess.WITSML200.ReferenceData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml
{
    [TestClass]
    public class DataObjectTests
    {
        [TestMethod, Description("Tests that the simpleTypes with a union are generated as enum.")]
        public void UnionTypes_Are_Generated_As_Enum()
        {
            Assert.IsTrue(typeof(LithologyQualifierKind).IsEnum);
            Assert.IsTrue(typeof(EquipmentType).IsEnum);
            Assert.IsTrue(typeof(UnitOfMeasure).IsEnum);
            Assert.IsTrue(typeof(ReferenceCondition).IsEnum);
            Assert.IsTrue(typeof(LithologyKind).IsEnum);
        }
    }
}
