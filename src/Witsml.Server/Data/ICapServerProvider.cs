namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Defines properties and methods used for retrieving WITSML Store capabilities. 
    /// </summary>
    public interface ICapServerProvider
    {
        /// <summary>
        /// Gets the data schema version.
        /// </summary>
        /// <value>The data schema version.</value>
        string DataSchemaVersion { get; }

        /// <summary>
        /// Returns the server capabilities object as XML.
        /// </summary>
        /// <returns>A capServers object as an XML string.</returns>
        string ToXml();

        /// <summary>
        /// Determines whether the specified function is supported for the object type.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>true if the WITSML Store supports the function for the specified object type, otherwise, false</returns>
        bool IsSupported(Functions function, string objectType);
    }
}
