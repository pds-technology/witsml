namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Date writer that encapsulates add, update, and delete service calls for WITSML query
    /// </summary>
    public interface IWitsmlDataWriter
    {
        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="witsmlType">Type of WITSML data-object.</param>
        /// <param name="xml">The XML string for the data-object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// A WITSML result that includes return code and/or message.
        /// </returns>
        WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities);

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <param name="witsmlType">Type of WITSML data-object.</param>
        /// <param name="xml">The XML string for the data-object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// A WITSML result that includes return code and/or message.
        /// </returns>
        WitsmlResult UpdateInStore(string witsmlType, string xml, string options, string capabilities);

        /// <summary>
        /// Deletes or partially update object from store.
        /// </summary>
        /// <param name="witsmlType">Type of WITSML data-object.</param>
        /// <param name="xml">The XML string for the delete query.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// A WITSML result that includes return code and/or message.
        /// </returns>
        WitsmlResult DeleteFromStore(string witsmlType, string xml, string options, string capabilities);
    }
}
