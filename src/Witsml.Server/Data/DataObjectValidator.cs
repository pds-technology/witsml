//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
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
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.Validation;
using PDS.Witsml.Data;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines common validation functionality for WITSML data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.Witsml.Data.DataObjectNavigator{DataObjectValidationContext}" />
    /// <seealso cref="PDS.Witsml.Server.Data.IDataObjectValidator{T}" />
    /// <seealso cref="System.ComponentModel.DataAnnotations.IValidatableObject" />
    public abstract class DataObjectValidator<T> : DataObjectNavigator<DataObjectValidationContext<T>>,  IDataObjectValidator<T>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectValidator{T}"/> class.
        /// </summary>
        protected DataObjectValidator() : base(new DataObjectValidationContext<T>())
        {
        }

        /// <summary>
        /// Gets the data object being validated.
        /// </summary>
        /// <value>The data object.</value>
        public T DataObject { get; private set; }

        /// <summary>
        /// Gets the input template parser.
        /// </summary>
        /// <value>The input template parser.</value>
        public WitsmlQueryParser Parser { get; private set; }

        /// <summary>
        /// Validates the specified data object while executing a WITSML API method.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        /// <exception cref="PDS.Witsml.WitsmlException">If any validation errors are detected.</exception>
        public void Validate(Functions function, WitsmlQueryParser parser, T dataObject)
        {
            Logger.DebugFormat("Validating data object for {0}; Type: {1}", function, typeof(T).FullName);

            Context.Function = function;
            DataObject = dataObject;
            Parser = parser;

            IList<ValidationResult> results;
            DataObjectValidator.TryValidate(this, out results);
            ValidateResults(results);

            WitsmlOperationContext.Current.Warnings.AddRange(Context.Warnings);
        }

        /// <summary>
        /// Parses the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="parser">The input template parser.</param>
        /// <returns>A copy of the parsed element.</returns>
        public XElement Parse(Functions function, WitsmlQueryParser parser)
        {
            var root = new XElement(parser.Root);
            Context.RemoveNaNElements = true;
            if (function == Functions.AddToStore || function == Functions.UpdateInStore)
                Navigate(root.Elements().FirstOrDefault());

            return root;
        }

        /// <summary>
        /// Determines whether the specified object is valid.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection that holds failed-validation information.</returns>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            switch (Context.Function)
            {
                case Functions.GetFromStore:
                    foreach (var result in ValidateForGet())
                        yield return result;
                    break;

                case Functions.AddToStore:
                    foreach (var result in ValidateProperties().Union(ValidateForInsert()))
                        yield return result;
                    break;

                case Functions.UpdateInStore:
                    foreach (var result in ValidateForUpdate())
                        yield return result;
                    break;

                case Functions.DeleteObject:
                case Functions.DeleteFromStore:
                    foreach (var result in ValidateForDelete())
                        yield return result;
                    break;

                case Functions.PutObject:
                    foreach (var result in ValidateForPutObject())
                        yield return result;
                    break;
            }
        }

        /// <summary>
        /// Handles the NaN value during parse navigation by removing NaN values.
        /// </summary>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.RemoveNaNElements)
                Remove(xmlObject);
        }

        /// <summary>
        /// Validates the data object properties using .NET validation attributes.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateProperties()
        {
            IList<ValidationResult> results;
            DataObjectValidator.TryValidate(DataObject, out results);
            ValidateResults(results);
            yield break;
        }

        /// <summary>
        /// Validates the data object while executing GetFromStore
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<ValidationResult> ValidateForGet()
        {
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
        /// Validates the data object while executing UpdateInStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateForUpdate()
        {
            yield break;
        }

        /// <summary>
        /// Validates the data object while executing DeleteFromStore.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateForDelete()
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

        private static void ValidateResults(IList<ValidationResult> results)
        {
            if (!results.Any()) return;

            ErrorCodes errorCode;
            var witsmlValidationResult = results.OfType<WitsmlValidationResult>().FirstOrDefault();

            if (witsmlValidationResult != null)
            {
                throw new WitsmlException((ErrorCodes)witsmlValidationResult.ErrorCode);
            }

            if (Enum.TryParse(results.First().ErrorMessage, out errorCode))
            {
                throw new WitsmlException(errorCode);
            }

            throw new WitsmlException(ErrorCodes.InputTemplateNonConforming,
                string.Join("; ", results.Select(x => x.ErrorMessage)));
        }
    }
}
