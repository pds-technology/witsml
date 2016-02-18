using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data.CapServers
{
    public abstract class CapServerProvider<T> : ICapServerProvider
    {
        private T _capServer;
        private XDocument _capServerDoc;
        private string _capServerXml;

        public abstract string DataSchemaVersion { get; }

        public string ToXml()
        {
            if (!string.IsNullOrWhiteSpace(_capServerXml))
            {
                return _capServerXml;
            }

            var capServer = GetCapServer();

            if (capServer != null)
            {
                _capServerXml = EnergisticsConverter.ObjectToXml(capServer);
            }

            return _capServerXml;
        }

        public bool IsSupported(Functions function, string objectType)
        {
            var capServerDoc = GetCapServerDocument();
            var ns = XNamespace.Get(capServerDoc.Root.CreateNavigator().GetNamespace(string.Empty));

            return capServerDoc.Descendants(ns + "dataObject")
                .Where(x => x.Value == objectType && x.Parent.Attribute("name").Value == "WMLS_" + function)
                .Any();
        }

        protected abstract T CreateCapServer();

        private T GetCapServer()
        {
            if (_capServer != null)
            {
                return _capServer;
            }

            _capServer = CreateCapServer();

            return _capServer;
        }

        private XDocument GetCapServerDocument()
        {
            if (_capServerDoc != null)
            {
                return _capServerDoc;
            }

            _capServerDoc = XDocument.Parse(ToXml());

            return _capServerDoc;
        }
    }
}
