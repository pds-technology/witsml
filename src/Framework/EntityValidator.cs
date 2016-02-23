using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Framework
{
    /// <summary>
    /// Defines a helper method that can be used to validate objects using data annotation attributes.
    /// </summary>
    public static class EntityValidator
    {
        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="results">The validation results.</param>
        /// <returns>true if the object is valid; otherwise, false</returns>
        public static bool TryValidate(object instance, out IList<ValidationResult> results)
        {
            var context = new ValidationContext(instance, serviceProvider: null, items: null);
            results = new List<ValidationResult>();

            return Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        }
    }
}
