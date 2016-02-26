using System.Collections.Generic;
using System.ComponentModel.Composition;
using Energistics.DataAccess;
using Energistics.DataAccess.WITSML141;

namespace PDS.Witsml.Server.Data.Wellbores
{
    [Export141(ObjectTypes.Wellbore, typeof(IWitsmlDataProvider))]
    [Export141(ObjectTypes.Wellbore, typeof(IWitsmlDataWriter))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class Wellbore141DataProvider : WitsmlDataProvider<WellboreList, Wellbore>
    {
        [ImportingConstructor]
        public Wellbore141DataProvider(IWitsmlDataAdapter<Wellbore> dataAdapter) : base(dataAdapter)
        {
        }

        protected override WitsmlResult<IEnergisticsCollection> FormatResponse(WitsmlQueryParser parser, WitsmlResult<List<Wellbore>> result)
        {
            // TODO: format response according to OptionsIn

            return new WitsmlResult<IEnergisticsCollection>(
                ErrorCodes.Success,
                new WellboreList()
                {
                    Wellbore = result.Results
                });
        }
    }
}
