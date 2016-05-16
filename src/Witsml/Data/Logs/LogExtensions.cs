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
using Witsml131 = Energistics.DataAccess.WITSML131;
using Witsml141 = Energistics.DataAccess.WITSML141;
using Witsml200 = Energistics.DataAccess.WITSML200;
using PDS.Framework;

namespace PDS.Witsml.Data.Logs
{
    public static class LogExtensions
    {
        /// <summary>
        /// Gets the <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> by uid.
        /// </summary>
        /// <param name="logCurveInfos">The collection of log curves.</param>
        /// <param name="uid">The uid.</param>
        /// <returns>The <see cref="Witsml131.ComponentSchemas.LogCurveInfo"/> specified by the uid.</returns>
        public static Witsml131.ComponentSchemas.LogCurveInfo GetByUid(this IEnumerable<Witsml131.ComponentSchemas.LogCurveInfo> logCurveInfos, string uid)
        {
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
            double? start = null;
            double? end = null;

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

            return new Range<double?>(start, end)
                .Sort(increasing);
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
            double? start = null;
            double? end = null;

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

            return new Range<double?>(start, end)
                .Sort(increasing);
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
                .Select(x => log.LogCurveInfo.First(lci => lci.Mnemonic.EqualsIgnoreCase(x)))
                .Select(n => GetNullValue(log.NullValue, n.NullValue));
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
                .Select(x => log.LogCurveInfo.First(lci => lci.Mnemonic.Value.EqualsIgnoreCase(x)))
                .Select(n => GetNullValue(log.NullValue, n.NullValue));
        }

        /// <summary>
        /// Gets the valid null value indicator from the specified values.
        /// </summary>
        /// <param name="logNullValue">The log null value.</param>
        /// <param name="logCurveInfoNullValue">The log curve information null value.</param>
        /// <returns>A valid null indicator value.</returns>
        private static string GetNullValue(string logNullValue, string logCurveInfoNullValue)
        {
            return !string.IsNullOrWhiteSpace(logCurveInfoNullValue)
                ? logCurveInfoNullValue
                : !string.IsNullOrWhiteSpace(logNullValue)
                ? logNullValue
                : "null";
        }
    }
}
