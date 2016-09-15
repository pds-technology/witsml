//----------------------------------------------------------------------- 
// PDS.Witsml, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
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
using System.Text.RegularExpressions;
using log4net;
using PDS.Framework;
using PDS.Witsml.Data.Channels;
using PDS.Witsml.Properties;
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;

namespace PDS.Witsml.Data.Logs
{
    /// <summary>
    /// Provides extension methods for log instances
    /// </summary>
    public static class LogExtensions
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (LogExtensions));
        private static readonly int _maxDataDelimiterLength = Settings.Default.MaxDataDelimiterLength;
        private static readonly string _dataDelimiterExclusions = @"[\d\s\.\+\-]";

        /// <summary>
        /// Determines whether the <see cref="Witsml131.Log"/> is increasing.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Witsml131.Log"/> is increasing; otherwise, false.
        /// </returns>
        public static bool IsIncreasing(this Witsml131.Log log)
        {
            return log.Direction.GetValueOrDefault(Witsml131.ReferenceData.LogIndexDirection.increasing) == Witsml131.ReferenceData.LogIndexDirection.increasing;
        }

        /// <summary>
        /// Determines whether the <see cref="Witsml141.Log"/> is increasing.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Witsml141.Log"/> is increasing; otherwise, false.
        /// </returns>
        public static bool IsIncreasing(this Witsml141.Log log)
        {
            return log.Direction.GetValueOrDefault(Witsml141.ReferenceData.LogIndexDirection.increasing) == Witsml141.ReferenceData.LogIndexDirection.increasing;
        }

        /// <summary>
        /// Determines whether the <see cref="Witsml131.Log"/> is a time log.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="includeElapsedTime">if set to <c>true</c>, include elapsed time.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Witsml131.Log"/> is a time log; otherwise, false.
        /// </returns>
        public static bool IsTimeLog(this Witsml131.Log log, bool includeElapsedTime = false)
        {
            if (log.IndexType.HasValue)
            {
                return log.IndexType.Value == Witsml131.ReferenceData.LogIndexType.datetime ||
                       (log.IndexType.Value == Witsml131.ReferenceData.LogIndexType.elapsedtime && includeElapsedTime);
            }

            // Use LogIndexType default if logData not available
            if (log.LogData == null) return true;

            var data = log.LogData.FirstOrDefault();
            return data.IsFirstValueDateTime();
        }



        /// <summary>
        /// Determines whether the <see cref="Witsml141.Log"/> is a time log.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="includeElapsedTime">if set to <c>true</c>, include elapsed time.</param>
        /// <returns>
        /// <c>true</c> if the <see cref="Witsml141.Log"/> is a time log; otherwise, false.
        /// </returns>
        public static bool IsTimeLog(this Witsml141.Log log, bool includeElapsedTime = false)
        {
            if (log.IndexType.HasValue)
            {
                return log.IndexType.Value == Witsml141.ReferenceData.LogIndexType.datetime ||
                       (log.IndexType.Value == Witsml141.ReferenceData.LogIndexType.elapsedtime && includeElapsedTime);
            }

            // Use LogIndexType default if logData not available
            if (log.LogData == null) return true;

            var data = log.LogData.SelectMany(x => x.Data ?? new List<string>(0)).FirstOrDefault();

            return data.IsFirstValueDateTime(log.GetDataDelimiterOrDefault());
        }

        /// <summary>
        /// Determines if the first value in the data row is a type of datetime.
        /// </summary>
        /// <param name="dataRow">A row of data from log data.</param>
        /// <param name="delimiter">The delimeter of the log data row.</param>
        /// <returns>
        /// <c>true</c> if the first value of the row is a type of date time; otherwise, false.
        /// </returns>
        public static bool IsFirstValueDateTime(this string dataRow, string delimiter = ",")
        {
            double value;
            return !double.TryParse(ChannelDataReader.Split(dataRow, delimiter).FirstOrDefault(), out value);
        }

        /// <summary>
        /// Gets the <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> by uid.
        /// </summary>
        /// <param name="logCurveInfos">The collection of log curves.</param>
        /// <param name="uid">The uid.</param>
        /// <returns>The <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> specified by the uid.</returns>
        public static Witsml131.ComponentSchemas.LogCurveInfo GetByUid(this IEnumerable<Witsml131.ComponentSchemas.LogCurveInfo> logCurveInfos, string uid)
        {
            _log.DebugFormat("Getting logCurveInfo by UID: {0}", uid);
            return logCurveInfos?.FirstOrDefault(x => x.Uid.EqualsIgnoreCase(uid));
        }

        /// <summary>
        /// Gets the <see cref="Witsml141.ComponentSchemas.LogCurveInfo"/> by uid.
        /// </summary>
        /// <param name="logCurveInfos">The collection of log curves.</param>
        /// <param name="uid">The uid.</param>
        /// <returns>The <see cref="Witsml141.ComponentSchemas.LogCurveInfo"/> specified by the uid.</returns>
        public static Witsml141.ComponentSchemas.LogCurveInfo GetByUid(this IEnumerable<Witsml141.ComponentSchemas.LogCurveInfo> logCurveInfos, string uid)
        {
            _log.DebugFormat("Getting logCurveInfo by UID: {0}", uid);
            return logCurveInfos?.FirstOrDefault(x => x.Uid.EqualsIgnoreCase(uid));
        }

        /// <summary>
        /// Gets the <see cref="Witsml200.Channel"/> by uuid.
        /// </summary>
        /// <param name="channels">The collection of channels.</param>
        /// <param name="uuid">The uuid.</param>
        /// <returns>The <see cref="Witsml200.Channel"/> specified by the uuid.</returns>
        public static Witsml200.Channel GetByUuid(this IEnumerable<Witsml200.Channel> channels, string uuid)
        {
            _log.DebugFormat("Getting Channel by UUID: {0}", uuid);
            return channels?.FirstOrDefault(x => x.Uuid.EqualsIgnoreCase(uuid));
        }

        /// <summary>
        /// Gets the <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> by mnemonic.
        /// </summary>
        /// <param name="logCurveInfos">The collection of log curves.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns>The <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> specified by the mnemonic.</returns>
        public static Witsml131.ComponentSchemas.LogCurveInfo GetByMnemonic(this IEnumerable<Witsml131.ComponentSchemas.LogCurveInfo> logCurveInfos, string mnemonic)
        {
            _log.DebugFormat("Getting logCurveInfo by mnemonic: {0}", mnemonic);
            return logCurveInfos?.FirstOrDefault(x => x.Mnemonic.EqualsIgnoreCase(mnemonic));
        }

        /// <summary>
        /// Gets the <see cref="Witsml141.ComponentSchemas.LogCurveInfo"/> by mnemonic.
        /// </summary>
        /// <param name="logCurveInfos">The collection of log curves.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns>The <see cref="Witsml141.ComponentSchemas.LogCurveInfo"/> specified by the mnemonic.</returns>
        public static Witsml141.ComponentSchemas.LogCurveInfo GetByMnemonic(this IEnumerable<Witsml141.ComponentSchemas.LogCurveInfo> logCurveInfos, string mnemonic)
        {
            _log.DebugFormat("Getting logCurveInfo by mnemonic: {0}", mnemonic);
            return logCurveInfos?.FirstOrDefault(x => x.Mnemonic.Value.EqualsIgnoreCase(mnemonic));
        }

        /// <summary>
        /// Gets the <see cref="Witsml200.Channel"/> by mnemonic.
        /// </summary>
        /// <param name="channels">The collection of channels.</param>
        /// <param name="mnemonic">The mnemonic.</param>
        /// <returns>The <see cref="Witsml200.Channel"/> specified by the mnemonic.</returns>
        public static Witsml200.Channel GetByMnemonic(this IEnumerable<Witsml200.Channel> channels, string mnemonic)
        {
            _log.DebugFormat("Getting Channel by mnemonic: {0}", mnemonic);
            return channels?.FirstOrDefault(x => x.Mnemonic.EqualsIgnoreCase(mnemonic));
        }

        /// <summary>
        /// Gets the index range for the specified <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/>.
        /// </summary>
        /// <param name="logCurveInfo">The log curve.</param>
        /// <param name="increasing">if set to <c>true</c>, index values are increasing.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> when using a datetime index.</param>
        /// <returns>The index range for the specified log curve.</returns>
        public static Range<double?> GetIndexRange(this Witsml131.ComponentSchemas.LogCurveInfo logCurveInfo, bool increasing = true, bool isTimeIndex = false)
        {
            _log.DebugFormat("Getting logCurveInfo index range: {0}", logCurveInfo?.Mnemonic);

            double? start = null;
            double? end = null;

            if (logCurveInfo == null)
                return new Range<double?>(null, null)
                    .Sort(increasing);

            if (isTimeIndex)
            {
                if (logCurveInfo.MinDateTimeIndex.HasValue)
                    start = logCurveInfo.MinDateTimeIndex.ToUnixTimeMicroseconds();
                if (logCurveInfo.MaxDateTimeIndex.HasValue)
                    end = logCurveInfo.MaxDateTimeIndex.ToUnixTimeMicroseconds();
            }
            else
            {
                if (logCurveInfo.MinIndex != null)
                    start = logCurveInfo.MinIndex.Value;
                if (logCurveInfo.MaxIndex != null)
                    end = logCurveInfo.MaxIndex.Value;
            }

            return increasing
                ? new Range<double?>(start, end)
                : new Range<double?>(end, start);
        }

        /// <summary>
        /// Gets the index range for the specified <see cref="Witsml141.ComponentSchemas.LogCurveInfo" />.
        /// </summary>
        /// <param name="logCurveInfo">The log curve.</param>
        /// <param name="increasing">if set to <c>true</c>, index values are increasing.</param>
        /// <param name="isTimeIndex">if set to <c>true</c> the log is using a datetime index.</param>
        /// <returns>The index range for the specified log curve.</returns>
        public static Range<double?> GetIndexRange(this Witsml141.ComponentSchemas.LogCurveInfo logCurveInfo, bool increasing = true, bool isTimeIndex = false)
        {
            _log.DebugFormat("Getting logCurveInfo index range: {0}", logCurveInfo?.Mnemonic?.Value);

            double? start = null;
            double? end = null;

            if (logCurveInfo == null)
                return new Range<double?>(null, null)
                    .Sort(increasing);

            if (isTimeIndex)
            {
                if (logCurveInfo.MinDateTimeIndex.HasValue)
                    start = logCurveInfo.MinDateTimeIndex.ToUnixTimeMicroseconds();
                if (logCurveInfo.MaxDateTimeIndex.HasValue)
                    end = logCurveInfo.MaxDateTimeIndex.ToUnixTimeMicroseconds();
            }
            else
            {
                if (logCurveInfo.MinIndex != null)
                    start = logCurveInfo.MinIndex.Value;
                if (logCurveInfo.MaxIndex != null)
                    end = logCurveInfo.MaxIndex.Value;
            }

            return increasing
                ? new Range<double?>(start, end)
                : new Range<double?>(end, start);
        }

        /// <summary>
        /// Gets the channels null value of the m
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <returns>
        /// A <see cref="IDictionary{TKey, TValue}" /> with the column index as key and the log curve null value as the value.
        /// </returns>
        public static IEnumerable<string> GetNullValues(this Witsml131.Log log, string[] mnemonics)
        {
            return mnemonics
                .Select(x => log.LogCurveInfo.GetByMnemonic(x))
                .Select(n => GetNullValue(log.NullValue, n?.NullValue));
        }

        /// <summary>
        /// Gets the null values with the column index
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="mnemonics">The mnemonics.</param>
        /// <returns>
        /// A <see cref="IDictionary{TKey, TValue}" /> with the column index as key and the log curve null value as the value.
        /// </returns>
        public static IEnumerable<string> GetNullValues(this Witsml141.Log log, string[] mnemonics)
        {
            return mnemonics
                .Select(x => log.LogCurveInfo.GetByMnemonic(x))
                .Select(n => GetNullValue(log.NullValue, n?.NullValue));
        }

        /// <summary>
        /// Gets the valid null value indicator from the specified values.
        /// </summary>
        /// <param name="logNullValue">The log null value.</param>
        /// <param name="logCurveInfoNullValue">The log curve information null value.</param>
        /// <returns>A valid null indicator value.</returns>
        private static string GetNullValue(string logNullValue, string logCurveInfoNullValue)
        {
            _log.DebugFormat("Getting null value from log: {0} or logCurveInfo: {1}", logNullValue, logCurveInfoNullValue);

            return !string.IsNullOrWhiteSpace(logCurveInfoNullValue)
                ? logCurveInfoNullValue
                : !string.IsNullOrWhiteSpace(logNullValue)
                ? logNullValue
                : "null";
        }

        /// <summary>
        /// Gets the data delimiter for the log or the default data delimiter.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>The data delimiter.</returns>
        public static string GetDataDelimiterOrDefault(this Witsml141.Log log)
        {
            return ChannelDataExtensions.GetDataDelimiterOrDefault(log?.DataDelimiter);
        }

        /// <summary>
        /// Determines whether the specified log's data delimiter is valid.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <returns>true if the log's data delimiter is valid, false otherwise.</returns>
        public static bool IsValidDataDelimiter(this Witsml141.Log log)
        {
            // null or empty string will use default data delimiter
            if (string.IsNullOrWhiteSpace(log.DataDelimiter)) return true;

            // check delimiter length
            if (log.DataDelimiter.Length > _maxDataDelimiterLength) return false;

            // check for invalid characters
            var regEx = new Regex(_dataDelimiterExclusions);
            return !regEx.IsMatch(log.DataDelimiter);
        }
    }
}
