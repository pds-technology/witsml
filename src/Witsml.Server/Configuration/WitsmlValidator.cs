//----------------------------------------------------------------------- 
// PDS.Witsml.Server, 2016.1
//
// Copyright 2016 Petrotechnical Data Systems
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.Web;
using System.Xml.Linq;
using Energistics.DataAccess.Validation;
using log4net;
using PDS.Framework;

namespace PDS.Witsml.Server.Configuration
{
    /// <summary>
    /// Provides common validation for the main WITSML Store API methods.
    /// </summary>
    public abstract class WitsmlValidator
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlValidator));

        /// <summary>
        /// Validates the WITSML Store API request.
        /// </summary>
        /// <param name="providers">The capServer providers.</param>
        /// <param name="version">The data schema version.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateRequest(IEnumerable<ICapServerProvider> providers, out string version)
        {
            var context = WitsmlOperationContext.Current;
            _log.DebugFormat("Validating WITSML request for {0}", context.Request.ObjectType);

            ValidateUserAgent(WebOperationContext.Current);
            context.Document = ValidateInputTemplate(context.Request.Xml);

            var dataSchemaVersion = ObjectTypes.GetVersion(context.Document.Root);
            ValidateDataSchemaVersion(dataSchemaVersion);

            var capServerProvider = providers.FirstOrDefault(x => x.DataSchemaVersion == dataSchemaVersion);
            if (capServerProvider == null)
            {
                throw new WitsmlException(ErrorCodes.DataObjectNotSupported,
                    "WITSML object type not supported: " + context.Request.ObjectType + "; Version: " + dataSchemaVersion);
            }

            capServerProvider.ValidateRequest();
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
        public virtual void ValidateRequest()
        {
            var context = WitsmlOperationContext.Current;
            ValidateNamespace(context.Document);

            // Skip any documentInfo elements
            context.Document.Root?.Elements()
                .Where(x => !ObjectTypes.DocumentInfo.EqualsIgnoreCase(x.Name.LocalName))
                .ForEach(e => ValidateObjectType(context.Request.Function, context.Request.ObjectType, e.Name.LocalName));

            ValidatePluralRootElement(context.Request.ObjectType, context.Document);
        }

        /// <summary>
        /// Validates the required User-Agent header is supplied by the client.
        /// </summary>
        /// <param name="context">The web operation context.</param>
        /// <exception cref="WitsmlException">Thrown if the User-Agent header is missing.</exception>
        public static void ValidateUserAgent(WebOperationContext context)
        {
            _log.DebugFormat("Validating user agent: {0}", context?.IncomingRequest?.UserAgent);

            if (context?.IncomingRequest != null && string.IsNullOrWhiteSpace(context.IncomingRequest.UserAgent))
            {
                throw new WitsmlException(ErrorCodes.MissingClientUserAgent);
            }
        }

        /// <summary>
        /// Validates the required WITSML input template.
        /// </summary>
        /// <param name="xml">The XML input template.</param>
        /// <exception cref="WitsmlException"></exception>
        public static XDocument ValidateInputTemplate(string xml)
        {
            _log.Debug("Validating WITSML input template.");

            if (string.IsNullOrWhiteSpace(xml))
            {
                throw new WitsmlException(ErrorCodes.MissingInputTemplate);
            }

            XDocument doc = WitsmlParser.Parse(xml);

            if (string.IsNullOrEmpty(GetNamespace(doc)))
            {
                throw new WitsmlException(ErrorCodes.MissingDefaultWitsmlNamespace);
            }

            return doc;
        }

        /// <summary>
        /// Validates the required data schema version has been specified.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateDataSchemaVersion(string version)
        {
            _log.DebugFormat("Validating data schema version: {0}", version);

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new WitsmlException(ErrorCodes.MissingDataSchemaVersion);
            }
        }

        /// <summary>
        /// Validates the required plural root element.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="document">The XML document.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidatePluralRootElement(string objectType, XDocument document)
        {
            _log.DebugFormat("Validating plural root element for {0}", objectType);

            var objectGroupType = ObjectTypes.GetObjectGroupType(document.Root);
            var pluralObjectType = objectType + "s";

            if (objectGroupType != pluralObjectType)
            {
                throw new WitsmlException(ErrorCodes.MissingPluralRootElement);
            }
        }

        /// <summary>
        /// Validates the non-empty root element.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="document">The XML document.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateEmptyRootElement(string objectType, XDocument document)
        {
            _log.DebugFormat("Validating empty root element for {0}", objectType);

            if (!document.Root.Elements().Any())
            {
                throw new WitsmlException(WitsmlOperationContext.Current.Request.Function.GetNonConformingErrorCode());
            }
        }

        /// <summary>
        /// Validates the required singular root element.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="document">The XML document.</param>
        /// <exception cref="WitsmlException"></exception>
        public static void ValidateSingleChildElement(string objectType, XDocument document)
        {
            _log.DebugFormat("Validating single child element for {0}", objectType);

            if (document.Root.Elements().Count() > 1)
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
            _log.Debug("Validating keywords for OptionsIn");

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
            _log.DebugFormat("Validating compression method: {0}", compressionMethod);

            string optionValue;
            if (!options.TryGetValue(OptionsIn.CompressionMethod.Keyword, out optionValue))
            {
                return;
            }

            // Validate compression method value
            if (!OptionsIn.CompressionMethod.None.Equals(optionValue) &&
                !OptionsIn.CompressionMethod.Gzip.Equals(optionValue))
            {
                throw new WitsmlException(ErrorCodes.InvalidKeywordValue);
            }

            // Validate compression method is supported
            if (!OptionsIn.CompressionMethod.None.Equals(optionValue) &&
                !optionValue.EqualsIgnoreCase(compressionMethod))
            {
                throw new WitsmlException(ErrorCodes.KeywordNotSupportedByServer);
            }
        }

        /// <summary>
        /// Validates the cascaded delete.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cascadedDelete">if set to true cascade delete is supported.</param>
        public static void ValidateCascadedDelete(Dictionary<string, string> options, bool? cascadedDelete)
        {
            _log.DebugFormat("Validating compression method: {0}", cascadedDelete);

            string optionValue;
            if (!options.TryGetValue(OptionsIn.CascadedDelete.Keyword, out optionValue))
            {
                return;
            }

            // Validate CascadedDelete value
            if (!OptionsIn.CascadedDelete.True.Equals(optionValue) &&
                !OptionsIn.CascadedDelete.False.Equals(optionValue))
            {
                throw new WitsmlException(ErrorCodes.InvalidKeywordValue);
            }

            // Validate CascadedDelete is supported
            if (!OptionsIn.CascadedDelete.False.Equals(optionValue) &&
                !optionValue.EqualsIgnoreCase(cascadedDelete?.ToString().ToLower()))
            {
                throw new WitsmlException(ErrorCodes.KeywordNotSupportedByServer);
            }
        }

        /// <summary>
        /// Validates the request maximum return nodes.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="WitsmlException"></exception>
        public void ValidateRequestMaxReturnNodes(Dictionary<string, string> options)
        {
            _log.Debug("Validating max return nodes.");

            string optionValue;
            if (!options.TryGetValue(OptionsIn.MaxReturnNodes.Keyword, out optionValue))
            {
                return;
            }

            // Validate value 
            int maxReturnNodes;
            if (!int.TryParse(optionValue, out maxReturnNodes) || maxReturnNodes <= 0)
            {
                throw new WitsmlException(ErrorCodes.InvalidMaxReturnNodes);
            }
        }

        /// <summary>
        /// Validates the requestObjectSelectionCapability option.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="document">The document.</param>
        /// <exception cref="WitsmlException">
        /// </exception>
        public void ValidateRequestObjectSelectionCapability(Dictionary<string, string> options, string objectType, XDocument document)
        {
            _log.DebugFormat("Validating request object selection capability for {0}", objectType);

            string optionValue;
            if (!options.TryGetValue(OptionsIn.RequestObjectSelectionCapability.Keyword, out optionValue))
            {
                return;
            }

            // Validate value 
            if (!OptionsIn.RequestObjectSelectionCapability.None.Equals(optionValue) &&
                !OptionsIn.RequestObjectSelectionCapability.True.Equals(optionValue))
            {
                throw new WitsmlException(ErrorCodes.InvalidKeywordValue);
            }
          
            if (OptionsIn.RequestObjectSelectionCapability.True.Equals(optionValue))
            {
                // No other options should be specified if value is true
                if (options.Count != 1)
                {
                    throw new WitsmlException(ErrorCodes.InvalidOptionsInCombination);
                }

                ValidateMinimumQueryTemplate(objectType, document);
            }
        }

        private static void ValidateMinimumQueryTemplate(string objectType, XDocument document)
        {
            _log.DebugFormat("Validating minimum query template for {0}", objectType);

            XElement root = document.Root;

            if ( !(root.Elements().Count() == 1 &&
                   root.Elements().All(x =>  x.Name.LocalName.Equals(objectType) && !x.HasAttributes && !x.HasElements )) )
            {
                throw new WitsmlException(ErrorCodes.InvalidMinimumQueryTemplate);
            }
        }

        /// <summary>
        /// Validates the returnElements option.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <exception cref="WitsmlException">
        /// </exception>
        public void ValidateReturnElements(Dictionary<string, string> options, string objectType)
        {
            _log.DebugFormat("Validating return elements for {0}", objectType);

            string optionValue;
            if (!options.TryGetValue(OptionsIn.ReturnElements.Keyword, out optionValue))
            {
                return;
            }

            // Validate value 
            if (!OptionsIn.ReturnElements.GetValues().Any(x => x.Equals(optionValue)))
            {
                throw new WitsmlException(ErrorCodes.InvalidKeywordValue);
            }

            // HeaderOnly and DataOnly options are for growing data object only
            if ((OptionsIn.ReturnElements.HeaderOnly.Equals(optionValue) || OptionsIn.ReturnElements.DataOnly.Equals(optionValue))
                && !ObjectTypes.IsGrowingDataObject(objectType))
            {
                throw new WitsmlException(ErrorCodes.InvalidOptionForGrowingObjectOnly);
            }

            // Latest-Change-Only option is for ChangeLog only
            if ((OptionsIn.ReturnElements.LatestChangeOnly.Equals(optionValue) && !objectType.Equals(ObjectTypes.ChangeLog)))
            {
                throw new WitsmlException(ErrorCodes.InvalidOptionForChangeLogOnly);
            }

            // Station-Location-Only option is for Trajectory only
            if ((OptionsIn.ReturnElements.StationLocationOnly.Equals(optionValue) && !objectType.Equals(ObjectTypes.Trajectory)))
            {
                throw new WitsmlException(ErrorCodes.InvalidOptionForGrowingObjectOnly);
            }
        }

        /// <summary>
        /// Validates the selection criteria.
        /// </summary>
        /// <param name="document">The queryIn XML document.</param>
        /// <param name="allowEmptyRecurringElements">If set to true then allow empty recurring elements.</param>
        public static void ValidateSelectionCriteria(XDocument document, bool allowEmptyRecurringElements = false)
        {
            _log.Debug("Validating selection criteria.");

            var entities = document.Root.Elements();

            foreach (var entity in entities)
            {
                ValidateSelectionCriteriaForAnEntity(entity, allowEmptyRecurringElements);
            }
        }

        /// <summary>
        /// Process the validation results to raise a <see cref="WitsmlException" />.
        /// </summary>
        /// <param name="function">The WITSML API function.</param>
        /// <param name="results">The results.</param>
        public static void ValidateResults(Functions function, IList<ValidationResult> results)
        {
            if (!results.Any()) return;

            ErrorCodes errorCode;
            var witsmlValidationResult = results.OfType<WitsmlValidationResult>().FirstOrDefault();

            if (witsmlValidationResult != null)
            {
                throw new WitsmlException((ErrorCodes)witsmlValidationResult.ErrorCode);
            }

            if (Enum.TryParse(results.First().ErrorMessage, out errorCode))
            {
                throw new WitsmlException(errorCode);
            }

            throw new WitsmlException(function.GetNonConformingErrorCode(),
                string.Join("; ", results.Select(x => x.ErrorMessage)));
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
            _log.DebugFormat("Validating object type for {0}", objectType);

            if (string.IsNullOrWhiteSpace(objectType))
            {
                throw new WitsmlException(ErrorCodes.MissingWmlTypeIn);
            }

            // Different error codes return between AddToStore and other functions
            if (!objectType.Equals(xmlType))
            {
                if (function == Functions.AddToStore)
                    throw new WitsmlException(ErrorCodes.DataObjectTypesDontMatch);

                throw new WitsmlException(function.GetNonConformingErrorCode());
            }

            if (IsSupported(function, objectType))
                return;

            if (function == Functions.AddToStore)
                throw new WitsmlException(ErrorCodes.DataObjectTypeNotSupported);

            throw new WitsmlException(ErrorCodes.DataObjectNotSupported);
        }

        /// <summary>
        /// Validates the namespace for a specific WITSML data schema version.
        /// </summary>
        /// <param name="document">The document.</param>
        protected virtual void ValidateNamespace(XDocument document)
        {
        }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        /// <param name="document">The XML document.</param>
        /// <returns>The namespace.</returns>
        protected static string GetNamespace(XDocument document)
        {
            return document.Root.GetDefaultNamespace().NamespaceName;
        }

        /// <summary>
        /// Recursively validates the selection criteria for an element.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="allowEmptyRecurringElements">If set to true then allow empty recurring elements.</param>
        private static void ValidateSelectionCriteriaForAnEntity(XElement entity, bool allowEmptyRecurringElements = false)
        {
            _log.DebugFormat("Validating selection criteria for {0}", entity.Name.LocalName);
            if (entity == null) return;

            var elements = entity.Elements();
            if (elements == null) return;

            var groupings = elements.GroupBy(e => e.Name.LocalName);

            foreach (var group in groupings)
            {
                var values = group.ToList();
                var count = values.Count;

                var selection = values[0];

                if (count == 1)
                {
                    ValidateSelectionCriteriaForAnEntity(selection);
                }
                else
                {
                    ValidateEmptyRecurringElement(selection, allowEmptyRecurringElements);

                    for (var i = 1; i < values.Count; i++)
                    {
                        var match = values[i];
                        ValidateEmptyRecurringElement(match, allowEmptyRecurringElements);
                        IsSelectionMatch(selection, match);
                        ValidateSelectionCriteriaForAnEntity(match);
                    }
                }
            }
        }

        private static void IsSelectionMatch(XElement source, XElement target)
        {
            _log.Debug("Validating matching selection criteria.");

            foreach (var attribute in source.Attributes())
            {
                if (!target.Attributes().Any(a => a.Name.LocalName == attribute.Name.LocalName))
                    throw new WitsmlException(ErrorCodes.RecurringItemsInconsistentSelection);
            }

            foreach (var attribute in target.Attributes())
            {
                if (!source.Attributes().Any(a => a.Name.LocalName == attribute.Name.LocalName))
                    throw new WitsmlException(ErrorCodes.RecurringItemsInconsistentSelection);
            }

            foreach (var element in source.Elements())
            {
                if (!target.Elements().Any(e => e.Name.LocalName == element.Name.LocalName))
                    throw new WitsmlException(ErrorCodes.RecurringItemsInconsistentSelection);
            }

            foreach (var element in target.Elements())
            {
                if (!source.Elements().Any(e => e.Name.LocalName == element.Name.LocalName))
                    throw new WitsmlException(ErrorCodes.RecurringItemsInconsistentSelection);
            }
        }

        /// <summary>
        /// Determines whether there is empty recurring element
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="allowEmptyRecurringElements">If set to true then allow empty recurring elements.</param>
        /// <exception cref="WitsmlException"></exception>
        private static void ValidateEmptyRecurringElement(XElement element, bool allowEmptyRecurringElements = false)
        {
            // 131 Is allowed to have recurring empty elements
            if (allowEmptyRecurringElements)
                return;

            _log.Debug("Validating empty recurring elements.");

            if (string.IsNullOrEmpty(element.Value) && !element.HasAttributes
                || element.Elements().Any(e => string.IsNullOrEmpty(e.Value))
                || element.Attributes().Any(a => string.IsNullOrEmpty(a.Value)))
            {
                throw new WitsmlException(ErrorCodes.RecurringItemsEmptySelection);
            }
        }
    }
}
