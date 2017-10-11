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

using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using PDS.WITSMLstudio.Data.Channels;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Adapters
{
    /// <summary>
    /// An adapter around a WITSML 1.3.1 or 1.4.1 log to abstract away the version-specific differences for
    /// client applications.
    /// </summary>
    [Serializable]
    public sealed class Log : IWellboreObject
    {
        private readonly Energistics.DataAccess.WITSML131.Log _log131;
        private readonly Energistics.DataAccess.WITSML141.Log _log141;

        /// <summary>
        /// Initializes a new <see cref="Log" /> based on a <see cref="Energistics.DataAccess.WITSML131.Log" />.
        /// </summary>
        /// <param name="log">The WITSML 1.3.1.1 log</param>
        public Log(Energistics.DataAccess.WITSML131.Log log)
        {
            log.NotNull(nameof(log));

            _log131 = log;
            DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            InitializeLogData();
        }

        /// <summary>
        /// Initializes a new <see cref="Log" /> based on a <see cref="Energistics.DataAccess.WITSML141.Log" />.
        /// </summary>
        /// <param name="log">The WITSML 1.4.1.1 log</param>
        public Log(Energistics.DataAccess.WITSML141.Log log)
        {
            log.NotNull(nameof(log));

            _log141 = log;
            DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            InitializeLogData();
        }

        /// <summary>
        /// Initializes a new <see cref="Log" /> based on either a <see cref="Energistics.DataAccess.WITSML131.Log" />
        /// or a <see cref="Energistics.DataAccess.WITSML141.Log" />.
        /// </summary>
        /// <param name="log">The WITSML 1.3.1.1 or 1.4.1.1 log</param>
        public Log(object log)
        {
            log.NotNull(nameof(log));

            if (log is Energistics.DataAccess.WITSML131.Log)
            {
                _log131 = log as Energistics.DataAccess.WITSML131.Log;
                DataSchemaVersion = OptionsIn.DataVersion.Version131.Value;
            }
            else if (log is Energistics.DataAccess.WITSML141.Log)
            {
                _log141 = log as Energistics.DataAccess.WITSML141.Log;
                DataSchemaVersion = OptionsIn.DataVersion.Version141.Value;
            }
            else
                throw new ArgumentException(@"Not a WITSML 1.3.1.1 or WITSML 1.4.1.1 log", nameof(log));

            InitializeLogData();
        }

        /// <summary>
        /// The data schema version of the object.
        /// </summary>
        public string DataSchemaVersion { get; }

        /// <summary>
        /// Gets the underlying log.
        /// </summary>
        public IWellboreObject WrappedLog => (IWellboreObject) _log131 ?? _log141;

        /// <summary>
        /// Gets or sets the parent Well object identifier.
        /// </summary>
        public string UidWell
        {
            get { return _log131?.UidWell ?? _log141?.UidWell; }
            set { if (_log131 != null) { _log131.UidWell = value; } else { _log141.UidWell = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Wellbore object identifier.
        /// </summary>
        public string UidWellbore
        {
            get { return _log131?.UidWellbore ?? _log141?.UidWellbore; }
            set { if (_log131 != null) { _log131.UidWellbore = value; } else { _log141.UidWellbore = value; } }
        }

        /// <summary>
        /// Gets or sets the unique object identifier.
        /// </summary>
        public string Uid
        {
            get { return _log131?.Uid ?? _log141?.Uid; }
            set { if (_log131 != null) { _log131.Uid = value; } else { _log141.Uid = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Well object name.
        /// </summary>
        public string NameWell
        {
            get { return _log131?.NameWell ?? _log141?.NameWell; }
            set { if (_log131 != null) { _log131.NameWell = value; } else { _log141.NameWell = value; } }
        }

        /// <summary>
        /// Gets or sets the parent Wellbore object name.
        /// </summary>
        public string NameWellbore
        {
            get { return _log131?.NameWellbore ?? _log141?.NameWellbore; }
            set { if (_log131 != null) { _log131.NameWellbore = value; } else { _log141.NameWellbore = value; } }
        }

        /// <summary>
        /// Gets or sets the data object name.
        /// </summary>
        public string Name
        {
            get { return _log131?.Name ?? _log141?.Name; }
            set { if (_log131 != null) { _log131.Name = value; } else { _log141.Name = value; } }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is time log.
        /// </summary>
        public bool IsTimeLog => IsDateTimeLogDataType(IndexTypeName, DataSchemaVersion);

        /// <summary>
        /// Gets a value indicating whether this Log instance is growing.
        /// </summary>
        public bool IsObjectGrowing => 
            _log131?.ObjectGrowingSpecified ?? false 
                ? _log131.ObjectGrowing.Value 
                : _log141?.ObjectGrowingSpecified ?? false 
                    ? _log141.ObjectGrowing.Value 
                    : false;

        /// <summary>
        /// Gets the name of the index curve.
        /// </summary>
        public string IndexCurve => _log131?.IndexCurve?.Value ?? _log141?.IndexCurve;

        /// <summary>
        /// Gets the name of the index type.
        /// </summary>
        public string IndexTypeName => _log131?.IndexType?.GetName() ?? _log141?.IndexType?.GetName();

        /// <summary>
        /// Gets the name of the index direction.
        /// </summary>
        public string DirectionName => _log131?.Direction?.ToString("F") ?? _log141?.Direction?.ToString("F");

        /// <summary>
        /// Gets the log's null value.
        /// </summary>
        public string NullValue => _log131?.NullValue ?? _log141?.NullValue;

        /// <summary>
        /// Gets the name of the index curve.
        /// </summary>
        public string DataDelimiter => string.IsNullOrEmpty(_log141?.DataDelimiter) ? "," : _log141.DataDelimiter;

        /// <summary>
        /// Gets the log's start datetime index.
        /// </summary>
        public Timestamp? StartDateTimeIndex => _log131?.StartDateTimeIndex ?? _log141?.StartDateTimeIndex;

        /// <summary>
        /// Gets the log's end datetime index.
        /// </summary>
        public Timestamp? EndDateTimeIndex => _log131?.EndDateTimeIndex ?? _log141?.EndDateTimeIndex;

        /// <summary>
        /// Gets the log's start index.
        /// </summary>
        public double? StartIndex => _log131?.StartIndex?.Value ?? _log141?.StartIndex?.Value;

        /// <summary>
        /// Gets the log's end index.
        /// </summary>
        public double? EndIndex => _log131?.EndIndex?.Value ?? _log141?.EndIndex?.Value;

        /// <summary>
        /// Gets the log's depth uom.
        /// </summary>
        public string DepthUom => IsTimeLog ? null : _log131?.StartIndex?.Uom ?? _log141?.StartIndex?.Uom;

        /// <summary>
        /// Gets the log data.
        /// </summary>
        public List<LogData> LogData { get; private set; }

        /// <summary>
        /// Gets the log curve items.
        /// </summary>
        /// <returns>The log curve items.</returns>
        public List<LogCurveInfo> GetLogCurves() =>
            _log131?.LogCurveInfo?.Select(c => new LogCurveInfo(c)).ToList() ??
            _log141?.LogCurveInfo?.Select(c => new LogCurveInfo(c)).ToList();

        /// <summary>
        /// Gets a <see cref="ChannelDataReader"/> for the log.
        /// </summary>
        /// <param name="mnemonicPropertyPath">The mnemonic property path.</param>
        /// <returns>A <see cref="ChannelDataReader"/> instance.</returns>
        public ChannelDataReader GetReader(string mnemonicPropertyPath = null)
        {
            return _log131?.GetReader(mnemonicPropertyPath) ?? _log141.GetReaders(mnemonicPropertyPath).FirstOrDefault();
        }

        /// <summary>
        /// Checks if the data type is a datetime data type in the specified data schema version.
        /// </summary>
        /// <param name="datatype">The data type to check.</param>
        /// <param name="version">The data schema version.</param>
        /// <returns><c>true</c> if it is a datetime data type in the specified data schema version; <c>false</c> otherwise.</returns>
        public static bool IsDateTimeLogDataType(string datatype, string version)
        {
            if (OptionsIn.DataVersion.Version131.Equals(version))
            {
                return Energistics.DataAccess.WITSML131.ReferenceData.LogDataType.datetime.GetName().Equals(datatype) // This is the correct comparison.
                    || Energistics.DataAccess.WITSML131.ReferenceData.LogDataType.datetime.ToString().Equals(datatype); // XXX This is incorrect but works around an issue in LogCurveItem creation
            }
            if (OptionsIn.DataVersion.Version141.Equals(version))
            {
                return Energistics.DataAccess.WITSML141.ReferenceData.LogDataType.datetime.GetName().Equals(datatype) // This is the correct comparison.
                       || Energistics.DataAccess.WITSML141.ReferenceData.LogDataType.datetime.ToString().Equals(datatype); // XXX This is incorrect but works around an issue in LogCurveItem creation
            }
            return false;
        }

        /// <summary>
        /// Removes the log curves not found in the array.
        /// </summary>
        /// <param name="mnemonics">An array of mnemonics.</param>
        /// <param name="mnemonicPropertyName">Name of the mnemonic property.</param>
        public void RemoveLogCurves(string[] mnemonics, string mnemonicPropertyName)
        {
            _log131?.LogCurveInfo.RemoveAll(x =>
            {
                var mnemonic = string.IsNullOrWhiteSpace(mnemonicPropertyName)
                    ? x.Mnemonic
                    : ChannelDataExtensions.GetMnemonic(x, mnemonicPropertyName);
                return !mnemonics.ContainsIgnoreCase(mnemonic);
            });

            _log141?.LogCurveInfo.RemoveAll(x =>
            {
                var mnemonic = string.IsNullOrWhiteSpace(mnemonicPropertyName)
                    ? x.Mnemonic.Value
                    : ChannelDataExtensions.GetMnemonic(x, mnemonicPropertyName);
                return !mnemonics.ContainsIgnoreCase(mnemonic);
            });
        }

        /// <summary>
        /// Initializes the log data.
        /// </summary>
        private void InitializeLogData()
        {
            if (_log131 != null && _log131.LogCurveInfo != null && _log131.LogCurveInfo.Count > 0)
            {
                var mnemonicList = _log131.LogCurveInfo.OrderBy(x => x.ColumnIndex).Select(l => l.Mnemonic).ToArray();
                var unitList = _log131.LogCurveInfo.OrderBy(x => x.ColumnIndex).Select(l => l.Unit ?? string.Empty).ToArray();

                LogData = new List<LogData>();

                if (_log131.LogData != null)
                    LogData.Add(new LogData(mnemonicList, unitList, _log131.LogData));
            }

            if (_log141 != null && _log141.LogData != null)
            {
                LogData = new List<LogData>();

                foreach (var logData in _log141.LogData)
                {
                    var mnemonicList = logData.MnemonicList.Split(',').ToArray();
                    var unitList = logData.UnitList.Split(',').ToArray();

                    var data = logData.Data;

                    LogData.Add(new LogData(mnemonicList, unitList, data));
                }
            }
        }
    }
}
