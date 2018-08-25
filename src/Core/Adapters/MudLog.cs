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
    /// An adapter around a WITSML 1.3.1 or 1.4.1 MudLog to abstract away the version-specific differences for
    /// client applications.
    /// </summary>
    [Serializable]
    public sealed class MudLog : IWellboreObject
    {
        private readonly Energistics.DataAccess.WITSML131.MudLog _mudLog131;
        private readonly Energistics.DataAccess.WITSML141.MudLog _mudLog141;

        /// <summary>
        /// Initializes a new <see cref="MudLog" /> based on a <see cref="Energistics.DataAccess.WITSML131.MudLog" />.
        /// </summary>
        /// <param name="mudLog">The WITSML 1.3.1.1 MudLog</param>
        public MudLog(Energistics.DataAccess.WITSML131.MudLog mudLog)
        {
            mudLog.NotNull(nameof(mudLog));

            _mudLog131 = mudLog;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            InitializeGeologyIntervals();
        }

        /// <summary>
        /// Initializes a new <see cref="MudLog" /> based on a <see cref="Energistics.DataAccess.WITSML141.MudLog" />.
        /// </summary>
        /// <param name="mudLog">The WITSML 1.4.1.1 MudLog</param>
        public MudLog(Energistics.DataAccess.WITSML141.MudLog mudLog)
        {
            mudLog.NotNull(nameof(mudLog));

            _mudLog141 = mudLog;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            InitializeGeologyIntervals();
        }

        /// <summary>
        /// Initializes a new <see cref="MudLog" /> based on either a <see cref="Energistics.DataAccess.WITSML131.MudLog" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.MudLog" />.
        /// </summary>
        /// <param name="mudLog">The WITSML 1.3.1.1 or 1.4.1.1 MudLog</param>
        public MudLog(object mudLog)
        {
            mudLog.NotNull(nameof(mudLog));

            if (mudLog is Energistics.DataAccess.WITSML131.MudLog)
                _mudLog131 = mudLog as Energistics.DataAccess.WITSML131.MudLog;
            else if (mudLog is Energistics.DataAccess.WITSML141.MudLog)
                _mudLog141 = mudLog as Energistics.DataAccess.WITSML141.MudLog;
            else
                throw new ArgumentException("Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 MudLog", "mudLog");

            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;

            InitializeGeologyIntervals();
        }

        /// <summary>
        /// Returns whether the specified object is an instnce of a supported data type
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        public static bool IsSupportedObject(object dataObject)
        {
            return null != dataObject && (dataObject is Energistics.DataAccess.WITSML131.MudLog || dataObject is Energistics.DataAccess.WITSML141.MudLog);
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying MudLog.
        /// </summary>
        public IWellboreObject WrappedMudLog => (IWellboreObject) _mudLog131 ?? _mudLog141;

        /// <summary>
        /// Gets or sets the parent Well object identifier.
        /// </summary>
        public string UidWell
        {
            get { return _mudLog131?.UidWell ?? _mudLog141.UidWell; }
            set { if (_mudLog131 != null) { _mudLog131.UidWell = value; } else { _mudLog141.UidWell = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Wellbore object identifier.
        /// </summary>
        public string UidWellbore
        {
            get { return _mudLog131?.UidWellbore ?? _mudLog141.UidWellbore; }
            set { if (_mudLog131 != null) { _mudLog131.UidWellbore = value; } else { _mudLog141.UidWellbore = value; } }
        }

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _mudLog131?.Uid ?? _mudLog141.Uid; }
            set { if (_mudLog131 != null) { _mudLog131.Uid = value; } else { _mudLog141.Uid = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Well object name.
        /// </summary>
        public string NameWell
        {
            get { return _mudLog131?.NameWell ?? _mudLog141.NameWell; }
            set { if (_mudLog131 != null) { _mudLog131.NameWell = value; } else { _mudLog141.NameWell = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Wellbore object name.
        /// </summary>
        public string NameWellbore
        {
            get { return _mudLog131?.NameWellbore ?? _mudLog141.NameWellbore; }
            set { if (_mudLog131 != null) { _mudLog131.NameWellbore = value; } else { _mudLog141.NameWellbore = value; } }
        }

        /// <summary>
        /// Gets or sets the data object name.
        /// </summary>
        public string Name
        {
            get { return _mudLog131?.Name ?? _mudLog141.Name; }
            set { if (_mudLog131 != null) { _mudLog131.Name = value; } else { _mudLog141.Name = value; } }
        }


        /// <summary>
        /// Gets the MudLog's start MD.
        /// </summary>
        public double? StartMD => _mudLog131?.StartMD?.Value ?? _mudLog141?.StartMD?.Value;

        /// <summary>
        /// Gets the MudLog's end MD.
        /// </summary>
        public double? EndMD => _mudLog131?.EndMD?.Value ?? _mudLog141?.EndMD?.Value;

        /// <summary>
        /// Gets the MudLog's start MD UoM.
        /// </summary>
        public string StartMDUom => _mudLog131?.StartMD?.Uom.ToString("F") ?? _mudLog141?.StartMD?.Uom.ToString("F");

        /// <summary>
        /// Gets the MudLog's end MD UoM.
        /// </summary>
        public string EndMDUom => _mudLog131?.EndMD?.Uom.ToString("F") ?? _mudLog141?.EndMD?.Uom.ToString("F");

        /// <summary>
        /// Gets the MudLog geology intervals.
        /// </summary>
        public List<GeologyInterval> GeologyInterval { get; private set; }

        private void InitializeGeologyIntervals()
        {
            if (_mudLog131?.GeologyInterval != null)
            {
                GeologyInterval = new List<GeologyInterval>();

                _mudLog131.GeologyInterval.ForEach(x => GeologyInterval.Add(new GeologyInterval(x)));
            }

            if (_mudLog141?.GeologyInterval != null)
            {
                GeologyInterval = new List<GeologyInterval>();

                _mudLog141.GeologyInterval.ForEach(x => GeologyInterval.Add(new GeologyInterval(x)));
            }
        }
    }
}
