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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using log4net;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio
{
    /// <summary>
    /// Defines the set of well known configuration options that can be used 
    /// when requesting information from a WITSML store.
    /// </summary>
    public class OptionsIn
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(OptionsIn));

        /// <summary>
        /// Defines the list of supported data schema versions.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class DataVersion : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.DataVersion"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public DataVersion(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for a DataVersion OptionsIn
            /// </summary>
            public const string Keyword = "dataVersion";

            /// <summary>
            /// The DataVersion for version 1.3.1.1
            /// </summary>
            public static readonly DataVersion Version131 = new DataVersion("1.3.1.1");

            /// <summary>
            /// The DataVersion for version 1.4.1.1
            /// </summary>
            public static readonly DataVersion Version141 = new DataVersion("1.4.1.1");

            /// <summary>
            /// The DataVersion for version 2.0
            /// </summary>
            public static readonly DataVersion Version200 = new DataVersion("2.0");

            /// <summary>
            /// The DataVersion for version 2.1
            /// </summary>
            public static readonly DataVersion Version210 = new DataVersion("2.1");
        }

        /// <summary>
        /// Defines the list of returnElements configuration option values.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class ReturnElements : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.ReturnElements"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public ReturnElements(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for ReturnElements OptionsIn
            /// </summary>
            public const string Keyword = "returnElements";

            /// <summary>
            /// ReturnElements for all object data
            /// </summary>
            public static readonly ReturnElements All = new ReturnElements("all");

            /// <summary>
            /// ReturnElements for Id and Name object data
            /// </summary>
            public static readonly ReturnElements IdOnly = new ReturnElements("id-only");

            /// <summary>
            /// ReturnElements for all header data (applies only to growing data objects)
            /// </summary>
            public static readonly ReturnElements HeaderOnly = new ReturnElements("header-only");

            /// <summary>
            /// ReturnElements for all growing object data (applies only to growing data objects)
            /// </summary>
            public static readonly ReturnElements DataOnly = new ReturnElements("data-only");

            /// <summary>
            /// ReturnElements specialization of "data-only" for trajectories
            /// </summary>
            public static readonly ReturnElements StationLocationOnly = new ReturnElements("station-location-only");

            /// <summary>
            /// ReturnElements for the most recent changes and applies only to ChangeLogs
            /// </summary>
            public static readonly ReturnElements LatestChangeOnly = new ReturnElements("latest-change-only");

            /// <summary>
            /// ReturnElements to return only those data elements specified in the query.
            /// </summary>
            public static readonly ReturnElements Requested = new ReturnElements("requested");

            /// <summary>
            /// Creates a ReturnElements object for a specified value.
            /// </summary>
            /// <param name="value">The ReturnElements value.</param>
            /// <returns>A new <see cref="OptionsIn.ReturnElements"/> instance.</returns>
            public static ReturnElements Eq(string value) => new ReturnElements(value);

            /// <summary>
            /// Gets a collection of returnElements option values.
            /// </summary>
            /// <returns>A collection of all returnElements option values.</returns>
            public static IEnumerable<ReturnElements> GetValues()
            {
                yield return All;
                yield return IdOnly;
                yield return HeaderOnly;
                yield return DataOnly;
                yield return StationLocationOnly;
                yield return LatestChangeOnly;
                yield return Requested;
            }
        }

        /// <summary>
        /// Defines the maxReturnNodes option value.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class MaxReturnNodes : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.MaxReturnNodes"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public MaxReturnNodes(int value) : base(Keyword, value.ToString()) { }

            /// <summary>
            /// The keyword for MaxReturnNodes OptionsIn
            /// </summary>
            public const string Keyword = "maxReturnNodes";

            /// <summary>
            /// Creates a MaxReturnNodes object for a specified value.
            /// </summary>
            /// <param name="value">The MaxReturnNodes value.</param>
            /// <returns>A new <see cref="OptionsIn.MaxReturnNodes"/> instance.</returns>
            public static MaxReturnNodes Eq(int value)
            {
                return new MaxReturnNodes(value);
            }
        }

        /// <summary>
        /// Defines the list of compressionMethod configuration option values.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class CompressionMethod : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.CompressionMethod"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public CompressionMethod(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for CompressionMethod OptionsIn
            /// </summary>
            public const string Keyword = "compressionMethod";

            /// <summary>
            /// The CompressionMethod for no compression
            /// </summary>
            public static readonly CompressionMethod None = new CompressionMethod("none");

            /// <summary>
            /// The CompressionMethod for gzip compression
            /// </summary>
            public static readonly CompressionMethod Gzip = new CompressionMethod("gzip");

            /// <summary>
            /// Gets a collection of CompressionMethod option values.
            /// </summary>
            /// <returns>A collection of all CompressionMethod option values.</returns>
            public static IEnumerable<CompressionMethod> GetValues()
            {
                yield return None;
                yield return Gzip;
            }
        }

        /// <summary>
        /// Defines the choice of requestObjectSelectionCapability configuration option values.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class RequestObjectSelectionCapability : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.RequestObjectSelectionCapability"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public RequestObjectSelectionCapability(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for RequestObjectSelectionCapability OptionsIn.
            /// </summary>
            public const string Keyword = "requestObjectSelectionCapability";

            /// <summary>
            /// The RequestObjectSelectionCapability OptionsIn to not request object selection capabilities
            /// </summary>
            public static readonly RequestObjectSelectionCapability None = new RequestObjectSelectionCapability("none");

            /// <summary>
            /// The RequestObjectSelectionCapability OptionsIn to request object selection capabilities
            /// </summary>
            public static readonly RequestObjectSelectionCapability True = new RequestObjectSelectionCapability("true");

            /// <summary>
            /// Gets a collection of RequestObjectSelectionCapability option values.
            /// </summary>
            /// <returns>A collection of all RequestObjectSelectionCapability option values.</returns>
            public static IEnumerable<RequestObjectSelectionCapability> GetValues()
            {
                yield return None;
                yield return True;
            }
        }

        /// <summary>
        /// Defines the choice of RequestPrivateGroupOnly configuration option values.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class RequestPrivateGroupOnly : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.RequestPrivateGroupOnly"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public RequestPrivateGroupOnly(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for RequestPrivateGroupOnly OptionsIn
            /// </summary>
            public const string Keyword = "requestPrivateGroupOnly";

            /// <summary>
            /// The option to NOT request private group data only 
            /// </summary>
            public static readonly RequestPrivateGroupOnly False = new RequestPrivateGroupOnly("false");

            /// <summary>
            /// The option to request private group data only 
            /// </summary>
            public static readonly RequestPrivateGroupOnly True = new RequestPrivateGroupOnly("true");

            /// <summary>
            /// Gets a collection of RequestObjectSelectionCapability option values.
            /// </summary>
            /// <returns>A collection of all RequestObjectSelectionCapability option values.</returns>
            public static IEnumerable<RequestPrivateGroupOnly> GetValues()
            {
                yield return False;
                yield return True;
            }
        }

        /// <summary>
        /// Defines a RequestLatestValues OptionIn for a given value.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class RequestLatestValues : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.RequestLatestValues"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public RequestLatestValues(int value) : base(Keyword, value.ToString()) { }

            /// <summary>
            /// The keyword for RequestLatestValues OptionsIn
            /// </summary>
            public const string Keyword = "requestLatestValues";

            /// <summary>
            /// Creates a RequestLatestValues object for the specified value.
            /// </summary>
            /// <param name="value">The value for RequestLatestValues.</param>
            /// <returns></returns>
            public static RequestLatestValues Eq(int value)
            {
                return new RequestLatestValues(value);
            }
        }

        /// <summary>
        /// Defines the choices for the CascadedDelete configuration option value.
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class CascadedDelete : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.CascadedDelete"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public CascadedDelete(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for CascadeDelete OptionsIn
            /// </summary>
            public const string Keyword = "cascadedDelete";

            /// <summary>
            /// The option to turn off cascade delete
            /// </summary>
            public static readonly CascadedDelete False = new CascadedDelete("false");

            /// <summary>
            /// The option to turn on cascade delete
            /// </summary>
            public static readonly CascadedDelete True = new CascadedDelete("true");

            /// <summary>
            /// Gets a collection of CascadedDelete option values.
            /// </summary>
            /// <returns>A collection of all CascadedDelete option values.</returns>
            public static IEnumerable<CascadedDelete> GetValues()
            {
                yield return False;
                yield return True;
            }
        }

        /// <summary>
        /// Defines the choices for the IntervalRangeInclusion configuration option value. (applies only to mudlog data objects)
        /// </summary>
        /// <seealso cref="PDS.WITSMLstudio.OptionsIn" />
        public class IntervalRangeInclusion : OptionsIn
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PDS.WITSMLstudio.OptionsIn.IntervalRangeInclusion"/> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public IntervalRangeInclusion(string value) : base(Keyword, value) { }

            /// <summary>
            /// The keyword for IntervalRangeInclusion OptionsIn
            /// </summary>
            public const string Keyword = "intervalRangeInclusion";

            /// <summary>
            /// The nodes will be included if the interval minimum is within the range. (applies only to mudlog data objects)
            /// </summary>
            public static readonly IntervalRangeInclusion MinimumPoint = new IntervalRangeInclusion("minimum-point");

            /// <summary>
            /// The nodes will be included if the whole interval is within the range. (applies only to mudlog data objects)
            /// </summary>
            public static readonly IntervalRangeInclusion WholeInterval = new IntervalRangeInclusion("whole-interval");

            /// <summary>
            /// The nodes will be included if any part of the interval overlaps the range. (applies only to mudlog data objects)
            /// </summary>
            public static readonly IntervalRangeInclusion AnyPart = new IntervalRangeInclusion("any-part");

            /// <summary>
            /// Gets a collection of IntervalRangeInclusion option values.
            /// </summary>
            /// <returns>A collection of all IntervalRangeInclusion option values.</returns>
            public static IEnumerable<IntervalRangeInclusion> GetValues()
            {
                yield return MinimumPoint;
                yield return WholeInterval;
                yield return AnyPart;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsIn"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public OptionsIn(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Gets the option's key.
        /// </summary>
        /// <value>The option key.</value>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the option's value.
        /// </summary>
        /// <value>The option value.</value>
        public string Value { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0}={1}", Key, Value);
        }

        /// <summary>
        /// Determines whether the specified value is equal to the current value, ignoring case.
        /// </summary>
        /// <param name="value">The other value.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(string value)
        {
            return Value.EqualsIgnoreCase(value);
        }

        /// <summary>
        /// Parses the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>A collection of name-value pairs.</returns>
        public static Dictionary<string, string> Parse(string options)
        {
            _log.DebugFormat("Parsing OptionsIn: {0}", options);

            if (string.IsNullOrWhiteSpace(options))
            {
                return new Dictionary<string, string>(0);
            }

            return WebUtility
                .UrlDecode(options)
                .Split(';')
                .Select(x => x.Split('='))
                .ToLookup(x => x.First(), x => x.Last())
                .ToDictionary(x => x.Key, x => x.First());
        }

        /// <summary>
        /// Gets the value for the specified option.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The option value, if specified; otherwise, the default value.</returns>
        public static string GetValue(Dictionary<string, string> options, OptionsIn defaultValue)
        {
            _log.DebugFormat("Getting OptionsIn value: {0}", defaultValue?.Key);

            if (defaultValue == null) return null;
            string value;

            if (!options.TryGetValue(defaultValue.Key, out value))
                value = defaultValue.Value;

            return value;
        }

        /// <summary>
        /// Concatenates the specified options separated by a semicolon. 
        /// </summary>
        /// <param name="options">The OptionsIn params.</param>
        /// <returns>A concatenated list of OptionsIn strings separated by a semicolon.</returns>
        public static string Join(params OptionsIn[] options)
        {
            return string.Join(";", options.Select(x => x));
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="OptionsIn"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(OptionsIn option)
        {
            return option?.ToString();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="OptionsIn"/> to <see cref="Dictionary{String, String}"/>.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Dictionary<string, string>(OptionsIn option)
        {
            return new Dictionary<string, string>()
            {
                { option.Key, option.Value }
            };
        }
    }
}
