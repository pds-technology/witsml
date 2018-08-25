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
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Encapsulates geology intervals from either a WITSML 1.4.1.1 or WITSML 1.3.1.1 mud log
    /// </summary>
    [Serializable]
    public sealed class GeologyInterval
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval _geologyInterval131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval _geologyInterval141;

        /// <summary>
        /// Initializes a new <see cref="GeologyInterval" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval" />.
        /// </summary>
        /// <param name="geologyInterval">The WITSML 1.3.1.1 geology interval</param>
        public GeologyInterval(Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval geologyInterval)
        {
            geologyInterval.NotNull(nameof(geologyInterval));

            _geologyInterval131 = geologyInterval;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;

            InitializeLithologies();
        }

        /// <summary>
        /// Initializes a new <see cref="GeologyInterval" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval" />.
        /// </summary>
        /// <param name="geologyInterval">The WITSML 1.4.1.1 geology interval</param>
        public GeologyInterval(Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval geologyInterval)
        {
            geologyInterval.NotNull(nameof(geologyInterval));

            _geologyInterval141 = geologyInterval;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;

            InitializeLithologies();
        }

        /// <summary>
        /// Initializes a new <see cref="GeologyInterval" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval" />.
        /// </summary>
        /// <param name="geologyInterval">The WITSML 1.3.1.1 or 1.4.1.1 geology interval</param>
        public GeologyInterval(object geologyInterval)
        {
            geologyInterval.NotNull(nameof(geologyInterval));

            if (geologyInterval is Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval)
            {
                _geologyInterval131 = geologyInterval as Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (geologyInterval is Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval)
            {
                _geologyInterval141 = geologyInterval as Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 geology interval", nameof(geologyInterval));

            InitializeLithologies();
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.ComponentSchemas.GeologyInterval || dataObject is Energistics.DataAccess.WITSML141.ComponentSchemas.GeologyInterval);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying geology interval.
        /// </summary>
        public IUniqueId WrappedGeologyInterval => (IUniqueId)_geologyInterval131 ?? _geologyInterval141;

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _geologyInterval131?.Uid ?? _geologyInterval141?.Uid; }
            set { if (_geologyInterval131 != null) { _geologyInterval131.Uid = value; } else { _geologyInterval141.Uid = value; } }
        }

        /// <summary>
        /// Gets the name of the lithology type
        /// </summary>
        public string TypeLithologyName => _geologyInterval131?.TypeLithology?.ToString("F") ?? _geologyInterval141?.TypeLithology?.ToString("F");

        /// <summary>
        /// Gets the measured depth at top of interval.
        /// </summary>
        public double? MDTop => _geologyInterval131?.MDTop.Value ?? _geologyInterval141?.MDTop.Value;

        /// <summary>
        /// Gets the measured depth at base of interval.
        /// </summary>
        public double? MDBottom => _geologyInterval131?.MDBottom.Value ?? _geologyInterval141?.MDBottom.Value;

        /// <summary>
        /// Gets the true vertical depth at top of interval.
        /// </summary>
        public double? TvdTop => _geologyInterval131?.TvdTop.Value ?? _geologyInterval141?.TvdTop.Value;

        /// <summary>
        /// Gets the true vertical depth at base of interval.
        /// </summary>
        public double? TvdBase => _geologyInterval131?.TvdBase.Value ?? _geologyInterval141?.TvdBase.Value;

        /// <summary>
        /// Gets the geology intervals.
        /// </summary>
        public List<Lithology> Lithology { get; private set; }

        private void InitializeLithologies()
        {
            if (_geologyInterval131?.Lithology != null)
            {
                Lithology = new List<Lithology>();

                _geologyInterval131.Lithology.ForEach(x => Lithology.Add(new Lithology(x)));
            }

            if (_geologyInterval141?.Lithology != null)
            {
                Lithology = new List<Lithology>();

                _geologyInterval141.Lithology.ForEach(x => Lithology.Add(new Lithology(x)));
            }
        }
    }
}
