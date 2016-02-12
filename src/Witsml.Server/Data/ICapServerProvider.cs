namespace PDS.Witsml.Server.Data
{
    public interface ICapServerProvider
    {
        string DataSchemaVersion { get; }

        string ToXml();
    }
}
