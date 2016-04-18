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

using System.Collections.Generic;
using System.Linq;
using PDS.Framework;

namespace PDS.Witsml
{
    /// <summary>
    /// Defines the set of well known configuration options that can be used 
    /// when requesting information from a WITSML store.
    /// </summary>
    public class OptionsIn
    {
        /// <summary>
        /// Defines the list of supported data schema versions.
        /// </summary>
        /// <seealso cref="PDS.Witsml.OptionsIn" />
        public class DataVersion : OptionsIn
        {
            public DataVersion(string value) : base(Keyword, value) { }

            public const string Keyword = "dataVersion";

            public static readonly DataVersion Version131 = new DataVersion("1.3.1.1");
            public static readonly DataVersion Version141 = new DataVersion("1.4.1.1");
            public static readonly DataVersion Version200 = new DataVersion("2.0");
        }

        /// <summary>
        /// Defines the list of returnElements configuration option values.
        /// </summary>
        /// <seealso cref="PDS.Witsml.OptionsIn" />
        public class ReturnElements : OptionsIn
        {
            public ReturnElements(string value) : base(Keyword, value) { }

            public const string Keyword = "returnElements";

            public static readonly ReturnElements All = new ReturnElements("all");
            public static readonly ReturnElements IdOnly = new ReturnElements("id-only");
            public static readonly ReturnElements HeaderOnly = new ReturnElements("header-only");
            public static readonly ReturnElements DataOnly = new ReturnElements("data-only");
            public static readonly ReturnElements StationLocationOnly = new ReturnElements("station-location-only");
            public static readonly ReturnElements LatestChangeOnly = new ReturnElements("latest-change-only");
            public static readonly ReturnElements Requested = new ReturnElements("requested");

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
        /// Defines the list of compressionMethod configuration option values.
        /// </summary>
        /// <seealso cref="PDS.Witsml.OptionsIn" />
        public class CompressionMethod : OptionsIn
        {
            public CompressionMethod(string value) : base(Keyword, value) { }

            public const string Keyword = "compressionMethod";

            public static readonly CompressionMethod None = new CompressionMethod("None");
            public static readonly CompressionMethod Gzip = new CompressionMethod("gzip");
        }

        /// <summary>
        /// Defines the choice of requestObjectSelectionCapability configuration option values.
        /// </summary>
        /// <seealso cref="PDS.Witsml.OptionsIn" />
        public class RequestObjectSelectionCapability : OptionsIn
        {
            public RequestObjectSelectionCapability(string value) : base(Keyword, value) { }

            public const string Keyword = "requestObjectSelectionCapability";

            public static readonly RequestObjectSelectionCapability None = new RequestObjectSelectionCapability("none");
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
        /// <seealso cref="PDS.Witsml.OptionsIn" />
        public class RequestPrivateGroupOnly : OptionsIn
        {
            public RequestPrivateGroupOnly(string value) : base(Keyword, value) { }

            public const string Keyword = "requestPrivateGroupOnly";

            public static readonly RequestPrivateGroupOnly False = new RequestPrivateGroupOnly("false");
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

        public class RequestLatestValues : OptionsIn
        {
            public RequestLatestValues(int value) : base(Keyword, value.ToString()) { }

            public const string Keyword = "requestLatestValues";
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
            if (string.IsNullOrWhiteSpace(options))
            {
                return new Dictionary<string, string>(0);
            }

            return options.Split(';')
                .Select(x => x.Split('='))
                .ToDictionary(x => x.First(), x => x.Last());
        }

        /// <summary>
        /// Gets the value for the specified option.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The option value, if specified; otherwise, the default value.</returns>
        public static string GetValue(Dictionary<string, string> options, OptionsIn defaultValue)
        {
            string value;
            if (!options.TryGetValue(defaultValue.Key, out value))
            {
                value = defaultValue.Value;
            }
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
            return option.ToString();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="OptionsIn"/> to <see cref="Dictionary{System.String, System.String}"/>.
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
