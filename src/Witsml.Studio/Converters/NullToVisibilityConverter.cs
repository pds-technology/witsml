//----------------------------------------------------------------------- 
// PDS.Witsml.Studio, 2016.1
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
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PDS.Witsml.Studio.Converters
{
    /// <summary>
    /// Converts an object to a Visibility enumeration using a null test.
    /// </summary>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>Initializes a new instance of the <see cref="T:PDS.Witsml.Studio.Converters.NullToVisibilityConverter" /> class.</summary>
        public NullToVisibilityConverter()
        {
        }

        /// <summary>Converts a Null value to a <see cref="T:System.Windows.Visibility" /> enumeration value for Visibility.</summary>
        /// <param name="value">The object that is Null tested to convert.</param>
        /// <param name="targetType">This parameter is not used.</param>
        /// <param name="parameter">This parameter is not used.</param>
        /// <param name="culture">This parameter is not used.</param>
        /// <returns>
        ///   <see cref="F:System.Windows.Visibility.Visible" /> if <paramref name="value" /> is not null; otherwise, <see cref="F:System.Windows.Visibility.Collapsed" />.
        /// </returns>        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// This method is not implemented
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
