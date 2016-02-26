using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Data.Logs
{
    [Export141(ObjectTypes.Log, typeof(IWitsmlDataProvider))]
    [Export141(ObjectTypes.Log, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Log141DataProvider : WitsmlDataProvider<LogList, Log>
    {
        [ImportingConstructor]
        public Log141DataProvider(IWitsmlDataAdapter<Log> dataAdapter) : base(dataAdapter)
        {
        }

        protected override WitsmlResult<IEnergisticsCollection> FormatResponse(WitsmlQueryParser parser, WitsmlResult<List<Log>> result)
        {
            // TODO: format response according to OptionsIn

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new LogList()
                {
                    Log = result.Results
                });
        }
    }
}
