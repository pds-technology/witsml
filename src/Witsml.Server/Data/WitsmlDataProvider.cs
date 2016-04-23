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

using System.Collections;
using System.Collections.Generic;
using Energistics.Common;
using Energistics.DataAccess;
using Energistics.Datatypes;
using Energistics.Datatypes.Object;
using log4net;
using PDS.Framework;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
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

            if (type.Assembly != typeof(IDataObject).Assembly)
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
        public virtual void Put(DataObject dataObject)
        {
            var context = new RequestContext(Functions.PutObject, ObjectTypes.GetObjectType<TObject>(),
                dataObject.GetXml(), null, null);

            var parser = new WitsmlQueryParser(context);

            Put(parser);
        }

        /// <summary>
        /// Puts a data object into the data store.
        /// </summary>
        /// <param name="parser">The input template parser.</param>
        public virtual void Put(WitsmlQueryParser parser)
        {
            var uri = parser.GetUri<TObject>();
            Logger.DebugFormat("Putting {0} with URI '{1}'", typeof(TObject).Name, uri);

            if (!string.IsNullOrWhiteSpace(uri.ObjectId) && Exists(uri))
            {
                Update(parser);
            }

            Add(parser);
        }

        /// <summary>
        /// Deletes a data object by the specified URI.
        /// </summary>
        /// <param name="uri">The data object URI.</param>
        public virtual void Delete(EtpUri uri)
        {
            DataAdapter.Delete(uri);
        }

        /// <summary>
        /// Gets the URI for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        /// <returns></returns>
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
            var dataObject = Parse(parser.Context.Xml);

            SetDefaultValues(dataObject);
            var uri = GetUri(dataObject);
            Logger.DebugFormat("Adding {0} with URI '{1}'", typeof(TObject).Name, uri);

            Validate(Functions.AddToStore, parser, dataObject);
            Logger.DebugFormat("Validated {0} with URI '{1}' for Add", typeof(TObject).Name, uri);

            DataAdapter.Add(parser, dataObject);
            return new WitsmlResult(ErrorCodes.Success, uri.ObjectId);
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
            var dataObject = Parse(parser.Context.Xml);

            var uri = GetUri(dataObject);
            Logger.DebugFormat("Updating {0} with URI '{1}'", typeof(TObject).Name, uri);

            Validate(Functions.UpdateInStore, parser, dataObject);
            Logger.DebugFormat("Validated {0} with URI '{1}' for Update", typeof(TObject).Name, uri);

            DataAdapter.Update(parser, dataObject);
            return new WitsmlResult(ErrorCodes.Success);
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
            var dataObject = Parse(parser.Context.Xml);
            Logger.DebugFormat("Deleting {0}", typeof(TObject).Name);

            Validate(Functions.DeleteFromStore, parser, dataObject);
            Logger.DebugFormat("Validated {0} for Delete", typeof(TObject).Name);

            DataAdapter.Delete(parser);
            return new WitsmlResult(ErrorCodes.Success);
        }

        /// <summary>
        /// Parses the specified XML string.
        /// </summary>
        /// <param name="xml">The XML string.</param>
        /// <returns>The data object instance.</returns>
        protected virtual TObject Parse(string xml)
        {
            return WitsmlParser.Parse<TObject>(xml);
        }

        /// <summary>
        /// Validates the input template for the specified function.
        /// </summary>
        /// <param name="function">The WITSML API method.</param>
        /// <param name="parser">The input template parser.</param>
        /// <param name="dataObject">The data object.</param>
        protected virtual void Validate(Functions function, WitsmlQueryParser parser, TObject dataObject)
        {
            var validator = Container.Resolve<IDataObjectValidator<TObject>>();
            validator.Validate(function, parser, dataObject);

            if (function == Functions.AddToStore || function == Functions.UpdateInStore)
                DataAdapter.Validate(parser);
        }

        /// <summary>
        /// Sets the default values for the specified data object.
        /// </summary>
        /// <param name="dataObject">The data object.</param>
        protected virtual void SetDefaultValues(TObject dataObject)
        {
        }
    }
}
