using System;
using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;
using log4net;

namespace PDS.Witsml.Server.Data
{
    public abstract class WitsmlDataProvider<TList, TObject> : IWitsmlDataProvider, IWitsmlDataWriter where TList : IEnergisticsCollection
    {
        protected readonly IWitsmlDataAdapter<TObject> _dataAdapter;
        private static readonly ILog _log = LogManager.GetLogger(typeof(WitsmlDataProvider<TList, TObject>));

        protected WitsmlDataProvider(IWitsmlDataAdapter<TObject> dataAdapter)
        {
            _dataAdapter = dataAdapter;
        }

        public virtual WitsmlResult<IEnergisticsCollection> GetFromStore(string witsmlType, string query, string options, string capabilities)
        {
            var parser = new WitsmlQueryParser(witsmlType, query, options, capabilities);
            var result = _dataAdapter.Query(parser);
            return FormatResponse(parser, result);
        }

        /// <summary>
        /// Implementation of WITSML AddToStore interface; for adding WITSML object to store
        /// </summary>
        /// <param name="witsmlType">Input string that specifies WITSML data-object type</param>
        /// <param name="xml">Input string for the WITSML data-object to be added</param>
        /// <param name="options">Input string that specifies the options</param>
        /// <param name="capabilities">Input string that specifies the client’s Capabilities Object (capClient) to be sent to the server</param>
        /// <returns>A WITSML result object that includes return code and/or message</returns>
        public virtual WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities)
        {
            try
            {
                var list = EnergisticsConverter.XmlToObject<TList>(xml);
                return _dataAdapter.Add(list.Items.Cast<TObject>().Single());
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                _log.ErrorFormat("Error converting XMLIn to Engergistic object: {0}{1}{2}", witsmlType, Environment.NewLine, message);
                return new WitsmlResult(ErrorCodes.Unset, message);
            }
        }

        public virtual WitsmlResult UpdateInStore(string witsmlType, string xml, string options, string capabilities)
        {
            var list = EnergisticsConverter.XmlToObject<TList>(xml);
            return _dataAdapter.Update(list.Items.Cast<TObject>().Single());
        }

        public virtual WitsmlResult DeleteFromStore(string witsmlType, string xml, string options, string capabilities)
        {
            var parser = new WitsmlQueryParser(witsmlType, xml, options, capabilities);
            return _dataAdapter.Delete(parser);
        }

        protected abstract WitsmlResult<IEnergisticsCollection> FormatResponse(WitsmlQueryParser parser, WitsmlResult<List<TObject>> result);
    }
}
