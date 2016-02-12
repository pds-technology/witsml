namespace PDS.Witsml
{
    public struct ObjectName
    {
        private readonly string _value;

        public ObjectName(string name, string version)
        {
            Name = name;
            Version = version;

            _value = string.Format("{0}_{1}", Name, Version.Replace(".", string.Empty));
        }

        public string Name { get; private set; }

        public string Version { get; private set; }

        public override string ToString()
        {
            return _value;
        }

        public static implicit operator string(ObjectName objectName)
        {
            return objectName.ToString();
        }
    }
}
