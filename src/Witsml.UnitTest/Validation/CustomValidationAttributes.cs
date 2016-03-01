using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PDS.Witsml.Validation
{
    /// <summary>
    /// Custom validation attribute that specifies how a non-primitive property is validated
    /// </summary>
    /// <seealso cref="System.ComponentModel.DataAnnotations.ValidationAttribute" />
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ObjectAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            IList<ValidationResult> results;
            EntityValidator.TryValidate(value, out results);
            return results.FirstOrDefault();
        }
    }


    /// <summary>
    /// Custom validation attribute that specifies how a collection property is validated
    /// </summary>
    /// <seealso cref="System.ComponentModel.DataAnnotations.ValidationAttribute" />
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class CollectionAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var uids = new List<string>();
            var list = (IEnumerable)value;
            foreach (var obj in list)
            {
                IList<ValidationResult> results;
                EntityValidator.TryValidate(obj, out results);
                if (results.Count > 0)
                    return results.FirstOrDefault();

                var pUid = obj.GetType().GetProperties().FirstOrDefault(p => p.Name == "Uid");
                var uid = pUid.GetValue(obj).ToString();
                if (uids.Contains(uid))
                    return new ValidationResult("Uid for recurring element must be unique", new string[] { "Uid" });
                uids.Add(uid);
            }
            return null;
        }
    }

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
