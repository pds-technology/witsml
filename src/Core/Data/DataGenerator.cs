//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.3
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
using System.Collections.Generic;
using Energistics.DataAccess.WITSML200.ComponentSchemas;

namespace PDS.WITSMLstudio.Data
{
    /// <summary>
    /// Provides methods to generate data.
    /// </summary>
    public class DataGenerator
    {
        /// <summary>
        /// The format applied to timestamps
        /// </summary>
        public readonly string TimestampFormat = "yyMMdd-HHmmss-fff";

        /// <summary>
        /// Creates a <see cref="Guid"/>.
        /// </summary>
        /// <returns>The Guid in string</returns>
        public string Uid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a name with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The name.</returns>
        public string Name(string prefix = null)
        {
            if (String.IsNullOrWhiteSpace(prefix))
                return DateTime.Now.ToString(TimestampFormat);

            return String.Format("{0} - {1}", prefix, DateTime.Now.ToString(TimestampFormat));
        }

        /// <summary>
        /// Lists the specified instances.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instances">The instances.</param>
        /// <returns></returns>
        public List<T> List<T>(params T[] instances)
        {
            return new List<T>(instances);
        }

        /// <summary>
        /// Converts the specified curve class string to a <see cref="DataObjectReference"/> instance.
        /// </summary>
        /// <param name="curveClass">The curve class.</param>
        /// <returns>A new <see cref="DataObjectReference"/> instance.</returns>
        public DataObjectReference ToPropertyKindReference(string curveClass)
        {
            return new DataObjectReference
            {
                ContentType = "application/x-eml+xml;version=2.1;type=PropertyKind",
                Uuid = Uid(),
                Title = curveClass,
            };
        }

        /// <summary>
        /// Generates the specified date time indexes starting at the given start index and using the specified interval.
        /// </summary>
        /// <param name="numOfRows">The number of rows to generate indexes for.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="interval">The interval between indexes.</param>
        /// <returns>The generated indexes.</returns>
        public List<string> GenerateDateTimeIndexes(int numOfRows, DateTimeOffset startIndex, TimeSpan interval)
        {
            var indexes = new List<string>();
            for (int i = 0; i < numOfRows; i++)
                indexes.Add(GenerateDateTimeIndex(startIndex + TimeSpan.FromTicks(interval.Ticks * i)));

            return indexes;
        }

        /// <summary>
        /// Generates the specified numeric indexes starting at the given start index and using the specified interval.
        /// </summary>
        /// <param name="numOfRows">The number of rows to generate indexes for.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="interval">The interval between indexes.</param>
        /// <returns>The generated indexes.</returns>
        public List<string> GenerateNumericIndexes(int numOfRows, double startIndex, double interval)
        {
            var indexes = new List<string>();
            for (int i = 0; i < numOfRows; i++)
                indexes.Add(GenerateNumericIndex(startIndex + i * interval));

            return indexes;
        }

        /// <summary>
        /// Generates the string representation of a date time index.
        /// </summary>
        /// <param name="index">The date time index to generate.</param>
        /// <returns>The string representation of the index.</returns>
        public string GenerateDateTimeIndex(DateTimeOffset index)
        {
            return index.ToString("o");
        }


        /// <summary>
        /// Generates the string representation of a numeric (depth or elapsed time) index.
        /// </summary>
        /// <param name="index">The numeric index to generate.</param>
        /// <returns>The string representation of the index.</returns>
        public string GenerateNumericIndex(double index)
        {
            return index.ToString("F3");
        }
    }
}
