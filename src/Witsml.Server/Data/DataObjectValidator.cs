using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PDS.Framework;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines common validation functionality for WITSML data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="System.ComponentModel.DataAnnotations.IValidatableObject" />
    public abstract class DataObjectValidator<T> : IDataObjectValidator<T>, IValidatableObject
    {
        /// <summary>
        /// Gets the data object being validated.
        /// </summary>
        /// <value>The data object.</value>
        public T DataObject { get; private set; }

        /// <summary>
        /// Gets the WITSML Store API method being executed.
        /// </summary>
        /// <value>The function.</value>
        public Functions Function { get; private set; }

        /// <summary>
        /// Validates the specified data object while executing a WITSML Store API method.
        /// </summary>
        /// <param name="function">The WITSML Store API method.</param>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A collection of validation results.</returns>
        public IList<ValidationResult> Validate(Functions function, T dataObject)
        {
            DataObject = dataObject;
            Function = function;

            IList<ValidationResult> results;
            EntityValidator.TryValidate(this, out results);
            return results;
        }

        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection that holds failed-validation information.</returns>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            switch (Function)
            {
                case Functions.AddToStore:
                    foreach (var result in ValidateForInsert())
                        yield return result;
                    break;
            }
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateForInsert()
        {
            yield break;
        }
    }
}
