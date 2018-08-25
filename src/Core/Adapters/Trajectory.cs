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
using PDS.WITSMLstudio.Data.Trajectories;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// An adapter around a WITSML 1.3.1 or 1.4.1 trajectory to abstract away the version-specific differences for
    /// client applications.
    /// </summary>
    [Serializable]
    public sealed class Trajectory : IWellboreObject
    {
        private readonly Energistics.DataAccess.WITSML131.Trajectory _trajectory131;
        private readonly Energistics.DataAccess.WITSML141.Trajectory _trajectory141;

        /// <summary>
        /// Initializes a new <see cref="Trajectory" /> based on a <see cref="Energistics.DataAccess.WITSML131.Trajectory" />.
        /// </summary>
        /// <param name="trajectory">The WITSML 1.3.1.1 trajectory</param>
        public Trajectory(Energistics.DataAccess.WITSML131.Trajectory trajectory)
        {
            trajectory.NotNull(nameof(trajectory));

            _trajectory131 = trajectory;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            InitializeTrajectoryStations();
        }

        /// <summary>
        /// Initializes a new <see cref="Trajectory" /> based on a <see cref="Energistics.DataAccess.WITSML141.Trajectory" />.
        /// </summary>
        /// <param name="trajectory">The WITSML 1.4.1.1 trajectory</param>
        public Trajectory(Energistics.DataAccess.WITSML141.Trajectory trajectory)
        {
            trajectory.NotNull(nameof(trajectory));

            _trajectory141 = trajectory;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            InitializeTrajectoryStations();
        }

        /// <summary>
        /// Initializes a new <see cref="Trajectory" /> based on either a <see cref="Energistics.DataAccess.WITSML131.Trajectory" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.Trajectory" />.
        /// </summary>
        /// <param name="trajectory">The WITSML 1.3.1.1 or 1.4.1.1 trajectory</param>
        public Trajectory(object trajectory)
        {
            trajectory.NotNull(nameof(trajectory));

            if (trajectory is Energistics.DataAccess.WITSML131.Trajectory)
                _trajectory131 = trajectory as Energistics.DataAccess.WITSML131.Trajectory;
            else if (trajectory is Energistics.DataAccess.WITSML141.Trajectory)
                _trajectory141 = trajectory as Energistics.DataAccess.WITSML141.Trajectory;
            else
                throw new ArgumentException("Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 trajectory", "trajectory");

            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;

            InitializeTrajectoryStations();
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.Trajectory || dataObject is Energistics.DataAccess.WITSML141.Trajectory);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying trajectory.
        /// </summary>
        public IWellboreObject WrappedTrajectory => (IWellboreObject) _trajectory131 ?? _trajectory141;

        /// <summary>
        /// Gets or sets the parent Well object identifier.
        /// </summary>
        public string UidWell
        {
            get { return _trajectory131?.UidWell ?? _trajectory141.UidWell; }
            set { if (_trajectory131 != null) { _trajectory131.UidWell = value; } else { _trajectory141.UidWell = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Wellbore object identifier.
        /// </summary>
        public string UidWellbore
        {
            get { return _trajectory131?.UidWellbore ?? _trajectory141.UidWellbore; }
            set { if (_trajectory131 != null) { _trajectory131.UidWellbore = value; } else { _trajectory141.UidWellbore = value; } }
        }

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _trajectory131?.Uid ?? _trajectory141.Uid; }
            set { if (_trajectory131 != null) { _trajectory131.Uid = value; } else { _trajectory141.Uid = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Well object name.
        /// </summary>
        public string NameWell
        {
            get { return _trajectory131?.NameWell ?? _trajectory141.NameWell; }
            set { if (_trajectory131 != null) { _trajectory131.NameWell = value; } else { _trajectory141.NameWell = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Wellbore object name.
        /// </summary>
        public string NameWellbore
        {
            get { return _trajectory131?.NameWellbore ?? _trajectory141.NameWellbore; }
            set { if (_trajectory131 != null) { _trajectory131.NameWellbore = value; } else { _trajectory141.NameWellbore = value; } }
        }

        /// <summary>
        /// Gets or sets the data object name.
        /// </summary>
        public string Name
        {
            get { return _trajectory131?.Name ?? _trajectory141.Name; }
            set { if (_trajectory131 != null) { _trajectory131.Name = value; } else { _trajectory141.Name = value; } }
        }


        /// <summary>
        /// Gets the trajectory's start MD.
        /// </summary>
        public double? MDMin => _trajectory131?.MDMin?.Value ?? _trajectory141?.MDMin?.Value;

        /// <summary>
        /// Gets the trajectory's end MD.
        /// </summary>
        public double? MDMax => _trajectory131?.MDMax?.Value ?? _trajectory141?.MDMax?.Value;

        /// <summary>
        /// Gets the trajectory's start MD UoM.
        /// </summary>
        public string MDMinUom => _trajectory131?.MDMin?.Uom.ToString("F") ?? _trajectory141?.MDMin?.Uom.ToString("F");

        /// <summary>
        /// Gets the trajectory's end MD UoM.
        /// </summary>
        public string MDMaxUom => _trajectory131?.MDMax?.Uom.ToString("F") ?? _trajectory141?.MDMax?.Uom.ToString("F");

        /// <summary>
        /// Gets the trajectory stations.
        /// </summary>
        public List<TrajectoryStation> TrajectoryStation { get; private set; }

        /// <summary>
        /// Gets a <see cref="TrajectoryDataReader"/> for the log.
        /// </summary>
        /// <returns>A <see cref="TrajectoryDataReader"/> instance.</returns>
        public TrajectoryDataReader GetReader()
        {
            return _trajectory131?.GetReader() ?? _trajectory141.GetReader();
        }

        private void InitializeTrajectoryStations()
        {
            if (_trajectory131?.TrajectoryStation != null)
            {
                TrajectoryStation = new List<TrajectoryStation>();

                _trajectory131.TrajectoryStation.ForEach(x => TrajectoryStation.Add(new TrajectoryStation(x)));
            }

            if (_trajectory141?.TrajectoryStation != null)
            {
                TrajectoryStation = new List<TrajectoryStation>();

                _trajectory141.TrajectoryStation.ForEach(x => TrajectoryStation.Add(new TrajectoryStation(x)));
            }
        }
    }
}
