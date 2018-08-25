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
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Data;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides a mechanism to map WITSML data objects and project specific properties.
    /// </summary>
    /// <typeparam name="T">The data object type.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Data.DataObjectNavigator{T}" />
    public class DataObjectMapper<T> : DataObjectNavigator<DataObjectMappingContext<T>>
    {
        private readonly WitsmlQueryParser _parser;
        private readonly List<string> _fields;
        private bool _isTargetCreated;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataObjectMapper{T}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="parser">The query parser.</param>
        /// <param name="fields">The fields of the data object to be selected.</param>
        /// <param name="ignored">The fields of the data object to be ignored.</param>
        public DataObjectMapper(IContainer container, WitsmlQueryParser parser, List<string> fields, List<string> ignored = null) : base(container, new DataObjectMappingContext<T>())
        {
            Context.Ignored = ignored;
            _parser = parser;
            _fields = fields;
        }

        /// <summary>
        /// Maps the specified data objects.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        /// <returns></returns>
        public List<T> Map(IEnumerable<T> dataObjects)
        {
            return dataObjects.Select(x => Map(x)).ToList();
        }

        /// <summary>
        /// Maps the specified data object.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="target">The target object.</param>
        /// <returns>A new data object instance.</returns>
        public T Map(T source, T target = default(T))
        {
            Context.Source = source;
            Context.Target = target;

            if (Context.Target == null)
            {
                Context.Target = Activator.CreateInstance<T>();
                _isTargetCreated = true;
            }

            if (Context.Properties == null)
            {
                Context.Properties = new List<string>(_fields ?? Enumerable.Empty<string>());
                var element = _parser?.Element();

                // Navigate the root element to map requested properties
                if (element != null)
                {
                    Navigate(element);
                }
            }

            MapProjectedProperties();

            return Context.Target;
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
        protected override void NavigateUomAttribute(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string measureValue, string uomValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the string value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleStringValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the date time value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="dateTimeValue">The date time value.</param>
        protected override void HandleDateTimeValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, DateTime dateTimeValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the timestamp value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="timestampValue">The timestamp value.</param>
        protected override void HandleTimestampValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, Timestamp timestampValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the object value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="objectValue">The object value.</param>
        protected override void HandleObjectValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue, object objectValue)
        {
            AddProjectionProperty(propertyPath);
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
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Handles the NaN value.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="xmlObject">The XML object.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <param name="propertyValue">The property value.</param>
        protected override void HandleNaNValue(PropertyInfo propertyInfo, XObject xmlObject, Type propertyType, string propertyPath, string propertyValue)
        {
            AddProjectionProperty(propertyPath);
        }

        /// <summary>
        /// Adds the projection property.
        /// </summary>
        /// <param name="propertyPath">The property path.</param>
        private void AddProjectionProperty(string propertyPath)
        {
            if (Context.Properties.Contains(propertyPath))
                return;

            Context.Properties.Add(propertyPath);
        }

        /// <summary>
        /// Maps the projected properties.
        /// </summary>
        private void MapProjectedProperties()
        {
            MapProjectedProperties(Context.Source, Context.Target, Context.Properties);
        }

        /// <summary>
        /// Maps the projected properties.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="properties">The properties.</param>
        private void MapProjectedProperties(object source, object target, IEnumerable<string> properties)
        {
            var propertyInfos = GetPropertyInfo(source.GetType());

            var propertyGroups = properties
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Split(new[] { '.' }, 2))
                .ToLookup(x => x.First(), x => x.Skip(1).FirstOrDefault());

            foreach (var propertyGroup in propertyGroups)
            {
                var propertyInfo =
                    propertyInfos.FirstOrDefault(x => x.Name.EqualsIgnoreCase(propertyGroup.Key)) ??
                    GetPropertyInfoForAnElement(propertyInfos, propertyGroup.Key);
                if (propertyInfo == null) continue;

                var sourceValue = propertyInfo.GetValue(source);
                var targetValue = sourceValue;

                var childProperties = propertyGroup
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (childProperties.Any() && sourceValue != null)
                {
                    targetValue = _isTargetCreated
                        ? Activator.CreateInstance(propertyInfo.PropertyType)
                        : propertyInfo.GetValue(target);

                    var targetList = targetValue as IList;

                    // Nested complex types
                    if (targetList == null)
                    {
                        MapProjectedProperties(sourceValue, targetValue, childProperties);
                        propertyInfo.SetValue(target, targetValue);
                        continue;
                    }

                    var args = propertyInfo.PropertyType.GetGenericArguments();
                    var childType = args.FirstOrDefault() ?? propertyInfo.PropertyType.GetElementType();
                    var sourceList = (IList)sourceValue;

                    // Recurring elements
                    foreach (var sourceItem in sourceList)
                    {
                        var targetItem = sourceItem;

                        if (_isTargetCreated)
                        {
                            targetItem = Activator.CreateInstance(childType);
                            targetList.Add(targetItem);
                        }

                        MapProjectedProperties(sourceItem, targetItem, childProperties);
                    }
                }

                propertyInfo.SetValue(target, targetValue);
            }
        }
    }
}
