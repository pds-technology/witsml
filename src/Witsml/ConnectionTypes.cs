using System.Collections.Generic;

namespace PDS.Witsml
{
    public class ConnectionTypes
    {
        public static readonly ConnectionTypes Etp = new ConnectionTypes("ETP");
        public static readonly ConnectionTypes Witsml = new ConnectionTypes("WITSML");

        public ConnectionTypes(string value)
        {
            Key = string.Format("connectionTypes-{0}", value);
            Value = value;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }


        public static implicit operator Dictionary<string, string>(ConnectionTypes connectionType)
        {
            return new Dictionary<string, string>()
            {
                { connectionType.Key, connectionType.Value }
            };
        }
    }
}
