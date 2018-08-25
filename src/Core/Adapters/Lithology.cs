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
    /// An adapter around a WITSML 1.3.1 or 1.4.1 geology interval lithology to abstract away
    /// the version-specific differences for client applications.
    /// </summary>
    [Serializable]
    public sealed class Lithology : IUniqueId
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology _lithology131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology _lithology141;

        /// <summary>
        /// Initializes a new <see cref="Lithology" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology" />.
        /// </summary>
        /// <param name="lithology">The WITSML 1.3.1.1 geology interval lithology</param>
        public Lithology(Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology lithology)
        {
            lithology.NotNull(nameof(lithology));

            _lithology131 = lithology;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="Lithology" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology" />.
        /// </summary>
        /// <param name="lithology">The WITSML 1.4.1.1 geology interval lithology</param>
        public Lithology(Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology lithology)
        {
            lithology.NotNull(nameof(lithology));

            _lithology141 = lithology;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="Lithology" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology" />.
        /// </summary>
        /// <param name="lithology">The WITSML 1.3.1.1 or 1.4.1.1 geology interval lithology</param>
        public Lithology(object lithology)
        {
            lithology.NotNull(nameof(lithology));

            if (lithology is Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology)
            {
                _lithology131 = lithology as Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (lithology is Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology)
            {
                _lithology141 = lithology as Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 geology interval lithology", nameof(lithology));
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.ComponentSchemas.Lithology || dataObject is Energistics.DataAccess.WITSML141.ComponentSchemas.Lithology);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying geology interval lithology.
        /// </summary>
        public IUniqueId WrappedLithology => (IUniqueId) _lithology131 ?? _lithology141;

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _lithology131?.Uid ?? _lithology141?.Uid; }
            set { if (_lithology131 != null) { _lithology131.Uid = value; } else { _lithology141.Uid = value; } }
        }

        /// <summary>
        /// Gets the lithology type name.
        /// </summary>
        public string LithologyTypeName => _lithology131?.Type?.Name ?? _lithology141?.Type?.Name;
    }
}
