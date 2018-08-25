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
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Encapsulates LengthMeasure from either a WITSML 1.4.1.1 or WITSML 1.3.1.1 LengthMeasure
    /// </summary>
    [Serializable]
    public class LengthMeasure
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure _lengthMeasure131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure _lengthMeasure141;

        /// <summary>
        /// Initializes a new <see cref="LengthMeasure" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure" />.
        /// </summary>
        /// <param name="lengthMeasure">The WITSML 1.3.1.1 LengthMeasure</param>
        public LengthMeasure(Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure lengthMeasure)
        {
            lengthMeasure.NotNull(nameof(lengthMeasure));

            _lengthMeasure131 = lengthMeasure;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="LengthMeasure" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure" />.
        /// </summary>
        /// <param name="lengthMeasure">The WITSML 1.4.1.1 LengthMeasure</param>
        public LengthMeasure(Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure lengthMeasure)
        {
            lengthMeasure.NotNull(nameof(lengthMeasure));

            _lengthMeasure141 = lengthMeasure;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="LengthMeasure" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure" />.
        /// </summary>
        /// <param name="lengthMeasure">The WITSML 1.3.1.1 or 1.4.1.1 LengthMeasure</param>
        public LengthMeasure(object lengthMeasure)
        {
            lengthMeasure.NotNull(nameof(lengthMeasure));

            if (lengthMeasure is Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure)
            {
                _lengthMeasure131 = (Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure)lengthMeasure;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (lengthMeasure is Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure)
            {
                _lengthMeasure141 = (Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure)lengthMeasure;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 LengthMeasure", nameof(lengthMeasure));
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.ComponentSchemas.LengthMeasure || dataObject is Energistics.DataAccess.WITSML141.ComponentSchemas.LengthMeasure);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the LengthMeasure value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public double Value => _lengthMeasure131?.Value ?? _lengthMeasure141?.Value ?? 0;

        /// <summary>
        /// Gets the LengthMeasure uom.
        /// </summary>
        public string Uom => _lengthMeasure131?.Uom.ToString() ?? _lengthMeasure141?.Uom.ToString();
    }
}
