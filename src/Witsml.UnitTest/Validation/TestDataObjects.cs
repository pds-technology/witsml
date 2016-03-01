using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
    public class WellDatum
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
