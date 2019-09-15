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

namespace PDS.WITSMLstudio.Framework
{
    /// <summary>
    /// Generic helper class for ParseEnum method.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    public class Enum<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Enum{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public Enum(string value)
        {
            StringValue = value;
            EnumValue = Parse(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Enum{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public Enum(T value)
        {
            EnumValue = value;
            StringValue = (value as System.Enum)?.GetName();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Enum{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public Enum(System.Enum value)
        {
            EnumValue = (T)(object)value;
            StringValue = value?.GetName();
        }

        /// <summary>
        /// Gets or sets the string value.
        /// </summary>
        public string StringValue { get; set; }

        /// <summary>
        /// Gets or sets the enum value.
        /// </summary>
        public T EnumValue { get; set; }

        /// <summary>
        /// Parses the specified enumeration value.
        /// </summary>
        /// <param name="enumValue">The enum value.</param>
        /// <param name="ignoreCase">if set to <c>true</c> comparison is case insensitive.</param>
        /// <returns>The parsed enumeration value.</returns>
        public static T Parse(string enumValue, bool ignoreCase = true)
        {
            return (T)typeof(T).ParseEnum(enumValue, ignoreCase);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="string"/> to <see cref="Enum{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Enum<T>(string value)
        {
            return new Enum<T>(value);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Enum{T}"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator string(Enum<T> value)
        {
            return value?.StringValue;
        }

        /// <summary>
        /// Performs an explicit conversion from <typeparamref name="T"/> to <see cref="Enum{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Enum<T>(T value)
        {
            return new Enum<T>(value);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="System.Enum"/> to <see cref="Enum{T}"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator Enum<T>(System.Enum value)
        {
            return new Enum<T>(value);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="Enum{T}"/> to <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator T(Enum<T> value)
        {
            return value == null ? default(T) : value.EnumValue;
        }
    }
}