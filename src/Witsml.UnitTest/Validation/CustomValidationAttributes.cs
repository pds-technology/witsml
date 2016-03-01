using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PDS.Framework;

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
            var list = (IEnumerable)value;
            foreach (var obj in list)
            {
                IList<ValidationResult> results;
                EntityValidator.TryValidate(obj, out results);
                if (results.Count > 0)
                    return results.FirstOrDefault();
            }
            return null;
        }
    }
}
