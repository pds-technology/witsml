namespace PDS.Witsml.Server.Data
{
    public interface IWitsmlDataWriter
    {
        /// <summary>
        /// WITSML AddToStore method Interface
        /// </summary>
        /// <param name="witsmlType">Input string that specifies WITSML data-object type</param>
        /// <param name="xml">Input string for the WITSML data-object to be added</param>
        /// <param name="options">Input string that specifies the options</param>
        /// <param name="capabilities">Input string that specifies the client’s Capabilities Object (capClient) to be sent to the server</param>
        /// <returns>A WITSML result object that includes return code and/or message</returns>
        WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities);

        WitsmlResult UpdateInStore(string witsmlType, string xml, string options, string capabilities);

        WitsmlResult DeleteFromStore(string witsmlType, string xml, string options, string capabilities);
    }
}
