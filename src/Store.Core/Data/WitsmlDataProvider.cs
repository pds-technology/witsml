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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Energistics.DataAccess.WITSML200;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Datatypes.Object;
using log4net;
using PDS.WITSMLstudio.Framework;

namespace PDS.WITSMLstudio.Store.Data
{
    /// <summary>
    /// Data provider that implements support for WITSML API functions.
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IEtpDataProvider{TObject}" />
    /// <seealso cref="PDS.WITSMLstudio.Store.Data.IEtpDataProvider" />
    public abstract class WitsmlDataProvider<TObject> : IEtpDataProvider<TObject>, IEtpDataProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlDataProvider{TObject}" /> class.
        /// </summary>
        /// <param name="container">The composition container.</param>
        /// <param name="dataAdapter">The data adapter.</param>
        protected WitsmlDataProvider(IContainer container, IWitsmlDataAdapter<TObject> dataAdapter)
        {
            Logger = LogManager.GetLogger(GetType());
            Container = container;
            DataAdapter = dataAdapter;
        }

        /// <summary>
        /// Gets the server sort order.
        /// </summary>
        public virtual string ServerSortOrder => DataAdapter.ServerSortOrder;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILog Logger { get; }

        /// <summary>
        /// Gets the composition container.
        /// </summary>
        /// <value>The composition container.</value>
        protected IContainer Container { get; }

        /// <summary>
        /// Gets the data adapter.
        /// </summary>
        /// <value>The data adapter.</value>
        protected IWitsmlDataAdapter<TObject> DataAdapter { get; }

        /// <summary>
        /// Adds the content types managed by this data adapter to the collection of <see cref="EtpContentType"/>.
        /// </summary>
        /// <param name="contentTypes">A collection of content types.</param>
        public virtual void GetSupportedObjects(IList<EtpContentType> contentTypes)
        {
            var type = typeof(TObject);

            if (!ObjectTypes.IsEnergisticsObjectType(type))
                return;

            var contentType = EtpUris.GetUriFamily(type)
                .Append(ObjectTypes.GetObjectType(type))
                .ContentType;

            contentTypes.Add(contentType);
        }

        /// <summary>
        /// Determines whether the data object exists in the data store.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>true if the data object exists; otherwise, false</returns>
        public virtual bool Exists(EtpUri uri)
        {
            return DataAdapter.Exists(uri);
        }

        /// <summary>
        /// Gets the count of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>The count of related data objects.</returns>
        public virtual int Count(EtpUri? parentUri)
        {
            return DataAdapter.Count(parentUri);
        }

