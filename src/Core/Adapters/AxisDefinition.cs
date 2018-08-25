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
using System.Linq;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// An adapter around a WITSML 1.3.1 or 1.4.1 log curve info axis definition to abstract away
    /// the version-specific differences for client applications.
    /// </summary>
    [Serializable]
    public sealed class AxisDefinition : IUniqueId
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition _axisDefinition131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition _axisDefinition141;

        /// <summary>
        /// Initializes a new <see cref="AxisDefinition" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition" />.
        /// </summary>
        /// <param name="axisDefinition">The WITSML 1.3.1.1 log curve info axis definition</param>
        public AxisDefinition(Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition axisDefinition)
        {
            axisDefinition.NotNull(nameof(axisDefinition));

            _axisDefinition131 = axisDefinition;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="AxisDefinition" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition" />.
        /// </summary>
        /// <param name="axisDefinition">The WITSML 1.4.1.1 log curve info axis definition</param>
        public AxisDefinition(Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition axisDefinition)
        {
            axisDefinition.NotNull(nameof(axisDefinition));

            _axisDefinition141 = axisDefinition;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="AxisDefinition" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition" />.
        /// </summary>
        /// <param name="axisDefinition">The WITSML 1.3.1.1 or 1.4.1.1 log curve info axis definition</param>
        public AxisDefinition(object axisDefinition)
        {
            axisDefinition.NotNull(nameof(axisDefinition));

            if (axisDefinition is Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition)
            {
                _axisDefinition131 = axisDefinition as Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (axisDefinition is Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition)
            {
                _axisDefinition141 = axisDefinition as Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 log curve info axis definition", nameof(axisDefinition));
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.ComponentSchemas.AxisDefinition || dataObject is Energistics.DataAccess.WITSML141.ComponentSchemas.AxisDefinition);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying log curve info axis definition.
        /// </summary>
        public IUniqueId WrappedAxisDefinition => (IUniqueId) _axisDefinition131 ?? _axisDefinition141;

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _axisDefinition131?.Uid ?? _axisDefinition141?.Uid; }
            set { if (_axisDefinition131 != null) { _axisDefinition131.Uid = value; } else { _axisDefinition141.Uid = value; } }
        }

        /// <summary>
        /// Gets the order.
        /// </summary>
        public short? Order => _axisDefinition131?.Order ?? _axisDefinition141?.Order;

        /// <summary>
        /// Gets the count.
        /// </summary>
        public short? Count => _axisDefinition131?.Count ?? _axisDefinition141?.Count;

        /// <summary>
        /// Gets the double values.
        /// </summary>
        public string DoubleValues => _axisDefinition131?.DoubleValues ?? _axisDefinition141?.DoubleValues;

        /// <summary>
        /// Gets the string values.
        /// </summary>
        public string StringValues => _axisDefinition131?.StringValues ?? _axisDefinition141.StringValues;

        /// <summary>
        /// Gets the double values as an array of doubles.
        /// </summary>
        /// <returns>An array of doubles.</returns>
        public double[] GetDoubleValues() => DoubleValues.Split<double>();

        /// <summary>
        /// Gets the string values as an array of strings.
        /// </summary>
        /// <returns>An array of strings.</returns>
        public string[] GetStringValues() => StringValues.Split<string>();

        /// <summary>
        /// Gets the string or double values as an array of objects.
        /// </summary>
        /// <returns>An array of objects.</returns>
        public object[] GetValues()
        {
            return string.IsNullOrWhiteSpace(DoubleValues)
                ? GetStringValues().Cast<object>().ToArray()
                : GetDoubleValues().Cast<object>().ToArray();
        }
    }
}
