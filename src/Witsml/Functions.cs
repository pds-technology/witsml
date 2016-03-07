using System.ComponentModel;

namespace PDS.Witsml
{
    /// <summary>
    /// Enumeration of WITSML API methods.
    /// </summary>
    public enum Functions
    {
        // SOAP
        [Description("Get Capabilities")]
        GetCap,
        [Description("Get Version")]
        GetVersion,
        [Description("Get From Store")]
        GetFromStore,
        [Description("Add To Store")]
        AddToStore,
        [Description("Update In Store")]
        UpdateInStore,
        [Description("Delete From Store")]
        DeleteFromStore,

        // ETP
        [Description("Get Object")]
        GetObject,
        [Description("Put Object")]
        PutObject,
        [Description("Delete Object")]
        DeleteObject
    }
}
