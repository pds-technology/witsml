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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Energistics.DataAccess;
using log4net;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Provides helper methods to create template for Witsml object
    /// </summary>
    /// <typeparam name="T">The type of the object</typeparam>
    public class WitsmlQueryTemplate<T>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlQueryTemplate<T>));
        private static readonly IList<Type> ExcludeTypes = new List<Type>();
        private static readonly DateTime DefaultDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTimeOffset DefaultDateTimeOffset = DateTimeOffset.MinValue.AddYears(1899);
        private const string UidPattern = "[^ ]*";
        private T _instance;

        static WitsmlQueryTemplate()
        {
            Exclude<Witsml131.ComponentSchemas.CustomData>();
            Exclude<Witsml131.ComponentSchemas.DocumentInfo>();

            Exclude<Witsml141.ComponentSchemas.CustomData>();
            Exclude<Witsml141.ComponentSchemas.DocumentInfo>();
            Exclude<Witsml141.ComponentSchemas.ExtensionAny>();
            Exclude<Witsml141.ComponentSchemas.ExtensionNameValue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlQueryTemplate{T}"/> class.
        /// </summary>
        public WitsmlQueryTemplate()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlQueryTemplate{T}"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public WitsmlQueryTemplate(T instance)
        {
            _instance = instance;
        }

        private static void Exclude<TExclude>()
        {
            ExcludeTypes.Add(typeof(TExclude));
        }

        /// <summary>
        /// Creates an instance of the object.
        /// </summary>
        /// <returns>The instance of the object.</returns>
        public T AsObject()
        {
            if (_instance == null)
                _instance = (T)CreateTemplate(typeof(T));

            return _instance;
        }

        /// <summary>
        /// Creates a list of object.
        /// </summary>
        /// <returns>The list of object.</returns>
        public List<T> AsList()
        {
            return new List<T>() { AsObject() };
        }

        /// <summary>
        /// Converts to the XML string for the template.
        /// </summary>
        /// <returns>The XML string.</returns>
        public string AsXml()
        {
            return ToXml(AsObject());
        }

        /// <summary>
        /// Converts to the XML string for the collection.
        /// </summary>
        /// <typeparam name="TList">The type of the list.</typeparam>
        /// <returns>The XML string.</returns>
        public string AsXml<TList>() where TList : IEnergisticsCollection
        {
            var list = CreateTemplate(typeof(TList));
            return ToXml(list);
        }

        /// <summary>
        /// Creates the template.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The template.</returns>
        protected object CreateTemplate(Type objectType)
        {
            if (objectType == null || ExcludeTypes.Contains(objectType))
            {
                return null;
            }
            if (objectType == typeof(string))
            {
                return "abc";
            }
            if (objectType == typeof(bool))
            {
                return false;
            }
            if (objectType == typeof(short) || objectType == typeof(int) || objectType == typeof(long) || 
                objectType == typeof(double) || objectType == typeof(float) || objectType == typeof(decimal))
            {
                return Convert.ChangeType(1, objectType, CultureInfo.InvariantCulture);
            }
            if (objectType == typeof(DateTime))
            {
                return DefaultDateTime;
            }
            if (objectType == typeof(DateTimeOffset))
            {
                return DefaultDateTimeOffset;
            }
            if (objectType == typeof(Timestamp))
            {
                return new Timestamp(DefaultDateTimeOffset);
            }
            if (objectType.IsEnum)
            {
                return Enum.GetValues(objectType).GetValue(0);
            }
            if (objectType.IsGenericType)
            {
                var genericType = objectType.GetGenericTypeDefinition();

                if (genericType == typeof(Nullable<>))
                {
                    return Activator.CreateInstance(objectType, CreateTemplate(Nullable.GetUnderlyingType(objectType)));
                }
                if (genericType == typeof(List<>))
                {
                    var childType = objectType.GetGenericArguments()[0];
                    var list = Activator.CreateInstance(objectType) as IList;
                    list?.Add(CreateTemplate(childType));
                    return list;
                }
            }
            if (objectType.IsAbstract)
            {
                var concreteType = objectType.Assembly.GetTypes()
                    .FirstOrDefault(x => !x.IsAbstract && objectType.IsAssignableFrom(x));

                return CreateTemplate(concreteType);
            }

            var dataObject = Activator.CreateInstance(objectType);

            foreach (var property in objectType.GetProperties())
            {
                try
                {
                    if (property.CanWrite && !IsIgnored(property))
                    {
                        var regex = property.GetCustomAttribute<RegularExpressionAttribute>();

                        if (property.PropertyType == typeof(string) && regex != null && !UidPattern.Equals(regex.Pattern))
                        {
                            var attribute = property.GetCustomAttribute<RegularExpressionAttribute>();
                            var xeger = new Fare.Xeger(attribute.Pattern);
                            property.SetValue(dataObject, xeger.Generate());
                            continue;
                        }

                        property.SetValue(dataObject, CreateTemplate(property.PropertyType));
                    }
                }
                catch
                {
                    _log.WarnFormat("Error setting property value. Type: {0}; Property: {1}", objectType.FullName, property.Name);
                }
            }

            return dataObject;
        }

        private bool IsIgnored(PropertyInfo property)
        {
            return property.GetCustomAttributes<XmlIgnoreAttribute>().Any();
        }

        private string ToXml(object instance)
        {
            return WitsmlParser.ToXml(instance);
        }
    }
}
