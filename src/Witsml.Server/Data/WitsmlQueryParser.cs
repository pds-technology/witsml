using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    public class WitsmlQueryParser
    {
        private readonly XNamespace _namespace;
        private readonly XDocument _document;

        public WitsmlQueryParser(RequestContext context)
        {
            Context = context;
            Options = OptionsIn.Parse(context.Options);
            _document = XDocument.Parse(context.Xml);
            _namespace = _document.Root.GetDefaultNamespace();
        }

        public RequestContext Context { get; private set; }

        public Dictionary<string, string> Options { get; private set; }

        public string ReturnElements()
        {
            return OptionsIn.GetValue(Options, OptionsIn.ReturnElements.Requested);
        }

        /// <summary>
        /// Requests the object selection capability.
        /// </summary>
        /// <returns>The capability value.</returns>
        public string RequestObjectSelectionCapability()
        {
            return OptionsIn.GetValue(Options, OptionsIn.RequestObjectSelectionCapability.None);
        }

        /// <summary>
        /// Requests the private group only.
        /// </summary>
        /// <returns></returns>
        public bool RequestPrivateGroupOnly()
        {
            string value = OptionsIn.GetValue(Options, OptionsIn.RequestPrivateGroupOnly.False);
            bool result;
            bool success = bool.TryParse(value, out result);
            if (success)
                return result;
            else
                return false;
        }

        public IEnumerable<XElement> Elements()
        {
            return _document.Root.Elements(_namespace + Context.ObjectType);
        }

        public XElement Element()
        {
            return Elements().FirstOrDefault();
        }

        public String Attribute(string name)
        {
            if (HasAttribute(name))
            {
                return (String)Element().Attribute(name);
            }
            return null;
        }

        public bool HasAttribute(string name)
        {
            var element = Element();
            return element != null && element.Attribute(name) != null;
        }

        public bool Contains(string name)
        {
            return Element().Elements(_namespace + name).Any();
        }

        public XElement Property(string name)
        {
            return Element().Elements(_namespace + name).FirstOrDefault();
        }

        public bool HasElements(string name)
        {
            return HasElements(Element(), name);
        }

        public bool HasElements(XElement element, string name)
        {
            return element != null &&
                element.Elements(_namespace + name) != null &&
                element.Elements(_namespace + name).Any();
        }

        public string PropertyValue(string name)
        {
            if (!HasElements(name))
            {
                return null;
            }
            return PropertyValue(Element(), name);
        }

        public string PropertyValue(XElement element, string name)
        {
            if (!HasElements(element, name))
            {
                return null;
            }
            return element
                .Elements(_namespace + name)
                .Select(e => e.Value)
                .FirstOrDefault();
        }

        public string PropertyAttribute(string name, string attribute)
        {
            if (!HasElements(name))
            {
                return null;
            }
            return Element()
                .Elements(_namespace + name)
                .Select(e => (String)e.Attribute(attribute))
                .FirstOrDefault();
        }
    }
}
