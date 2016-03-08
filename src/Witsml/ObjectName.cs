namespace PDS.Witsml
{
    /// <summary>
    /// Represents a version-qualified data object name.
    /// </summary>
    public struct ObjectName
    {
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectName"/> struct.
        /// </summary>
        /// <param name="version">The version.</param>
        public ObjectName(string version) : this(string.Empty, version)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectName"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        public ObjectName(string name, string version)
        {
            Name = name;
            Version = version;

            _value = string.Format("{0}_{1}", Name, Version.Replace(".", string.Empty).Substring(0, 2));
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; private set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return _value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ObjectName"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(ObjectName objectName)
        {
            return objectName.ToString();
        }
    }
}
