using System.Collections.Generic;
using System.Linq;

namespace PDS.Witsml
{
    public class OptionsIn
    {
        public class DataVersion : OptionsIn
        {
            public DataVersion(string value) : base(Keyword, value) { }

            public const string Keyword = "dataVersion";

            public static readonly DataVersion Version131 = new DataVersion("1.3.1.1");
            public static readonly DataVersion Version141 = new DataVersion("1.4.1.1");
        }

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
        }

        public OptionsIn(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}={1}", Key, Value);
        }

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

        public static string GetValue(Dictionary<string, string> options, OptionsIn defaultValue)
        {
            string value;
            if (!options.TryGetValue(defaultValue.Key, out value))
            {
                value = defaultValue.Value;
            }
            return value;
        }

        public static implicit operator Dictionary<string, string>(OptionsIn option)
        {
            return new Dictionary<string, string>()
            {
                { option.Key, option.Value }
            };
        }
    }
}
