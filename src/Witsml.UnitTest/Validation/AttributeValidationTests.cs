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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PDS.Witsml.Validation
{
    /// <summary>
    /// Testing class for custom validation attributes
    /// </summary>
    [TestClass]
    public class AttributeValidationTests
    {
        /// <summary>
        /// Test <see cref="ObjectAttribute"/> validation attribute for nested properties
        /// </summary>
        [TestMethod]
        public void Test_Object_Validation_Attributes()
        {
            var dtoWell = new DtoWell
            {
                Name = "Well-1",
                Elevation = new Elevation(),
                WellDatum = new List<WellDatum> { new WellDatum { Name = "Sea Level", Code = "SL", Uid = "SL" } }
            };

            IList<ValidationResult> results;
            var valid = EntityValidator.TryValidate(dtoWell, out results);

            Assert.IsFalse(valid);
        }

        /// <summary>
        /// Test <see cref="CollectionAttribute"/> validation attribute for collection property
        /// </summary>
        [TestMethod]
        public void Test_Collection_Validation_Attributes()
        {
            var dtoWell = new DtoWell
            {
                Name = "Well-1",
                Elevation = new Elevation { Uom = "m" },
                WellDatum = new List<WellDatum> { new WellDatum { Name = "Sea Level", Code = "SL" } }
            };

            IList<ValidationResult> results;
            var valid = EntityValidator.TryValidate(dtoWell, out results);

            Assert.IsFalse(valid);
        }

        /// <summary>
        /// Test <see cref="CollectionAttribute"/> validation attribute for Uid uniqueness of recurring elements
        /// </summary>
        [TestMethod]
        public void Test_Recurring_Elements_Validation()
        {
            var dtoWell = new DtoWell
            {
                Name = "Well-1",
                Elevation = new Elevation { Uom = "m" },
                WellDatum = new List<WellDatum>
                {
                    new WellDatum { Name = "Sea Level", Code = "SL", Uid = "SL" },
                    new WellDatum { Name = "Sea Level", Code = "SL", Uid = "SL" }
                }
            };

            IList<ValidationResult> results;
            var valid = EntityValidator.TryValidate(dtoWell, out results);

            Assert.IsFalse(valid);
        }
    }
}
