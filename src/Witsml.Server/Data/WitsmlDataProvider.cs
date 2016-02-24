using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Energistics.DataAccess;
using log4net;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data provider that encapsulates CRUD service calls for WITSML query.
    /// </summary>
    /// <typeparam name="TList">Type of the object list.</typeparam>
    /// <typeparam name="TObject">Type of the object.</typeparam>
    /// <seealso cref="IWitsmlDataProvider" />
    /// <seealso cref="IWitsmlDataWriter" />
    public abstract class WitsmlDataProvider<TList, TObject> : IWitsmlDataProvider, IWitsmlDataWriter where TList : IEnergisticsCollection
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlDataProvider<TList, TObject>));
        private readonly IWitsmlDataAdapter<TObject> _dataAdapter;
        private readonly string _dataSchemaVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlDataProvider{TList, TObject}"/> class.
        /// </summary>
        /// <param name="dataAdapter">The data adapter.</param>
        protected WitsmlDataProvider(IWitsmlDataAdapter<TObject> dataAdapter, string dataSchemaVersion)
        {
            _dataAdapter = dataAdapter;
            _dataSchemaVersion = dataSchemaVersion;
        }

        /// <summary>
        /// Gets or sets the cap server providers.
        /// </summary>
        /// <value>The cap server providers.</value>
        [ImportMany]
        public IEnumerable<ICapServerProvider> CapServerProviders { get; set; }

        /// <summary>
        /// Gets object(s) from store.
        /// </summary>
        /// <param name="witsmlType">Type of the data-object.</param>
        /// <param name="query">The XML query string.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// Queried objects.
        /// </returns>
        public virtual WitsmlResult<IEnergisticsCollection> GetFromStore(string witsmlType, string query, string options, string capabilities)
        {
            var parser = new WitsmlQueryParser(witsmlType, query, options, capabilities);
            var result = _dataAdapter.Query(parser);
            return FormatResponse(parser, result);
        }

        /// <summary>
        /// Adds an object to the data store.
        /// </summary>
        /// <param name="witsmlType">Type of the data-object.</param>
        /// <param name="xml">The XML string for the data-object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities)
        {
            var capServerProvider = CapServerProviders.FirstOrDefault(x => x.DataSchemaVersion == _dataSchemaVersion);
            capServerProvider.Validate(Functions.AddToStore, witsmlType, xml, options, capabilities);

            var list = EnergisticsConverter.XmlToObject<TList>(xml);
            return _dataAdapter.Add(list.Items.Cast<TObject>().Single());
        }

        /// <summary>
        /// Updates an object in the data store.
        /// </summary>
        /// <param name="witsmlType">Type of the data-object.</param>
        /// <param name="xml">The XML string for the data-object.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult UpdateInStore(string witsmlType, string xml, string options, string capabilities)
        {
            var list = EnergisticsConverter.XmlToObject<TList>(xml);
            return _dataAdapter.Update(list.Items.Cast<TObject>().Single());
        }

        /// <summary>
        /// Deletes or partially update object from store.
        /// </summary>
        /// <param name="witsmlType">Type of the data-object.</param>
        /// <param name="xml">The XML string for the delete query.</param>
        /// <param name="options">The options.</param>
        /// <param name="capabilities">The client’s Capabilities Object (capClient).</param>
        /// <returns>
        /// A WITSML result that includes a positive value indicates a success or a negative value indicates an error.
        /// </returns>
        public virtual WitsmlResult DeleteFromStore(string witsmlType, string xml, string options, string capabilities)
        {
            var parser = new WitsmlQueryParser(witsmlType, xml, options, capabilities);
            return _dataAdapter.Delete(parser);
        }
        
        protected abstract WitsmlResult<IEnergisticsCollection> FormatResponse(WitsmlQueryParser parser, WitsmlResult<List<TObject>> result);
    }
}
