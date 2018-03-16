//----------------------------------------------------------------------- 
// PDS WITSMLstudio Core, 2018.1
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
using System.Linq;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// An adapter around a WITSML 1.3.1 or 1.4.1 log curve info to abstract away
    /// the version-specific differences for client applications.
    /// </summary>
    [Serializable]
    public sealed class LogCurveInfo : IUniqueId
    {
        private readonly Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo _logCurveInfo131;
        private readonly Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo _logCurveInfo141;

        /// <summary>
        /// Initializes a new <see cref="LogCurveInfo" /> based on a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo" />.
        /// </summary>
        /// <param name="logCurveInfo">The WITSML 1.3.1.1 log curve info</param>
        public LogCurveInfo(Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo logCurveInfo)
        {
            logCurveInfo.NotNull(nameof(logCurveInfo));

            _logCurveInfo131 = logCurveInfo;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="LogCurveInfo" /> based on a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo" />.
        /// </summary>
        /// <param name="logCurveInfo">The WITSML 1.4.1.1 log curve info</param>
        public LogCurveInfo(Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo logCurveInfo)
        {
            logCurveInfo.NotNull(nameof(logCurveInfo));

            _logCurveInfo141 = logCurveInfo;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
        }

        /// <summary>
        /// Initializes a new <see cref="LogCurveInfo" /> based on either a <see cref="Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo" />.
        /// </summary>
        /// <param name="logCurveInfo">The WITSML 1.3.1.1 or 1.4.1.1 log curve info</param>
        public LogCurveInfo(object logCurveInfo)
        {
            logCurveInfo.NotNull(nameof(logCurveInfo));

            if (logCurveInfo is Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo)
            {
                _logCurveInfo131 = logCurveInfo as Energistics.DataAccess.WITSML131.ComponentSchemas.LogCurveInfo;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (logCurveInfo is Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo)
            {
                _logCurveInfo141 = logCurveInfo as Energistics.DataAccess.WITSML141.ComponentSchemas.LogCurveInfo;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 log curve info", nameof(logCurveInfo));
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying log curve info.
        /// </summary>
        public IUniqueId WrappedLogCurveInfo => (IUniqueId) _logCurveInfo131 ?? _logCurveInfo141;

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _logCurveInfo131?.Uid ?? _logCurveInfo141?.Uid; }
            set { if (_logCurveInfo131 != null) { _logCurveInfo131.Uid = value; } else { _logCurveInfo141.Uid = value; } }
        }

        /// <summary>
        /// Gets the mnemonic.
        /// </summary>
        public string Mnemonic => _logCurveInfo131?.Mnemonic ?? _logCurveInfo141?.Mnemonic.Value;

        /// <summary>
        /// Gets the mnem alias.
        /// </summary>
        public string MnemAlias => _logCurveInfo131?.MnemAlias ?? _logCurveInfo141?.MnemAlias?.Value;

        /// <summary>
        /// Gets the curve description.
        /// </summary>
        public string CurveDescription => _logCurveInfo131?.CurveDescription ?? _logCurveInfo141?.CurveDescription;

        /// <summary>
        /// Gets the unit.
        /// </summary>
        public string Unit => _logCurveInfo131?.Unit ?? _logCurveInfo141.Unit;

        /// <summary>
        /// Gets the null value.
        /// </summary>
        public string NullValue => _logCurveInfo131?.NullValue ?? _logCurveInfo141?.NullValue;

        /// <summary>
        /// Gets the name of the type log data.
        /// </summary>
        public string TypeLogDataName => _logCurveInfo131?.TypeLogData?.ToString("F") ?? _logCurveInfo141?.TypeLogData?.ToString("F");

        /// <summary>
        /// Gets the minimum index.
        /// </summary>
        public double? MinIndex => _logCurveInfo131?.MinIndex?.Value ?? _logCurveInfo141?.MinIndex?.Value;

        /// <summary>
        /// Gets the maximum index.
        /// </summary>
        public double? MaxIndex => _logCurveInfo131?.MaxIndex?.Value ?? _logCurveInfo141?.MaxIndex?.Value;

        /// <summary>
        /// Gets the minimum index of the date time.
        /// </summary>
        public Timestamp? MinDateTimeIndex => _logCurveInfo131?.MinDateTimeIndex ?? _logCurveInfo141?.MinDateTimeIndex;

        /// <summary>
        /// Gets the maximum index of the date time.
        /// </summary>
        public Timestamp? MaxDateTimeIndex => _logCurveInfo131?.MaxDateTimeIndex ?? _logCurveInfo141?.MaxDateTimeIndex;

        /// <summary>
        /// Gets a value indicating whether this instance has array data.
        /// </summary>
        public bool IsArrayData => _logCurveInfo131?.AxisDefinitionSpecified ?? _logCurveInfo141.AxisDefinitionSpecified;

        /// <summary>
        /// Gets the log curve infos axis definitions.
        /// </summary>
        /// <returns>The axis definitions.</returns>
        public List<AxisDefinition> GetAxisDefinitions() =>
            _logCurveInfo131?.AxisDefinition?.Select(c => new AxisDefinition(c)).ToList() ??
            _logCurveInfo141?.AxisDefinition?.Select(c => new AxisDefinition(c)).ToList();
    }
}
