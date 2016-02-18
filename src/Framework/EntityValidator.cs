using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Framework
{
    public static class EntityValidator
    {
        public static bool TryValidate(object dataObject, out ICollection<ValidationResult> results)
        {
            var context = new ValidationContext(dataObject, serviceProvider: null, items: null);
            results = new List<ValidationResult>();

            return Validator.TryValidateObject(dataObject, context, results, validateAllProperties: true);
        }
    }
}
