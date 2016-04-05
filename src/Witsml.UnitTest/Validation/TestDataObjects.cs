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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Energistics.DataAccess;

namespace PDS.Witsml.Validation
{
    /// <summary>
    /// Testing data-object that implements properties having custom validation attributes
    /// </summary>
    public class DtoWell
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [Object(ErrorMessage = "Invalid well elevation data")]
        public Elevation Elevation { get; set; }

        [Required]
        [Collection(ErrorMessage = "Well datum has invalid data")]
        public List<WellDatum> WellDatum { get; set; }
    }

    /// <summary>
    /// Property class to test <see cref="ObjectAttribute"/> custom validation attribute
    /// </summary>
    public class Elevation
    {
        [Required]
        public string Uom { get; set; }

        [Required]
        public double Value { get; set; }

        public string Description { get; set; }
    }

    /// <summary>
    /// Property class to test <see cref="CollectionAttribute"/> custom validation attribute
    /// </summary>
    public class WellDatum : IDataObject
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Uid { get; set; }

        [Required]
        public string Code { get; set; }

        public string Description { get; set; }
    }
}
