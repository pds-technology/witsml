//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.1
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using PDS.WITSMLstudio.Framework.Properties;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides custom extension methods for .NET framework types.
    /// </summary>
    public static class FrameworkExtensions
    {
        private static readonly string _defaultEncryptionKey = Settings.Default.DefaultEncryptionKey;

        /// <summary>
        /// Gets the version for the <see cref="System.Reflection.Assembly"/> containing the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="fieldCount">The field count.</param>
        /// <returns>The version number string.</returns>
        public static string GetAssemblyVersion(this Type type, int fieldCount = 4)
        {
            return type.Assembly.GetName().Version.ToString(fieldCount);
        }

        /// <summary>
        /// Throws an exception if the input parameter is null.
        /// </summary>
        /// <param name="parameter">The parameter to check.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is null.</exception>
        public static void NotNull(this object parameter, string parameterName)
        {
            if (parameter == null)
                throw new ArgumentNullException(parameterName);
        }

        /// <summary>
        /// Splits the string to an array.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="value">The string value.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>An array of values if successful, otherwise an empty array.</returns>
        public static T[] Split<T>(this string value, string separator = " ")
        {
            try
            {
                return value
                    .Split(new[] { separator }, StringSplitOptions.None)
                    .Select(s => (T) Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture))
                    .ToArray();
            }
            catch
            {
                return new T[0];
            }
        }

        /// <summary>
        /// Creates an array of trimmed strings by splitting this string at each occurence of a separator.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>A string array.</returns>
        public static string[] SplitAndTrim(this string value, string separator)
        {
            return string.IsNullOrWhiteSpace(value)
                ? new string[0]
                : value.Split(new[] { separator }, StringSplitOptions.None)
                       .Select(x => x.Trim())
                       .ToArray();
        }

        /// <summary>
        /// Trims leading and training white space and any trailing zeros after a decimal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string TrimTrailingZeros(this string value)
        {
            if (value == null) return null;

            var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            return value.Contains(separator)
                ? value.Trim().TrimEnd('0')
                : value.Trim();
        }

        /// <summary>
        /// Determines whether the string contains the specified value, ignoring case.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the string contains the specified value; otherwise, false.</returns>
        public static bool ContainsIgnoreCase(this string source, string value)
        {
            if (source == null) return false;
            return source.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        /// <summary>
        /// Determines whether the collection contains the specified value, ignoring case.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the collection contains the specified value; otherwise, false.</returns>
        public static bool ContainsIgnoreCase(this IEnumerable<string> source, string value)
        {
            return source.Any(x => x.EqualsIgnoreCase(value));
        }

        /// <summary>
        /// Determines whether two specified strings have the same value, ignoring case.
        /// </summary>
        /// <param name="a">The first string to compare, or null.</param>
        /// <param name="b">The second string to compare, or null.</param>
        /// <returns>
        /// true if the value of a is the same as the value of b; otherwise, false.
        /// If both a and b are null, the method returns true.
        /// </returns>
        public static bool EqualsIgnoreCase(this string a, string b)
        {
            return string.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Converts the specified string to camel case.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The camel case string value.</returns>
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Substring(0, 1).ToLowerInvariant() + value.Substring(1);
        }

        /// <summary>
        /// Converts the specified string to pascal case.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The pascal case string value.</returns>
        public static string ToPascalCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);
        }

        /// <summary>
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            var items = collection.ToArray();

            foreach (var item in items)
                action(item);
            
            return items;
        }

        /// <summary>
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T, int> action)
        {
            var items = collection.ToArray();
            var index = 0;

            foreach (var item in items)
                action(item, index++);

            return items;
        }

        /// <summary>
        /// Creates a dictionary from collection without duplicates.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        public static IDictionary<string, T> ToDictionaryIgnoreCase<T>(this IEnumerable<T> collection, Func<T, string> keySelector)
        {
            return ToDictionaryIgnoreCase(collection, keySelector, e => e);
        }

        /// <summary>
        ///  Creates a dictionary from collection without duplicates.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="elementSelector">The element selector.</param>
        /// <returns></returns>
        public static IDictionary<string, TValue> ToDictionaryIgnoreCase<T, TValue>(this IEnumerable<T> collection, Func<T, string> keySelector, Func<T, TValue> elementSelector)
        {
            return collection?
                .ToLookup(keySelector, elementSelector, StringComparer.InvariantCultureIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the property value from the specified object instance.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>The property value.</returns>
        public static object GetPropertyValue(this object instance, string propertyPath)
        {
            foreach (var propertyName in propertyPath.Split('.'))
            {
                if (instance == null) return null;

                var type = instance.GetType();
                var info = type.GetProperty(propertyName);

                if (info == null)
                {
                    var list = instance as IList;

                    if (list != null && list.Count > 0)
                    {
                        // TODO: Add support for property path with index or basic filter
                        instance = list[0];
                        type = instance?.GetType();
                        info = type?.GetProperty(propertyName);

                        if (info == null)
                            return null;
                    }
                    else
                    {
                        return null;
                    }
                }

                instance = info.GetValue(instance);
            }

            return instance;
        }

        /// <summary>
        /// Gets the property value from the specified object instance.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="instance">The object instance.</param>
        /// <param name="propertyPath">The property path.</param>
        /// <returns>The property value.</returns>
        public static T GetPropertyValue<T>(this object instance, string propertyPath)
        {
            var propertyValue = instance.GetPropertyValue(propertyPath);
            if (propertyValue == null) return default(T);
            return (T)propertyValue;
        }

        /// <summary>
        /// Gets the custom attribute defined for the specified enumeration member.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="value">The enumeration value.</param>
        /// <returns>The defined attribute, or null.</returns>
        public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
        {
            var enumType = value.GetType();
            var fieldInfo = enumType.GetField(Enum.GetName(enumType, value));

            return XmlAttributeCache<TAttribute>.GetCustomAttribute(fieldInfo);
        }

        /// <summary>
        /// Gets the description for the specified enumeration member.
        /// </summary>
        /// <param name="value">The enumeration value.</param>
        /// <returns>
        /// The description from the <see cref="DescriptionAttribute"/> when available;
        /// otherwise, the value's ToString() representation.
        /// </returns>
        public static string GetDescription(this Enum value)
        {
            var attribute = value.GetAttribute<DescriptionAttribute>();

            return attribute != null
                ? attribute.Description
                : value.ToString();
        }

        /// <summary>
        /// Gets the name for the specified enumeration member.
        /// </summary>
        /// <param name="value">The enumeration value.</param>
        /// <returns>
        /// The name from the <see cref="XmlEnumAttribute"/> when available;
        /// otherwise, the value's ToString() representation.
        /// </returns>
        public static string GetName(this Enum value)
        {
            var attribute = value.GetAttribute<XmlEnumAttribute>();

            return attribute != null
                ? attribute.Name
                : value.ToString();
        }

        /// <summary>
        /// Parses the enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumValue">The enum value.</param>
        /// <returns></returns>
        public static object ParseEnum(this Type enumType, string enumValue)
        {
            if (string.IsNullOrWhiteSpace(enumValue)) return null;

            try
            {
                double index;

                // Ensure enumValue is not numeric
                if (!double.TryParse(enumValue, out index))
                    return Enum.Parse(enumType, enumValue);
            }
            catch
            {
                // Ignore
            }

            var enumMember = enumType.GetMembers().FirstOrDefault(x =>
            {
                if (x.Name.EqualsIgnoreCase(enumValue))
                    return true;

                var xmlEnumAttrib = XmlAttributeCache<XmlEnumAttribute>.GetCustomAttribute(x);
                if (xmlEnumAttrib != null && xmlEnumAttrib.Name.EqualsIgnoreCase(enumValue))
                    return true;

                var descriptionAttr = XmlAttributeCache<DescriptionAttribute>.GetCustomAttribute(x);
                return descriptionAttr != null && descriptionAttr.Description.EqualsIgnoreCase(enumValue);
            });

            // must be a valid enumeration member
            if (!enumType.IsEnum || enumMember == null)
            {
                throw new ArgumentException();
            }

            return Enum.Parse(enumType, enumMember.Name);
        }

        /// <summary>
        /// Determines whether the specified type is numeric.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the type is numeric; otherwise, false</returns>
        public static bool IsNumeric(this Type type)
        {
            if (type == null) return false;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            var typeCode = Type.GetTypeCode(type);
            return typeCode >= TypeCode.SByte && typeCode <= TypeCode.Decimal;
        }

        /// <summary>
        /// Encrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="key">The encryption key.</param>
        /// <param name="forLocalMachine">if set to <c>true</c> encrypt for local machine.</param>
        /// <returns>The encrypted value.</returns>
        public static string Encrypt(this string value, string key = null, bool forLocalMachine = false)
        {
            if (value == null) return null;

            var bytes = Encoding.Unicode.GetBytes(value);
            var entropy = Encoding.Unicode.GetBytes(key ?? _defaultEncryptionKey);

            bytes = ProtectedData.Protect(bytes, entropy, forLocalMachine ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="key">The encryption key.</param>
        /// <param name="forLocalMachine">if set to <c>true</c> decrypt for local machine.</param>
        /// <returns>The decrypted value.</returns>
        public static string Decrypt(this string value, string key = null, bool forLocalMachine = false)
        {
            if (value == null) return null;

            var bytes = Convert.FromBase64String(value);
            var entropy = Encoding.Unicode.GetBytes(key ?? _defaultEncryptionKey);

            bytes = ProtectedData.Unprotect(bytes, entropy, forLocalMachine ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(bytes);
        }

        /// <summary>
        /// Creates a new <see cref="SecureString"/> from the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>A <see cref="SecureString"/> instance.</returns>
        public static SecureString ToSecureString(this string value)
        {
            var secure = new SecureString();

            if (!string.IsNullOrWhiteSpace(value))
                value.ForEach(secure.AppendChar);

            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>
        /// Gets the base exception of the specified type.
        /// </summary>
        /// <typeparam name="T">The exception type.</typeparam>
        /// <param name="ex">The exception.</param>
        /// <returns>An exception of the specified type, or null.</returns>
        public static T GetBaseException<T>(this Exception ex) where T : Exception
        {
            var typed = ex as T;
            if (typed != null) return typed;

            var inner = ex.InnerException;

            while (inner != null)
            {
                typed = inner as T;
                if (typed != null) return typed;

                inner = inner.InnerException;
            }

            return null;
        }
    }
}
