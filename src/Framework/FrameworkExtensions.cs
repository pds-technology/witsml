//----------------------------------------------------------------------- 
// PDS.Framework, 2016.1
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
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PDS.Framework.Properties;

namespace PDS.Framework
{
    /// <summary>
    /// Provides custom extension methods for .NET framework types.
    /// </summary>
    public static class FrameworkExtensions
    {
        private static readonly string DefaultEncryptionKey = Settings.Default.DefaultEncryptionKey;

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
            foreach (var item in collection)
                action(item);
            
            return collection;
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
            int index = 0;

            foreach (var item in collection)
                action(item, index++);

            return collection;
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
            var enumType = value.GetType();
            var fieldInfo = enumType.GetField(Enum.GetName(enumType, value));

            if (fieldInfo != null)
            {
                var attribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true)
                    .Cast<DescriptionAttribute>()
                    .FirstOrDefault();

                if (attribute != null)
                {
                    return attribute.Description;
                }
            }

            return value.ToString();
        }

        /// <summary>
        /// Determines whether the specified type is numeric.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the type is numeric; otherwise, false</returns>
        public static bool IsNumeric(this Type type)
        {
            if (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            var typeCode = Type.GetTypeCode(type);
            return typeCode >= TypeCode.Char && typeCode <= TypeCode.Decimal;
        }

        /// <summary>
        /// Encrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="key">The encryption key.</param>
        /// <returns>The encrypted value.</returns>
        public static string Encrypt(this string value, string key = null)
        {
            if (value == null) return null;

            var bytes = Encoding.Unicode.GetBytes(value);
            var entropy = Encoding.Unicode.GetBytes(key ?? DefaultEncryptionKey);

            bytes = ProtectedData.Protect(bytes, entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decrypts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="key">The encryption key.</param>
        /// <returns>The decrypted value.</returns>
        public static string Decrypt(this string value, string key = null)
        {
            if (value == null) return null;

            var bytes = Convert.FromBase64String(value);
            var entropy = Encoding.Unicode.GetBytes(key ?? DefaultEncryptionKey);

            bytes = ProtectedData.Unprotect(bytes, entropy, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(bytes);
        }
    }
}
