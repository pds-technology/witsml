//----------------------------------------------------------------------- 
// PDS WITSMLstudio Framework, 2018.3
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualBasic.FileIO;
using PDS.WITSMLstudio.Framework.Properties;

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Provides custom extension methods for .NET framework types.
    /// </summary>
    public static class FrameworkExtensions
    {
        private static readonly string _defaultEncryptionKey = Settings.Default.DefaultEncryptionKey;
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(FrameworkExtensions));

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
        /// Splits the quoted string value based on the specified separator.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>A string array.</returns>
        public static string[] SplitQuotedString(this string value, string separator)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new string[0];

            // Check to see if delimiter is a single character and there are no quoted strings
            if (separator.Length < 2 && !value.Contains("\""))
            {
                return value.SplitAndTrim(separator);
            }

            // TextFieldParser iterates when it detects new line characters
            value = value.Replace("\n", " ");

            using (var parser = new TextFieldParser(new StringReader(value)))
            {
                parser.SetDelimiters(separator);

                try
                {
                    return parser.ReadFields();
                }
                catch
                {
                    // Ignore
                }
            }

            return new string[0];
        }

        /// <summary>
        /// Joins an enumerable of strings that may contain quotes based with the specified separator in between.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="separator">The separator.</param>
        /// <returns>The string value.</returns>
        public static string JoinQuotedStrings(this IEnumerable<string> values, string separator)
        {
            var stringValues = values.Select(value =>
            {
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                var needQuotes = value.IndexOf(separator, StringComparison.InvariantCulture) >= 0
                                 || value.IndexOf("\"", StringComparison.InvariantCulture) >= 0
                                 || value.IndexOf(Environment.NewLine, StringComparison.InvariantCulture) >= 0;
                var csvValue = value.Replace("\"", "\"\"");

                return needQuotes ? $"\"{csvValue}\"" : csvValue;
            });
            return string.Join(separator, stringValues);
        }

        /// <summary>
        /// Trims leading and training white space and any trailing zeros after a decimal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string TrimTrailingZeros(this string value)
        {
            if (value == null) return null;

            // TODO: char version of contains is faster than string version (investigate whether it matters)
            //var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
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
        /// Determines whether the string matches the specified pattern
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns>
        ///   <c>true</c> if the specified pattern is match; otherwise, <c>false</c>.
        /// Empty or null pattern will always match
        /// </returns>
        public static bool IsMatch(this string value, string pattern)
        {
            return string.IsNullOrWhiteSpace(pattern) || value.EqualsIgnoreCase(pattern);
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
        /// Generates a unique name based on the collection of existing names.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="existingNames">The existing names.</param>
        /// <returns>name</returns>
        public static string ToUniqueName(this string name, string[] existingNames)
        {
            // Remove any non-alpha characters from the end of the string
            while (name.Length > 1 && !char.IsLetter(name[name.Length - 1]))
            {
                name = name.Substring(0, name.Length - 1);
            }

            // Check if there are any existing names that match the new copy
            var startIndex = 1;
            while (existingNames.ContainsIgnoreCase($"{name}_{startIndex}"))
            {
                startIndex++;
            }

            return $"{name}_{startIndex}";
        }

        /// <summary>
        /// Next or default.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="item">The item.</param>
        /// <returns>NextOrDefault</returns>
        public static T NextOrDefault<T>(this IList<T> collection, T item)
        {
            var index = collection.IndexOf(item);
            item = default(T);

            if (index + 1 < collection.Count)
                item = collection[index + 1];
            else if (index - 1 >= 0)
                item = collection[index - 1];

            return item;
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
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static IList<T> ForEach<T>(this IList<T> collection, Action<T, int> action)
        {
            for (var index = 0; index < collection.Count; ++index)
            {
                action(collection[index], index);
            }

            return collection;
        }

        /// <summary>
        /// Returns whether a collection contains any values.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static bool Any<T>(this IList<T> collection)
        {
            return collection.Count > 0;
        }

        /// <summary>
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static bool Any<T>(this IList<T> collection, Func<T, bool> action)
        {
            for (var index = 0; index < collection.Count; ++index)
            {
                if (action(collection[index])) return true;
            }

            return false;
        }

        /// <summary>
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static bool Any<T>(this IList<T> collection, Func<T, int, bool> action)
        {
            for (var index = 0; index < collection.Count; ++index)
            {
                if (action(collection[index], index)) return true;
            }

            return false;
        }

        /// <summary>
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static bool All<T>(this IList<T> collection, Func<T, bool> action)
        {
            for (var index = 0; index < collection.Count; ++index)
            {
                if (!action(collection[index])) return false;
            }

            return true;
        }

        /// <summary>
        /// Performs the specified action on each item in the collection.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to perform on each item in the collection.</param>
        /// <returns>The source collection, for chaining.</returns>
        public static bool All<T>(this IList<T> collection, Func<T, int, bool> action)
        {
            for (var index = 0; index < collection.Count; ++index)
            {
                if (!action(collection[index], index)) return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<T> Skip<T>(IEnumerable<T> values, int count)
        {
            var list = values as IList<T>;
            if (null != list)
                return list.Skip(count);

            var array = values as object[];
            if (null != array)
                return array.Skip(count).Cast<T>();

            return Enumerable.Skip(values, count);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<T> Skip<T>(this IList<T> values, int count)
        {
            if (count < 0)
                count = 0;

            while (count < values.Count)
                yield return values[count++];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static IEnumerable<T> Skip<T>(this T[] values, int count)
        {
            if (count < 0)
                count = 0;

            while (count < values.Length)
                yield return values[count++];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Contains(this string instance, char value)
        {
            return instance.IndexOf(value) >= 0;
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

        private class EnumMemberInfo
        {
            private class EnumMemberData
            {
                public EnumMemberData(StringComparer stringComparison)
                {
                    NamedValues = new Dictionary<string, object>(stringComparison);
                    EnumAttributeValues = new Dictionary<string, object>(stringComparison);
                    DescriptionAttributeValues = new Dictionary<string, object>(stringComparison);
                }

                public Dictionary<string, object> NamedValues { get; }
                public Dictionary<string, object> EnumAttributeValues { get; }
                public Dictionary<string, object> DescriptionAttributeValues { get; }

            }

            public EnumMemberInfo(Type enumType)
            {
                // must be a valid enumeration member
                if (!enumType.IsEnum)
                {
                    throw new ArgumentException($"{enumType} must be a valid enumeration type");
                }

                FieldInfo = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
                MappedValues = new Dictionary<long, object>();

                for (var i = 0; i < FieldInfo.Length; i++)
                {
                    var field = FieldInfo[i];
                    var fieldName = field.Name;
                    var fieldValue = field.GetValue(null);
                    var fieldConstant = Convert.ToInt64(field.GetRawConstantValue());
                    var xmlEnumAttribute = XmlAttributeCache<XmlEnumAttribute>.GetCustomAttribute(field);
                    var descriptionAttribute = XmlAttributeCache<DescriptionAttribute>.GetCustomAttribute(field);

                    MappedValues[fieldConstant] = fieldValue;

                    EnumMemberMappings.Item1.NamedValues[fieldName] = fieldValue;
                    EnumMemberMappings.Item2.NamedValues[fieldName] = fieldValue;

                    if (xmlEnumAttribute != null)
                    {
                        EnumMemberMappings.Item1.EnumAttributeValues[xmlEnumAttribute.Name] = fieldValue;
                        EnumMemberMappings.Item2.EnumAttributeValues[xmlEnumAttribute.Name] = fieldValue;
                    }

                    if (descriptionAttribute != null)
                    {
                        EnumMemberMappings.Item1.DescriptionAttributeValues[descriptionAttribute.Description] = fieldValue;
                        EnumMemberMappings.Item2.DescriptionAttributeValues[descriptionAttribute.Description] = fieldValue;
                    }
                }
            }

            private FieldInfo[] FieldInfo { get; }
            private Dictionary<long, object> MappedValues { get; }
            private Tuple<EnumMemberData, EnumMemberData> EnumMemberMappings { get; } = new Tuple<EnumMemberData, EnumMemberData>(
                new EnumMemberData(StringComparer.InvariantCulture), new EnumMemberData(StringComparer.InvariantCultureIgnoreCase));

            public object GetValue(long enumValue)
            {
                object mappedValue;
                if (!MappedValues.TryGetValue(enumValue, out mappedValue))
                    return null;

                return mappedValue;
            }

            public object GetValue(string enumValue, bool ignoreCase)
            {
                return ignoreCase
                    ? GetValue(EnumMemberMappings.Item2, enumValue)
                    : GetValue(EnumMemberMappings.Item1, enumValue);
            }

            private object GetValue(EnumMemberData enumMemberData, string enumValue)
            {
                object mappedValue;
                if (enumMemberData.NamedValues.TryGetValue(enumValue, out mappedValue)
                    || enumMemberData.EnumAttributeValues.TryGetValue(enumValue, out mappedValue)
                    || enumMemberData.DescriptionAttributeValues.TryGetValue(enumValue, out mappedValue))
                {
                    return mappedValue;
                }

                return null;
            }

        }

        private static ConcurrentDictionary<Type, EnumMemberInfo> _enumMemberInfo = new ConcurrentDictionary<Type, EnumMemberInfo>();

        /// <summary>
        /// Parses the enum.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumValue">The enum value.</param>
        /// <param name="ignoreCase">if set to <c>true</c> comparison is case insensitive.</param>
        /// <returns>The parsed enumeration value.</returns>
        public static object ParseEnum(this Type enumType, string enumValue, bool ignoreCase = true)
        {
            if (string.IsNullOrWhiteSpace(enumValue)) return null;

            object enumMember;
            // must be a valid enumeration member
            if (TryParseEnum(enumType, enumValue, ignoreCase, out enumMember))
                return enumMember;

            throw new ArgumentException($"Could not parse enumeration value ({enumValue}), ({enumType}).");
        }

        /// <summary>
        /// Tries to parses the enum value.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <param name="enumValue">The input enum value.</param>
        /// <param name="ignoreCase">if set to <c>true</c> comparison is case insensitive.</param>
        /// <param name="enumMember">The output enum value.</param>
        /// <returns>Whether parsing succeeded.</returns>
        public static bool TryParseEnum(this Type enumType, string enumValue, bool ignoreCase, out object enumMember)
        {
            enumMember = null;

            if (string.IsNullOrWhiteSpace(enumValue)) return false;

            enumType = Nullable.GetUnderlyingType(enumType) ?? enumType;

            // must be a valid enumeration type
            if (!enumType.IsEnum)
            {
                return false;
            }

            var enumMemberInfo = _enumMemberInfo.GetOrAdd(enumType, type => new EnumMemberInfo(type));
            try
            {
                enumMember = enumMemberInfo.GetValue(enumValue, ignoreCase);
            }
            catch
            {
                // Ignore
            }

            // must be a valid enumeration member
            return enumMember != null;
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

            try
            {
                bytes = ProtectedData.Unprotect(bytes, entropy,
                    forLocalMachine ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException ex)
            {
                _log.ErrorFormat("Error decrypting string: {0}", ex);
                return null;
            }
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


        /// <summary>
        /// Takes an incoming IEnumerable of T and splits it into "chunks" specified by size with the last partition containing whatever was left over
        /// </summary>
        /// <param name="sequence">the original enumerable to split up</param>
        /// <param name="size">the size of each partition</param>
        /// <typeparam name="T">generic type for enumerable</typeparam>
        /// <returns>an enumerable of enumerable chunks sized to match the size parameter, in original order</returns>
        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> sequence, int size)
        {
            List<T> partition = new List<T>(size);
            foreach (var item in sequence)
            {
                partition.Add(item);
                if (partition.Count == size)
                {
                    yield return partition;
                    partition = new List<T>(size);
                }
            }
            if (partition.Count > 0)
                yield return partition;
        }
    }
}
