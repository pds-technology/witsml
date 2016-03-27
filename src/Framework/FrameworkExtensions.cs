using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PDS.Framework
{
    /// <summary>
    /// Provides custom extension methods for .NET framework types.
    /// </summary>
    public static class FrameworkExtensions
    {
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
                var attribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .Cast<DescriptionAttribute>()
                    .FirstOrDefault();

                if (attribute != null)
                {
                    return attribute.Description;
                }
            }

            return value.ToString();
        }
    }
}
