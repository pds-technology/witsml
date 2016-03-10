﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Energistics.DataAccess.Validation;
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
        /// Gets the WITSML API method being executed.
        /// </summary>
        /// <value>The function.</value>
        public Functions Function { get; private set; }

        /// <summary>
        /// Validates the specified data object while executing a WITSML API method.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="dataObject">The data object.</param>
        /// <exception cref="PDS.Witsml.WitsmlException">If any validation errors are detected.</exception>
        public void Validate(Functions function, T dataObject)
        {
            DataObject = dataObject;
            Function = function;

            IList<ValidationResult> results;
            DataObjectValidator.TryValidate(this, out results);

            if (results.Any())
            {
                var errorCode = ErrorCodes.Unset;
                var witsmlValidationResult = results.FirstOrDefault(r => r.GetType() == typeof(WitsmlValidationResult)) as WitsmlValidationResult;
                if (witsmlValidationResult != null)
                    Enum.TryParse(witsmlValidationResult.ErrorCode.ToString(), out errorCode);
                else
                    Enum.TryParse(results.First().ErrorMessage, out errorCode);
                throw new WitsmlException(errorCode);
            }
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
                    foreach (var result in ValidateProperties().Union(ValidateForInsert()))
                        yield return result;
                    break;

                case Functions.PutObject:
                    foreach (var result in ValidateProperties().Union(ValidateForPutObject()))
                        yield return result;
                    break;
            }
        }

        /// <summary>
        /// Validates the data object properties using .NET validation attributes.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateProperties()
        {
            IList<ValidationResult> results;

            // Validate object properties
            if (!DataObjectValidator.TryValidate(DataObject, out results))
            {
                throw new WitsmlException(ErrorCodes.InputTemplateNonConforming,
                    string.Join("; ", results.Select(x => x.ErrorMessage)));
            }

            yield break;
        }

        /// <summary>
        /// Validates the data object while executing AddToStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateForInsert()
        {
            yield break;
        }

        /// <summary>
        /// Validates the data object while executing PutObject.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateForPutObject()
        {
            yield break;
        }
    }
}
