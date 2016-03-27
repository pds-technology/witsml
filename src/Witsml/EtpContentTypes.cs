using Energistics.Datatypes;

namespace PDS.Witsml
{
    /// <summary>
    /// Defines the supported list of ETP content types.
    /// </summary>
    public static class EtpContentTypes
    {
        public static readonly EtpContentType Prodml200 = new EtpContentType("application/x-prodml+xml;version=2.0;");

        public static readonly EtpContentType Resqml200 = new EtpContentType("application/x-resqml+xml;version=2.0;");
        public static readonly EtpContentType Resqml201 = new EtpContentType("application/x-resqml+xml;version=2.0.1;");

        public static readonly EtpContentType Witsml131 = new EtpContentType("application/x-witsml+xml;version=1.3.1.1;");
        public static readonly EtpContentType Witsml141 = new EtpContentType("application/x-witsml+xml;version=1.4.1.1;");
        public static readonly EtpContentType Witsml200 = new EtpContentType("application/x-witsml+xml;version=2.0;");
    }
}
