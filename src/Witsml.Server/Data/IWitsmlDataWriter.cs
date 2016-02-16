namespace PDS.Witsml.Server.Data
{
    public interface IWitsmlDataWriter
    {
        /// <summary>
        /// Adds a WITSML object into data store
        /// </summary>
        /// <param name="witsmlType">WITSML data-object type</param>
        /// <param name="xml">WITSML data-object to be added</param>
        /// <param name="options">Options sent by client</param>
        /// <param name="capabilities">Client’s Capabilities Object (capClient)</param>
        /// <returns>A WITSML result object that includes return code and/or message</returns>
        WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities);

        WitsmlResult UpdateInStore(string witsmlType, string xml, string options, string capabilities);

        WitsmlResult DeleteFromStore(string witsmlType, string xml, string options, string capabilities);
    }
}
