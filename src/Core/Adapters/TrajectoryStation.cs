//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2017.2
//
// Copyright 2017 PDS Americas LLC
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

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// Encapsulates trajectory stations from either a WITSML 1.4.1.1 or WITSML 1.3.1.1 trajectory
    /// </summary>
    public sealed class TrajectoryStation
    {
        /// <summary>
        /// Gets the measured depth.
        /// </summary>
        public double MD { get; }
        /// <summary>
        /// Gets the true vertical depth.
        /// </summary>
        public double? Tvd { get; }
        /// <summary>
        /// Gets the inclincation.
        /// </summary>
        public double? Incl { get; }
        /// <summary>
        /// Gets the azimuth.
        /// </summary>
        public double? Azi { get; }
        /// <summary>
        /// Gets the magnetic toolface.
        /// </summary>
        public double? Mtf { get; }
        /// <summary>
        /// Gets the gravity toolface.
        /// </summary>
        public double? Gtf { get; }
        /// <summary>
        /// Gets the dogleg severity.
        /// </summary>
        public double? DoglegSeverity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrajectoryStation" /> class.
        /// </summary>
        /// <param name="md">The measured depth.</param>
        /// <param name="tvd">The true vertical depth.</param>
        /// <param name="incl">The inclination.</param>
        /// <param name="azi">The azimuth.</param>
        /// <param name="mtf">The magnetic toolface.</param>
        /// <param name="gtf">The gravity toolface.</param>
        /// <param name="doglegSeverity">The dogleg severity.</param>
        public TrajectoryStation(double md, double? tvd, double? incl, double? azi, double? mtf, double? gtf, double? doglegSeverity)
        {
            MD = md;
            Tvd = tvd;
            Incl = incl;
            Azi = azi;
            Mtf = mtf;
            Gtf = gtf;
            DoglegSeverity = doglegSeverity;
        }
    }
}
