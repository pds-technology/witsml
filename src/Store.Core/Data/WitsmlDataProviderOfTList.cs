//----------------------------------------------------------------------- 
// PDS WITSMLstudio Store, 2018.3
//
// Copyright 2018 PDS Americas LLC
// 
// Licensed under the PDS Open Source WITSML Product License Agreement (the
// "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//     http://www.pds.group/WITSMLstudio/OpenSource/ProductLicenseAgreement
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess;
using Energistics.Etp.Common.Datatypes;
using PDS.WITSMLstudio.Framework;
using PDS.WITSMLstudio.Store.Configuration;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions.
    /// </summary>
    /// <typeparam name="TList">Type of the object list.</typeparam>
    /// <typeparam name="TObject">Type of the object.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IWitsmlDataProvider" />
    public abstract class WitsmlDataProvider<TList, TObject> : WitsmlDataProvider<TObject>, IWitsmlDataProvider
        where TList : IEnergisticsCollection
        where TObject : class, IDataObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlDataProvider{TList, TObject}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        protected WitsmlDataProvider(IContainer container, IWitsmlDataAdapter<TObject> dataAdapter) : base(container, dataAdapter)
        {
        }

        /// <summary>
        /// Gets object(s) from store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>Queried objects.</returns>
        public virtual WitsmlResult<IEnergisticsCollection> GetFromStore(RequestContext context)
        {
            Logger.DebugFormat("Getting {0}", typeof(TObject).Name);

            var op = WitsmlOperationContext.Current;
            var parser = new WitsmlQueryParser(op.Document.Root, context.ObjectType, context.Options);
            var childParsers = parser.ForkElements().ToArray();

            // Validate each query template separately
            foreach (var childParser in childParsers)
                Validate(Functions.GetFromStore, childParser, null);

            Logger.DebugFormat("Validated {0} for Query", typeof(TObject).Name);

            var responseContext = parser.ToContext();

            // Execute each query separately
            var results = childParsers
                .SelectMany(p => DataAdapter.Query(p, responseContext))
                .ToList(); // Fully evaluate before setting the error code.

            var collection = CreateCollection(results);

            // Generate documentInfo, if requested
            if (collection.Items.Count > 0 && parser.HasDocumentInfo())
                collection.SetDocumentInfo(parser, op.User);

            return new WitsmlResult<IEnergisticsCollection>(
                responseContext.DataTruncated && op.DataSchemaVersion.Equals(OptionsIn.DataVersion.Version141.Value)
                    ? op.Warnings.Any() ? ErrorCodes.PartialSuccessWithWarnings : ErrorCodes.ParialSuccess 
                    : op.Warnings.Any() ? ErrorCodes.SuccessWithWarnings : ErrorCodes.Success,
                string.Join(" ", op.Warnings.Select(x => x.ErrorMessage)),
                collection);
        }

        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult AddToStore(RequestContext context)
        {
            Logger.DebugFormat("Adding {0}", typeof(TObject).Name);

            var op = WitsmlOperationContext.Current;
            var root = op.Document.Root;
            var parser = new WitsmlQueryParser(root, context.ObjectType, context.Options);

            return Add(parser);
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult UpdateInStore(RequestContext context)
        {
            Logger.DebugFormat("Updating {0}", typeof(TObject).Name);

            var op = WitsmlOperationContext.Current;
            var root = op.Document.Root;
            var parser = new WitsmlQueryParser(root, context.ObjectType, context.Options);

            return Update(parser);
        }

        /// <summary>
        /// Deletes or partially update object from store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult DeleteFromStore(RequestContext context)
        {
            Logger.DebugFormat("Deleting {0}", typeof(TObject).Name);

            var op = WitsmlOperationContext.Current;
            var root = op.Document.Root;
            var parser = new WitsmlQueryParser(root, context.ObjectType, context.Options);

            WitsmlOperationContext.Current.IsCascadeDelete = parser.CascadedDelete();

            return Delete(parser);
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
        protected override EtpUri GetUri(TObject dataObject)
        {
            return dataObject.GetUri();
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The data object instance.</returns>
        protected override TObject Parse(XElement element)
        {
            var list = WitsmlParser.Parse<TList>(element, false);
            return list.Items.Cast<TObject>().FirstOrDefault();
        }

        /// <summary>
        /// Creates a new <see cref="WitsmlQueryParser"/> from the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A new <see cref="WitsmlQueryParser"/> instance.</returns>
        protected override WitsmlQueryParser CreateQueryParser(TObject dataObject)
        {
            var container = CreateCollection(dataObject.AsList());
            var document = WitsmlParser.Parse(WitsmlParser.ToXml(container));
            var objectType = ObjectTypes.GetObjectType(container);

            return new WitsmlQueryParser(document.Root, objectType, null);
        }

        /// <summary>
        /// Creates the collection.
        /// </summary>
        /// <param name="dataObjects">The data objects.</param>
        /// <returns></returns>
        protected abstract TList CreateCollection(List<TObject> dataObjects);
    }
}
