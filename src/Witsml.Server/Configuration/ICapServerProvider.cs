namespace PDS.Witsml.Server.Configuration
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
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="objectType">The type of the data object.</param>
        /// <returns>true if the WITSML Store supports the function for the specified object type, otherwise, false</returns>
        bool IsSupported(Functions function, string objectType);

        /// <summary>
        /// Performs validation for the specified function and supplied parameters.
        /// </summary>
        /// <param name="context">The request context.</param>
        void ValidateRequest(RequestContext context);
    }
}
