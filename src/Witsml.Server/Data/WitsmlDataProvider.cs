using System.Linq;
using Energistics.DataAccess;
using log4net;
using PDS.Witsml.Server.Configuration;

namespace PDS.Witsml.Server.Data
{
    /// <summary>
    /// Data provider that encapsulates CRUD service calls for WITSML query.
    /// </summary>
    /// <typeparam name="TList">Type of the object list.</typeparam>
    /// <typeparam name="TObject">Type of the object.</typeparam>
    /// <seealso cref="PDS.Witsml.Server.Data.IWitsmlDataProvider" />
    /// <seealso cref="PDS.Witsml.Server.Data.IWitsmlDataWriter" />
    public abstract class WitsmlDataProvider<TList, TObject> : IWitsmlDataProvider, IWitsmlDataWriter where TList : IEnergisticsCollection
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlDataProvider<TList, TObject>));
        private readonly IWitsmlDataAdapter<TObject> _dataAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="WitsmlDataProvider{TList, TObject}"/> class.
        /// </summary>
        /// <param name="dataAdapter">The data adapter.</param>
        protected WitsmlDataProvider(IWitsmlDataAdapter<TObject> dataAdapter)
        {
            _dataAdapter = dataAdapter;
        }

        /// <summary>
        /// Gets object(s) from store.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>Queried objects.</returns>
        public virtual WitsmlResult<IEnergisticsCollection> GetFromStore(RequestContext context)
        {
            var parser = new WitsmlQueryParser(context);
            return _dataAdapter.Query(parser);
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
            var list = WitsmlParser.Parse<TList>(context.Xml);
            return _dataAdapter.Add(list.Items.Cast<TObject>().Single());
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
            var list = WitsmlParser.Parse<TList>(context.Xml);
            return _dataAdapter.Update(list.Items.Cast<TObject>().Single());
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
            var parser = new WitsmlQueryParser(context);
            return _dataAdapter.Delete(parser);
        }
    }
}
