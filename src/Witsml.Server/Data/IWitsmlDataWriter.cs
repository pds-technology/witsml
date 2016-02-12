namespace PDS.Witsml.Server.Data
{
    public interface IWitsmlDataWriter
    {
        WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities);

        WitsmlResult UpdateInStore(string witsmlType, string xml, string options, string capabilities);

        WitsmlResult DeleteFromStore(string witsmlType, string xml, string options, string capabilities);
    }
}
