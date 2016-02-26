using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Web;
using System.Xml.Linq;
using PDS.Framework;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Provides common validation for the main WITSML Store API methods.
    /// </summary>
    public abstract class WitsmlValidator
    {
        /// <summary>
        /// Validates the WITSML Store API request.
        /// </summary>
        /// <param name="providers">The capServer providers.</param>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="objectType">The type of the data object.</param>
        /// <param name="xml">The XML string for the data object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client's capabilities object (capClient).</param>
        /// <param name="version">The data schema version.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateRequest(IEnumerable<ICapServerProvider> providers, Functions function, string objectType, string xml, string options, string capabilities, out string version)
        {
            ValidateUserAgent(WebOperationContext.Current);
            ValidateInputTemplate(xml);

            var dataSchemaVersion = ObjectTypes.GetVersion(xml);
            ValidateDataSchemaVersion(dataSchemaVersion);

            var capServerProvider = providers.FirstOrDefault(x => x.DataSchemaVersion == dataSchemaVersion);
            if (capServerProvider == null)
            {
                throw new WitsmlException(ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + objectType + "; Version: " + dataSchemaVersion);
            }

            capServerProvider.ValidateRequest(function, objectType, xml, options, capabilities);
            version = dataSchemaVersion;
        }

        /// <summary>
        /// Determines whether the specified function is supported for the object type.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// true if the WITSML Store supports the function for the specified object type, otherwise, false
        /// </returns>
        public abstract bool IsSupported(Functions function, string objectType);

        /// <summary>
        /// Performs validation for the specified function and supplied parameters.
        /// </summary>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="objectType">The type of the data object.</param>
        /// <param name="xml">The XML string for the data object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client's capabilities object (capClient).</param>
        public virtual void ValidateRequest(Functions function, string objectType, string xml, string options, string capabilities)
        {
            ValidateObjectType(function, objectType, ObjectTypes.GetObjectType(xml));
            ValidatePluralRootElement(objectType, xml);
        }

        /// <summary>
        /// Validates the required User-Agent header is supplied by the client.
        /// </summary>
        /// <param name="context">The web operation context.</param>
        /// <exception cref="WitsmlException">Thrown if the User-Agent header is missing.</exception>
        public static void ValidateUserAgent(WebOperationContext context)
        {
            if (context != null && context.IncomingRequest != null && string.IsNullOrWhiteSpace(context.IncomingRequest.UserAgent))
            {
                throw new WitsmlException(ErrorCodes.MissingClientUserAgent);
            }
        }

        /// <summary>
        /// Validates the required WITSML input template.
        /// </summary>
        /// <param name="xml">The XML input template.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateInputTemplate(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new WitsmlException(ErrorCodes.MissingInputTemplate);
            }
        }

        /// <summary>
        /// Validates the required data schema version has been specified.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateDataSchemaVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new WitsmlException(ErrorCodes.MissingDataSchemaVersion);
            }
        }

        /// <summary>
        /// Validates the required plural root element.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="xml">The XML.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidatePluralRootElement(string objectType, string xml)
        {
            var objectGroupType = ObjectTypes.GetObjectGroupType(xml);
            var pluralObjectType = objectType + "s";

            if (objectGroupType != pluralObjectType)
            {
                throw new WitsmlException(ErrorCodes.MissingPluralRootElement);
            }
        }

        /// <summary>
        /// Validates the required singular root element.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="xml">The XML.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateSingleChildElement(string objectType, string xml)
        {
            var doc = XDocument.Parse(xml);
            if (doc.Root.Elements().Count() != 1)
            {
                throw new WitsmlException(ErrorCodes.InputTemplateMultipleDataObjects);
            }
        }

        /// <summary>
        /// Validates the options are supported.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="keywords">The supported keywords.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateKeywords(Dictionary<string, string> options, params string[] keywords)
        {
            foreach (var option in options.Where(x => !keywords.Contains(x.Key)))
            {
                throw new WitsmlException(ErrorCodes.KeywordNotSupportedByFunction, "Option not supported: " + option.Key);
            }
        }

        /// <summary>
        /// Validates the supported compressionMethod configuration option.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="compressionMethod">The compression method.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateCompressionMethod(Dictionary<string, string> options, string compressionMethod)
        {
            string optionValue;
            if (!options.TryGetValue(OptionsIn.CompressionMethod.Keyword, out optionValue))
            {
                return;
            }

            // Validate compression method value
            if (!optionValue.EqualsIgnoreCase(OptionsIn.CompressionMethod.None.Value) &&
                !optionValue.EqualsIgnoreCase(OptionsIn.CompressionMethod.Gzip.Value))
            {
                throw new WitsmlException(ErrorCodes.InvalidKeywordValue);
            }

            // Validate compression method is supported
            if (!optionValue.EqualsIgnoreCase(OptionsIn.CompressionMethod.None.Value) &&
                !optionValue.EqualsIgnoreCase(compressionMethod))
            {
                throw new WitsmlException(ErrorCodes.KeywordNotSupportedByServer);
            }
        }

        /// <summary>
        /// Validates the required WITSML object type parameter for the WMLS_AddToStore method.
        /// </summary>
        /// <param name="function">The WITSML Store API function.</param>
        /// <param name="objectType">The type of data object.</param>
        /// <param name="xmlType">The type of data object in the XML.</param>
        /// <exception cref="WitsmlException"></exception>
        protected void ValidateObjectType(Functions function, string objectType, string xmlType)
        {
            if (string.IsNullOrWhiteSpace(objectType))
            {
                throw new WitsmlException(ErrorCodes.MissingWMLtypeIn);
            }

            // Not sure why these are only checked for AddToStore?
            if (function == Functions.AddToStore)
            {
                if (!objectType.Equals(xmlType))
                {
                    throw new WitsmlException(ErrorCodes.DataObjectTypesDontMatch);
                }

                if (!IsSupported(function, objectType))
                {
                    throw new WitsmlException(ErrorCodes.DataObjectTypeNotSupported);
                }
            }
        }
    }
}