        /// <summary>
        /// Determines if the specified URI has child data objects.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>If there are any related data objects.</returns>
        public virtual bool Any(EtpUri? parentUri)
        {
            return DataAdapter.Any(parentUri);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        IList IEtpDataProvider.GetAll(EtpUri? parentUri)
        {
            return GetAll(parentUri);
        }

        /// <summary>
        /// Gets a collection of data objects related to the specified URI.
        /// </summary>
        /// <param name="parentUri">The parent URI.</param>
        /// <returns>A collection of data objects.</returns>
        public virtual List<TObject> GetAll(EtpUri? parentUri = null)
        {
            return DataAdapter.GetAll(parentUri);
        }

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        object IEtpDataProvider.Get(EtpUri uri)
        {
            return Get(uri);
        }

        /// <summary>
        /// Gets a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        /// <returns>The data object instance.</returns>
        public virtual TObject Get(EtpUri uri)
        {
            return DataAdapter.Get(uri);
        }

        /// <summary>
        /// Puts the specified data object into the data store.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        public virtual void Put(IDataObject dataObject)
        {
            var context = WitsmlOperationContext.Current;
            context.Document = WitsmlParser.Parse(context.Request.Xml);
            context.DataObject = dataObject;

            var parser = new WitsmlQueryParser(context.Document.Root, context.Request.ObjectType, null);

            Put(parser);
        }

        /// <summary>
        /// Puts a data object into the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        public virtual void Put(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<TObject>();
            Logger.Debug($"Putting {typeof(TObject).Name} with URI '{uri}'");

            if (!string.IsNullOrWhiteSpace(uri.ObjectId) && Exists(uri))
            {
                Replace(parser);
            }
            else
            {
                Add(parser);
            }
        }

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public virtual void Delete(EtpUri uri)
        {
            using (var transaction = DataAdapter.GetTransaction())
            {
                transaction.SetContext(uri);

                // Create an empty dataObject to use for delete with parser
                var dataObject = Activator.CreateInstance<TObject>();
                SetDefaultValues(dataObject, uri);

                var parser = CreateQueryParser(dataObject);
                parser.RemoveSubElements();

                var context = WitsmlOperationContext.Current;
                context.Document = parser.Root.Document;

                Delete(parser);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Ensures the data object exists with the specified URI, otherwise, it is created.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public virtual void Ensure(EtpUri uri)
        {
            if (Exists(uri)) return;
            var parent = uri.Parent;

            using (var transaction = DataAdapter.GetTransaction())
            {
                transaction.SetContext(uri);

                if (!parent.IsBaseUri)
                {
                    var dataProvider = Container.Resolve<IEtpDataProvider>(new ObjectName(parent.ObjectType, uri.Family, parent.GetDataSchemaVersion()));
                    dataProvider.Ensure(parent);
                }

                // Create object instance
                var instance = Activator.CreateInstance<TObject>();
                SetDefaultValues(instance, uri);

                // Add to data store using parser
                var parser = CreateQueryParser(instance);
                Add(parser);

                transaction.Commit();
            }
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>The data object URI.</returns>
        protected abstract EtpUri GetUri(TObject dataObject);

        /// <summary>
        /// Adds a data object to the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        protected virtual WitsmlResult Add(WitsmlQueryParser parser)
        {
            using (var transaction = DataAdapter.GetTransaction())
            {
                var validator = Container.Resolve<IDataObjectValidator<TObject>>();
                var element = validator.Parse(Functions.AddToStore, parser);
                var dataObject = Parse(element);

                SetDefaultValues(dataObject);
                var uri = GetUri(dataObject);
                transaction.SetContext(uri);
                Logger.DebugFormat("Adding {0} with URI '{1}'", typeof(TObject).Name, uri);

                if (DataAdapter.IsValidationEnabled(Functions.AddToStore, parser, dataObject))
                {
                    validator.Validate(Functions.AddToStore, parser, dataObject);
                    Logger.DebugFormat("Validated {0} with URI '{1}' for Add", typeof(TObject).Name, uri);
                }

                DataAdapter.Add(parser, dataObject);
                transaction.Commit();

                return Success(uri.ObjectId);
            }
        }

        /// <summary>
        /// Updates a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        protected virtual WitsmlResult Update(WitsmlQueryParser parser)
        {
            using (var transaction = DataAdapter.GetTransaction())
            {
                var validator = Container.Resolve<IDataObjectValidator<TObject>>();
                var element = validator.Parse(Functions.UpdateInStore, parser);
                var dataObject = Parse(element);

                UpdateDefaultValues(dataObject, parser);
                var uri = GetUri(dataObject);
                transaction.SetContext(uri);
                Logger.DebugFormat("Updating {0} with URI '{1}'", typeof(TObject).Name, uri);

                if (DataAdapter.IsValidationEnabled(Functions.UpdateInStore, parser, dataObject))
                {
                    validator.Validate(Functions.UpdateInStore, parser, dataObject);
                    Logger.DebugFormat("Validated {0} with URI '{1}' for Update", typeof(TObject).Name, uri);
                }

                DataAdapter.Update(parser, dataObject);
                transaction.Commit();

                return Success();
            }
        }

        /// <summary>
        /// Replaces a data object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        protected virtual WitsmlResult Replace(WitsmlQueryParser parser)
        {
            using (var transaction = DataAdapter.GetTransaction())
            {
                var validator = Container.Resolve<IDataObjectValidator<TObject>>();
                var element = validator.Parse(Functions.PutObject, parser);
                var dataObject = Parse(element);

                SetDefaultValues(dataObject);
                var uri = GetUri(dataObject);
                transaction.SetContext(uri);
                Logger.DebugFormat("Replacing {0} with URI '{1}'", typeof(TObject).Name, uri);

                if (DataAdapter.IsValidationEnabled(Functions.PutObject, parser, dataObject))
                {
                    validator.Validate(Functions.PutObject, parser, dataObject);
                    Logger.DebugFormat("Validated {0} with URI '{1}' for Replace", typeof(TObject).Name, uri);
                }

                DataAdapter.Replace(parser, dataObject);
                transaction.Commit();

                return Success();
            }
        }

        /// <summary>
        /// Deletes or partially updates an object in the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        protected virtual WitsmlResult Delete(WitsmlQueryParser parser)
        {
            using (var transaction = DataAdapter.GetTransaction())
            {
                var validator = Container.Resolve<IDataObjectValidator<TObject>>();
                var element = validator.Parse(Functions.DeleteFromStore, parser);
                var dataObject = Parse(element);

                var uri = GetUri(dataObject);
                transaction.SetContext(uri);
                Logger.DebugFormat("Deleting {0} with URI '{1}'", typeof(TObject).Name, uri);

                if (DataAdapter.IsValidationEnabled(Functions.DeleteFromStore, parser, dataObject))
                {
                    validator.Validate(Functions.DeleteFromStore, parser, dataObject);
                    Logger.DebugFormat("Validated {0} with URI '{1}' for Delete", typeof(TObject).Name, uri);
                }

                DataAdapter.Delete(parser);
                transaction.Commit();

                return Success();
            }
        }

        /// <summary>
        /// Creates a successful <see cref="WitsmlResult"/> response.
        /// </summary>
        /// <param name="message">An optional message to include.</param>
        /// <returns>A <see cref="WitsmlResult"/> response.</returns>
        protected virtual WitsmlResult Success(string message = null)
        {
            var op = WitsmlOperationContext.Current;
            var messages = op.Warnings.Select(x => x.ErrorMessage).ToList();

            if (!string.IsNullOrWhiteSpace(message))
                messages.Insert(0, message);

            return new WitsmlResult(
                op.Warnings.Any() ? ErrorCodes.SuccessWithWarnings : ErrorCodes.Success,
                string.Join(" ", messages));
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <returns>The data object instance.</returns>
        protected virtual TObject Parse(XElement element)
        {
            return WitsmlParser.Parse<TObject>(element);
        }

        /// <summary>
        /// Validates the input template for the specified function.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        protected virtual void Validate(Functions function, WitsmlQueryParser parser, TObject dataObject)
        {
            if (!DataAdapter.IsValidationEnabled(function, parser, dataObject)) return;

            var validator = Container.Resolve<IDataObjectValidator<TObject>>();
            validator.Validate(function, parser, dataObject);
        }

        /// <summary>
        /// Creates a new <see cref="WitsmlQueryParser"/> from the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns>A new <see cref="WitsmlQueryParser"/> instance.</returns>
        protected virtual WitsmlQueryParser CreateQueryParser(TObject dataObject)
        {
            var document = WitsmlParser.Parse(WitsmlParser.ToXml(dataObject));
            var objectType = ObjectTypes.GetObjectType(dataObject as AbstractObject);

            return new WitsmlQueryParser(document.Root, objectType, null);
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected virtual void SetDefaultValues(TObject dataObject)
        {
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="uri">The data object URI.</param>
        protected virtual void SetDefaultValues(TObject dataObject, EtpUri uri)
        {
        }

        /// <summary>
        /// Sets the default values for the specified data object during update.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <param name="parser">The element.</param>
        protected virtual void UpdateDefaultValues(TObject dataObject, WitsmlQueryParser parser)
        {
        }
    }
}
