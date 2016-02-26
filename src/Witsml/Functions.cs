namespace PDS.Witsml
{
    /// <summary>
    /// Enumeration of WITSML API methods.
    /// </summary>
    public enum Functions
    {
        // SOAP
        GetCap,
        GetVersion,
        GetFromStore,
        AddToStore,
        UpdateInStore,
        DeleteFromStore,

        // ETP
        GetObject,
        PutObject,
        DeleteObject
    }
}
