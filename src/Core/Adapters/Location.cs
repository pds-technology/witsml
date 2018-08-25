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
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Encapsulates trajectory stations location from either a WITSML 1.4.1.1 or WITSML 1.3.1.1 trajectory station location
    /// </summary>
    [Serializable]
    public class Location
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.Location _location131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.Location _location141;

        /// <summary>
        /// Initializes a new <see cref="Location" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.Location" />.
        /// </summary>
        /// <param name="location">The WITSML 1.3.1.1 Location</param>
        public Location(Energistics.DataAccess.WITSML131.ComponentSchemas.Location location)
        {
            location.NotNull(nameof(location));

            _location131 = location;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="Location" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.Location" />.
        /// </summary>
        /// <param name="location">The WITSML 1.4.1.1 location</param>
        public Location(Energistics.DataAccess.WITSML141.ComponentSchemas.Location location)
        {
            location.NotNull(nameof(location));

            _location141 = location;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="Location" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.Location" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.Location" />.
        /// </summary>
        /// <param name="location">The WITSML 1.3.1.1 or 1.4.1.1 location</param>
        public Location(object location)
        {
            location.NotNull(nameof(location));

            if (location is Energistics.DataAccess.WITSML131.ComponentSchemas.Location)
            {
                _location131 = location as Energistics.DataAccess.WITSML131.ComponentSchemas.Location;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (location is Energistics.DataAccess.WITSML141.ComponentSchemas.Location)
            {
                _location141 = location as Energistics.DataAccess.WITSML141.ComponentSchemas.Location;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 location", nameof(location));
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.ComponentSchemas.Location || dataObject is Energistics.DataAccess.WITSML141.ComponentSchemas.Location);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying Location.
        /// </summary>
        public IUniqueId WrappedLocation => (IUniqueId)_location131 ?? _location141;


        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _location131?.Uid ?? _location141?.Uid; }
            set { if (_location131 != null) { _location131.Uid = value; } else { _location141.Uid = value; } }
        }

        /// <summary>
        /// Gets a value indicating whether the northing is specified.
        /// </summary>
        /// <value>
        ///   <c>true</c> if northing is specified; otherwise, <c>false</c>.
        /// </value>
        public bool NorthingSpecified => _location131?.NorthingSpecified ?? _location141.NorthingSpecified;

        private LengthMeasure _northing;

        /// <summary>
        /// Gets the northing.
        /// </summary>
        public LengthMeasure Northing
        {
            get
            {
                if (NorthingSpecified)
                {
                    return _northing ?? (_northing = new LengthMeasure((object)_location131?.Northing ?? _location141?.Northing));
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the easting is specified.
        /// </summary>
        /// <value>
        ///   <c>true</c> if easting is specified; otherwise, <c>false</c>.
        /// </value>
        public bool EastingSpecified => _location131?.EastingSpecified ?? _location141.EastingSpecified;


        private LengthMeasure _easting;

        /// <summary>
        /// Gets the easting.
        /// </summary>
        public LengthMeasure Easting
        {
            get
            {
                if (EastingSpecified)
                {
                    return _easting ?? (_easting = new LengthMeasure((object)_location131?.Easting ?? _location141?.Easting));
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the southing is specified.
        /// </summary>
        /// <value>
        ///   <c>true</c> if southing is specified; otherwise, <c>false</c>.
        /// </value>
        public bool SouthingSpecified => _location131?.SouthingSpecified ?? _location141.SouthingSpecified;

        private LengthMeasure _southing;

        /// <summary>
        /// Gets the northing.
        /// </summary>
        public LengthMeasure Southing
        {
            get
            {
                if (SouthingSpecified)
                {
                    return _southing ?? (_southing = new LengthMeasure((object)_location131?.Southing ?? _location141?.Southing));
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the westing is specified.
        /// </summary>
        /// <value>
        ///   <c>true</c> if westing is specified; otherwise, <c>false</c>.
        /// </value>
        public bool WestingSpecified => _location131?.WestingSpecified ?? _location141.WestingSpecified;

        private LengthMeasure _westing;

        /// <summary>
        /// Gets the northing.
        /// </summary>
        public LengthMeasure Westing
        {
            get
            {
                if (WestingSpecified)
                {
                    return _westing ?? (_westing = new LengthMeasure((object)_location131?.Westing ?? _location141?.Westing));
                }
                return null;
            }
        }

    }
}
