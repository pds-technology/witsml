using System.Text.RegularExpressions;

namespace Energistics.Datatypes
{
    /// <summary>
    /// Represents a content type supported by the Energistics Transfer Protocol (ETP).
    /// </summary>
    public struct EtpContentType
    {
        private static readonly Regex _pattern = new Regex(@"^application/x\-(witsml|resqml|prodml|energyml)\+xml;version=([0-9.]+)((;)?|(;type=((obj_)?(\w+))(;)?)?)$");
        private static readonly string _baseFormat = "application/x-{0}+xml;version={1};";
        private static readonly string _typeFormat = "type=obj_{0};";
        private readonly Match _match;
        private string _contentType;

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpContentType"/> struct.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        public EtpContentType(string contentType)
        {
            _contentType = contentType;
            _match = _pattern.Match(contentType);
            IsValid = _match.Success;

            Family = GetValue(_match, 1);
            Version = GetValue(_match, 2);
            ObjectType = GetValue(_match, 6);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpContentType"/> struct.
        /// </summary>
        /// <param name="family">The ML family name.</param>
        /// <param name="version">The version.</param>
        public EtpContentType(string family, string version) : this(family, version, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EtpContentType"/> struct.
        /// </summary>
        /// <param name="family">The ML family name.</param>
        /// <param name="version">The version.</param>
        /// <param name="objectType">Type of the object.</param>
        public EtpContentType(string family, string version, string objectType)
        {
            _match = null;
            IsValid = true;

            Family = family;
            Version = version;
            ObjectType = objectType;

            _contentType = string.Format(_baseFormat, family, version) + FormatType(objectType, version);
        }

        /// <summary>
        /// Gets the ML family name.
        /// </summary>
        /// <value>The ML family.</value>
        public string Family { get; private set; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the type of the object.
        /// </summary>
        /// <value>The type of the object.</value>
        public string ObjectType { get; private set; }

        /// <summary>
        /// Returns true if a valid content type was specified.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a base content type.
        /// </summary>
        /// <value><c>true</c> if this instance is a base content type; otherwise, <c>false</c>.</value>
        public bool IsBaseType
        {
            get { return string.IsNullOrWhiteSpace(ObjectType); }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EtpContentType"/> based on the
        /// current ML family name, version number and the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>The new <see cref="EtpContentType"/> instance.</returns>
        public EtpContentType For(string objectType)
        {
            return new EtpContentType(Family, Version, objectType);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return _contentType;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="EtpContentType"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator string(EtpContentType contentType)
        {
            return contentType.ToString();
        }

        /// <summary>
        /// Gets the value contained within the specified match at the specified index.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <param name="index">The index.</param>
        /// <returns>The matched value found at the specified index.</returns>
        private static string GetValue(Match match, int index)
        {
            return match.Success && match.Groups.Count > index
                ? match.Groups[index].Value
                : null;
        }

        /// <summary>
        /// Formats the specified object type to match the ML version.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="version">The version.</param>
        /// <returns>The formatted object type.</returns>
        private static string FormatType(string objectType, string version)
        {
            if (string.IsNullOrWhiteSpace(objectType))
                return string.Empty;

            if (!version.Contains("_"))
            {
                System.Version ver;

                objectType = (System.Version.TryParse(version, out ver) && ver.Major < 2
                    ? objectType.Substring(0, 1).ToLowerInvariant()
                    : objectType.Substring(0, 1).ToUpperInvariant())
                    + objectType.Substring(1);
            }

            return string.Format(_typeFormat, objectType);
        }
    }
}
