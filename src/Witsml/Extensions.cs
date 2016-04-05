//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
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
using Energistics.DataAccess;
using PDS.Witsml.Properties;

namespace PDS.Witsml
{
    /// <summary>
    /// Provides extension methods that can be used with common WITSML types and interfaces.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the description associated with the specified WITSML error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>The description for the error code.</returns>
        public static string GetDescription(this ErrorCodes errorCode)
        {
            return Resources.ResourceManager.GetString(errorCode.ToString(), Resources.Culture);
        }

        /// <summary>
        /// Gets the value of the Version property for specified container object.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The value of the Version property.</returns>
        public static string GetVersion<T>(this T dataObject) where T : IEnergisticsCollection
        {
            return (string)dataObject.GetType().GetProperty("Version").GetValue(dataObject, null);
        }

        /// <summary>
        /// Sets the value of the Version property for the specified container object.
        /// </summary>
        /// <typeparam name="T">The data object type.</typeparam>
        /// <param name="dataObject">The data object.</param>
        /// <param name="version">The version.</param>
        /// <returns>The data object instance.</returns>
        public static T SetVersion<T>(this T dataObject, string version) where T : IEnergisticsCollection
        {
            dataObject.GetType().GetProperty("Version").SetValue(dataObject, version);
            return dataObject;
        }

        /// <summary>
        /// Wraps the specified data object in a <see cref="List{TObject}"/>.
        /// </summary>
        /// <typeparam name="TObject">The type of data object.</typeparam>
        /// <param name="instance">The data object instance.</param>
        /// <returns>A <see cref="List{TObject}"/> instance containing a single item.</returns>
        public static List<TObject> AsList<TObject>(this TObject instance) where TObject : IDataObject
        {
            return new List<TObject>() { instance };
        }

        /// <summary>
        /// Converts a nullable scaled index to an index nullable double value.
        /// </summary>
        /// <param name="index">The nullable scaled index.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted index value</returns>
        public static double? IndexFromScale(this long? index, int scale, bool isTimeIndex = false)
        {
            if (index == null)
                return null;

            return index.Value.IndexFromScale(scale, isTimeIndex);
        }

        /// <summary>
        /// Converts a scaled index to an index double value.
        /// </summary>
        /// <param name="index">The scaled index.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted index value</returns>
        public static double IndexFromScale(this long index, int scale, bool isTimeIndex = false)
        {
            return isTimeIndex
                ? index
                : index * Math.Pow(10, -scale);
        }

        /// <summary>
        /// Converts a nullable index value to a nullable scaled long value.
        /// </summary>
        /// <param name="index">The nullable index value to be scaled.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted, scaled index value</returns>
        public static long? IndexToScale(this double? index, int scale, bool isTimeIndex = false)
        {
            if (index == null)
                return null;

            return index.Value.IndexToScale(scale, isTimeIndex);
        }

        /// <summary>
        /// Converts an index value to a scaled long value.
        /// </summary>
        /// <param name="index">The index value to be scaled.</param>
        /// <param name="scale">The scale factor value.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the index value is passed through, otherwise it is converted.</param>
        /// <returns>The converted, scaled index value</returns>
        public static long IndexToScale(this double index, int scale, bool isTimeIndex = false)
        {
            return Convert.ToInt64(
                (isTimeIndex
                    ? index
                    : index * Math.Pow(10, scale))
                );
        }
    }
}
