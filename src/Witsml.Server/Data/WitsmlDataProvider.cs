using System.Collections.Generic;
using System.Linq;
using Energistics.DataAccess;

namespace PDS.Witsml.Server.Data
{
    public abstract class WitsmlDataProvider<TList, TObject> : IWitsmlDataProvider, IWitsmlDataWriter where TList : IEnergisticsCollection
    {
        protected readonly IWitsmlDataAdapter<TObject> _dataAdapter;

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

        public virtual WitsmlResult AddToStore(string witsmlType, string xml, string options, string capabilities)
        {
            var list = EnergisticsConverter.XmlToObject<TList>(xml);
            return _dataAdapter.Add(list.Items.Cast<TObject>().Single());
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
