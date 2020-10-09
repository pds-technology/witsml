//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Energistics.DataAccess.Validation;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;
using PDS.WITSMLstudio.Store.Configuration;
using PDS.WITSMLstudio.Store.Data.Common;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Defines common validation functionality for WITSML data objects.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigator{DataObjectValidationContext}" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IDataObjectValidator{T}" />
    /// <seealso cref="System.ComponentModel.DataAnnotations.IValidatableObject" />
    public abstract class DataObjectValidator<T> : DataObjectNavigator<DataObjectValidationContext<T>>,  IDataObjectValidator<T>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectValidator{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        protected DataObjectValidator(IContainer container) : base(container, new DataObjectValidationContext<T>())
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
        /// <exception cref="PDS.WITSMLstudio.WitsmlException">If any validation errors are detected.</exception>
        public void Validate(Functions function, WitsmlQueryParser parser, T dataObject)
        {
            Logger.DebugFormat("Validating data object for {0}; Type: {1}", function, typeof(T).FullName);

            Context.Function = function;
            DataObject = dataObject;
            Parser = parser;
            ConfigureContext();

            IList<ValidationResult> results;
            DataObjectValidator.TryValidate(this, out results);
            WitsmlValidator.ValidateResults(function, results);

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
            Context.Function = function;

            if (function == Functions.DeleteFromStore && !root.HasElements)
                return root;

            if (function == Functions.AddToStore || function == Functions.UpdateInStore || function == Functions.DeleteFromStore || function == Functions.PutObject)
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

                case Functions.PutObject:
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
            }
        }

        /// <summary>
        /// Navigates the element.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <exception cref="WitsmlException"></exception>
        protected override void NavigateElementType(PropertyInfo propertyInfo, Type elementType, XElement element, string propertyPath)
        {
            if (Context.Function == Functions.DeleteFromStore && !HasUidProperty(elementType) && HasSimpleContent(elementType) && !element.HasElements && HasAttributesWithValues(element))
                throw new WitsmlException(ErrorCodes.ErrorDeletingSimpleContent);

            base.NavigateElementType(propertyInfo, elementType, element, propertyPath);
        }


        /// <summary>
        /// Handles the NaN value during parse navigation by removing NaN values.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            if (Context.RemoveNaNElements)
                Remove(xmlObject);
        }

        /// <summary>
        /// Handles the null value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNullValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            var isRequired = IsRequired(propertyInfo);

            // DeleteFromStore validation [-420]
            // Check Delete of non-recurring, required element or attribute
            if (Context.Function == Functions.DeleteFromStore && isRequired)
            {
                throw new WitsmlException(ErrorCodes.EmptyMandatoryNodeSpecified);
            }

            if (IsComplexType(propertyType) && !HasSimpleContent(propertyType))
            {
                // DeleteFromStore validation [-419]
                // Check Delete of non-recurring container element
                if (Context.Function == Functions.DeleteFromStore)
                {
                    if (!HasUidProperty(propertyType))
                        throw new WitsmlException(ErrorCodes.EmptyNonRecurringElementSpecified);
                }
            }

            base.HandleNullValue(propertyInfo, xmlObject, propertyType, propertyPath, propertyValue);
        }

        /// <summary>
        /// Handles the special case.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elementList">The element list.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="elementName">Name of the element.</param>
        /// <returns>true if the special case was handled, false otherwise.</returns>
        protected override bool HandleSpecialCase(PropertyInfo propertyInfo, List<XElement> elementList, string parentPath, string elementName)
        {
            if ((Context.Function != Functions.AddToStore && Context.Function != Functions.PutObject) || !IsSpecialCase(propertyInfo))
            {
                return base.HandleSpecialCase(propertyInfo, elementList, parentPath, elementName);
            }

            // If AddToStore && IsSpecialCase
            var propertyType = propertyInfo.PropertyType;

            var args = propertyType.GetGenericArguments();
            var childType = args.FirstOrDefault() ?? propertyType.GetElementType();

            if (IsComplexType(childType))
            {
                try
                {
                    var version = ObjectTypes.GetVersion(childType);
                    var family = ObjectTypes.GetFamily(childType);
                    var validator = Container.Resolve<IRecurringElementValidator>(new ObjectName(childType.Name, family, version));
                    validator?.Validate(Context.Function, childType, null, elementList);
                }
                catch (ContainerException)
                {
                    Logger.DebugFormat("{0} not configured for type: {1}", typeof(IRecurringElementValidator).Name, childType);
                }
            }

            return true;
        }

        /// <summary>
        /// Validates the data object properties using .NET validation attributes.
        /// </summary>
        /// <returns>A collection of validation results.</returns>
        protected virtual IEnumerable<ValidationResult> ValidateProperties()
        {
            IList<ValidationResult> results;
            DataObjectValidator.TryValidate(DataObject, out results);
            WitsmlValidator.ValidateResults(Context.Function, results);
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
        /// Initializes the recurring element handler.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void InitializeRecurringElementHandler(PropertyInfo propertyInfo, string propertyPath)
        {
            if (Context.Function != Functions.GetFromStore)
            {
                var propertyType = propertyInfo.PropertyType;
                var args = propertyType.GetGenericArguments();

                if (!args.Any() && !propertyType.IsArray && !typeof(IList).IsAssignableFrom(propertyType))
                {
                    throw new WitsmlException(ErrorCodes.InputTemplateNonConforming);
                }
            }

            base.InitializeRecurringElementHandler(propertyInfo, propertyPath);
        }

        /// <summary>
        /// Navigates the recurring elements with validation for Uids (MissingElementUid).
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void NavigateRecurringElements(PropertyInfo propertyInfo, List<XElement> elements, Type childType, string propertyPath)
        {
            var elementIds = new List<string>();

            foreach (var element in elements)
            {
                if (HasUidProperty(childType))
                {
                    var uidValue = GetAndValidateArrayElementUid(element);
                    if (string.IsNullOrWhiteSpace(uidValue))
                        continue;

                    elementIds.Add(uidValue);
                    NavigateElementType(propertyInfo, childType, element, propertyPath);
                }
                else
                    NavigateElementType(propertyInfo, childType, element, propertyPath);
            }

            // Look for duplicate uids
            var duplicateKeys = elementIds
                .GroupBy(x => x)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            if (duplicateKeys.Any())
            {
                throw new WitsmlException(ErrorCodes.ChildUidNotUnique);
            }
        }

        /// <summary>
        /// Navigates the array element with validation for Uid (MissingElementUid).  
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="elements">The elements.</param>
        /// <param name="childType">Type of the child.</param>
        /// <param name="element">The element.</param>
        /// <param name="propertyPath">The property path.</param>
        protected override void NavigateArrayElementType(PropertyInfo propertyInfo, List<XElement> elements, Type childType, XElement element, string propertyPath)
        {
            if (!HasUidProperty(childType) || !string.IsNullOrWhiteSpace(GetAndValidateArrayElementUid(element)))
                base.NavigateArrayElementType(propertyInfo, elements, childType, element, propertyPath);
        }

        /// <summary>
        /// Navigates the uom attribute.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="measureValue">The measure value.</param>
        /// <param name="uomValue">The uom value.</param>
        protected override void NavigateUomAttribute(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath,
            string measureValue, string uomValue)
        {
            // client MUST NOT [else error -417] specify an empty unit of measure (uom) attribute during a DeleteFromStore.
            if (Context.Function == Functions.DeleteFromStore && xmlObject != null && string.IsNullOrWhiteSpace(uomValue))
            {
                throw new WitsmlException(ErrorCodes.EmptyUomSpecified);
            }

            if (Context.Function != Functions.DeleteFromStore)
            {
                base.NavigateUomAttribute(propertyInfo, xmlObject, propertyType, propertyPath, measureValue, uomValue);
            }
        }

        /// <summary>
        /// Validate the uid attribute value of the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The value of the uid attribute.</returns>
        /// <exception cref="WitsmlException">
        /// </exception>
        protected virtual string GetAndValidateArrayElementUid(XElement element)
        {
            var uidAttribute = element.Attributes().FirstOrDefault(a => a.Name == "uid");

            if (string.IsNullOrEmpty(uidAttribute?.Value))
            {
                if (uidAttribute != null && Context.Function == Functions.DeleteFromStore)
                    throw new WitsmlException(ErrorCodes.EmptyUidSpecified);

                throw new WitsmlException(Context.Function.GetMissingElementUidErrorCode());
            }

            return uidAttribute.Value;
        }
    }
}
