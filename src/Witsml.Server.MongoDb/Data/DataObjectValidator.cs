using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PDS.Witsml.Server.Data
{
    public abstract class DataObjectValidator<T> : IValidatableObject
    {
        public T DataObject { get; set; }

        public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
    }
}
