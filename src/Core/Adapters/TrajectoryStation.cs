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
    /// Encapsulates trajectory stations from either a WITSML 1.4.1.1 or WITSML 1.3.1.1 trajectory
    /// </summary>
    [Serializable]
    public sealed class TrajectoryStation
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation _trajectoryStation131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation _trajectoryStation141;

        /// <summary>
        /// Initializes a new <see cref="TrajectoryStation" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation" />.
        /// </summary>
        /// <param name="trajectoryStation">The WITSML 1.3.1.1 trajectory station</param>
        public TrajectoryStation(Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation trajectoryStation)
        {
            trajectoryStation.NotNull(nameof(trajectoryStation));

            _trajectoryStation131 = trajectoryStation;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;

            InitializeLocations();
        }

        /// <summary>
        /// Initializes a new <see cref="TrajectoryStation" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation" />.
        /// </summary>
        /// <param name="trajectoryStation">The WITSML 1.4.1.1 trajectory station</param>
        public TrajectoryStation(Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation trajectoryStation)
        {
            trajectoryStation.NotNull(nameof(trajectoryStation));

            _trajectoryStation141 = trajectoryStation;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;

            InitializeLocations();
        }

        /// <summary>
        /// Initializes a new <see cref="TrajectoryStation" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation" />.
        /// </summary>
        /// <param name="trajectoryStation">The WITSML 1.3.1.1 or 1.4.1.1 trajectory station</param>
        public TrajectoryStation(object trajectoryStation)
        {
            trajectoryStation.NotNull(nameof(trajectoryStation));

            if (trajectoryStation is Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation)
            {
                _trajectoryStation131 = trajectoryStation as Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (trajectoryStation is Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation)
            {
                _trajectoryStation141 = trajectoryStation as Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 trajectory station", nameof(trajectoryStation));

            InitializeLocations();
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.ComponentSchemas.TrajectoryStation || dataObject is Energistics.DataAccess.WITSML141.ComponentSchemas.TrajectoryStation);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying trajectory station.
        /// </summary>
        public IUniqueId WrappedTrajectoryStation => (IUniqueId)_trajectoryStation131 ?? _trajectoryStation141;

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _trajectoryStation131?.Uid ?? _trajectoryStation141?.Uid; }
            set { if (_trajectoryStation131 != null) { _trajectoryStation131.Uid = value; } else { _trajectoryStation141.Uid = value; } }
        }

        /// <summary>
        /// Gets the measured depth.
        /// </summary>
        public double? MD => _trajectoryStation131?.MD?.Value ?? _trajectoryStation141?.MD?.Value;

        /// <summary>
        /// Gets the true vertical depth.
        /// </summary>
        public double? Tvd => _trajectoryStation131?.Tvd?.Value ?? _trajectoryStation141?.Tvd?.Value;

        /// <summary>
        /// Gets the inclination.
        /// </summary>
        public double? Incl => _trajectoryStation131?.Incl?.Value ?? _trajectoryStation141?.Incl?.Value;

        /// <summary>
        /// Gets the azimuth.
        /// </summary>
        public double? Azi => _trajectoryStation131?.Azi?.Value ?? _trajectoryStation141?.Azi?.Value;

        /// <summary>
        /// Gets the magnetic toolface.
        /// </summary>
        public double? Mtf => _trajectoryStation131?.Mtf?.Value ?? _trajectoryStation141?.Mtf?.Value;

        /// <summary>
        /// Gets the gravity toolface.
        /// </summary>
        public double? Gtf => _trajectoryStation131?.Gtf?.Value ?? _trajectoryStation141?.Gtf?.Value;

        /// <summary>
        /// Gets the dogleg severity.
        /// </summary>
        public double? DoglegSeverity => _trajectoryStation131?.DoglegSeverity?.Value ?? _trajectoryStation141?.DoglegSeverity?.Value;

        ///<summary>
        /// Gets the vertical section
        /// </summary>
        public LengthMeasure VertSect
        {
            get
            {
                if (_trajectoryStation131?.VertSect != null)
                {
                    return new LengthMeasure(_trajectoryStation131.VertSect);
                }

                if (_trajectoryStation141?.VertSect != null)
                {
                    return new LengthMeasure(_trajectoryStation141.VertSect);
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the dispEw / easting value
        /// </summary>
        public LengthMeasure DispEW
        {
            get
            {
                if (_trajectoryStation131?.DispEW != null)
                {
                    return new LengthMeasure(_trajectoryStation131.DispEW);
                }

                if (_trajectoryStation141?.DispEW != null)
                {
                    return new LengthMeasure(_trajectoryStation141.DispEW);
                }

                return null;
            }
        }

        /// <summary>
        /// returns the dispNs / northing value
        /// </summary>
        public LengthMeasure DispNS
        {
            get
            {
                if (_trajectoryStation131?.DispNS != null)
                {
                    return new LengthMeasure(_trajectoryStation131.DispNS);
                }

                if (_trajectoryStation141?.DispNS != null)
                {
                    return new LengthMeasure(_trajectoryStation141.DispNS);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the trajectory stations.
        /// </summary>
        public List<Location> Location { get; private set; }

        private void InitializeLocations()
        {
            if (_trajectoryStation131?.Location != null)
            {
                Location = new List<Location>();

                _trajectoryStation131.Location.ForEach(x => Location.Add(new Location(x)));
            }

            if (_trajectoryStation141?.Location != null)
            {
                Location = new List<Location>();

                _trajectoryStation141.Location.ForEach(x => Location.Add(new Location(x)));
            }
        }
    }
}
